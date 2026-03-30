using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;


namespace PoetryPlatform.Web.Controllers;

public class PoemsController(ApplicationDbContext db) : Controller
{
    // =========================
    // PUBLIC LIST (LOGIN REQUIRED)
    // =========================
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var poems = await db.Poems
            .Include(p => p.Author)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(poems);
    }

    // =========================
    // TRENDING POEMS 🔥
    // =========================
    [Authorize]
    public async Task<IActionResult> Trending()
    {
        var poems = await db.Poems
            .Where(p => p.IsPublished)
            .Select(p => new TrendingPoemVm
            {
                Id = p.Id,
                Title = p.Title,
                AuthorName = p.Author!.AuthorName ?? p.Author.UserName!,
                CreatedAt = p.CreatedAt,

                UniqueReaders = db.PoemViews.Count(v => v.PoemId == p.Id),
                Likes = db.PoemLikes.Count(l => l.PoemId == p.Id),

                Comments = db.Comments.Count(c =>
                    c.PoemId == p.Id &&
                    !c.IsDeleted &&
                    !c.IsFlagged),

                Score =
                    db.PoemViews.Count(v => v.PoemId == p.Id) * 1 +
                    db.PoemLikes.Count(l => l.PoemId == p.Id) * 3 +
                    db.Comments.Count(c =>
                        c.PoemId == p.Id &&
                        !c.IsDeleted &&
                        !c.IsFlagged) * 2
            })
            .OrderByDescending(p => p.Score)
            .ToListAsync();

        return View(poems);
    }


    // =========================
    // READ POEM (VIEWS + LIKES + COMMENTS)
    // =========================
    [Authorize]
    public async Task<IActionResult> Read(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .Include(p => p.Author)
            .Include(p => p.Comments.Where(c => !c.IsFlagged && !c.IsDeleted))
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

        if (poem == null)
            return NotFound();

        // ✅ Unique view tracking
        bool alreadyViewed = await db.PoemViews
            .AnyAsync(v => v.PoemId == id && v.UserId == userId);

        if (!alreadyViewed)
        {
            db.PoemViews.Add(new PoemView
            {
                PoemId = id,
                UserId = userId!
            });

            poem.Views++;
            await db.SaveChangesAsync();
        }

        // Likes info
        ViewBag.LikeCount = await db.PoemLikes.CountAsync(l => l.PoemId == id);
        ViewBag.UserHasLiked = await db.PoemLikes
            .AnyAsync(l => l.PoemId == id && l.UserId == userId);

        // FOR FLAG BUTTON UI (per-user)
        ViewBag.UserCommentFlags = await db.CommentFlags
            .Where(f => f.UserId == userId)
            .Select(f => f.CommentId)
            .ToListAsync();


        return View(poem);
    }

    // =========================
    // TOGGLE LIKE ❤️
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int poemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existingLike = await db.PoemLikes
            .FirstOrDefaultAsync(l => l.PoemId == poemId && l.UserId == userId);

        if (existingLike == null)
        {
            db.PoemLikes.Add(new PoemLike
            {
                PoemId = poemId,
                UserId = userId
            });
        }
        else
        {
            db.PoemLikes.Remove(existingLike);
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Read), new { id = poemId });
    }

    // =========================
    // ADD COMMENT
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int poemId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return RedirectToAction(nameof(Read), new { id = poemId });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        db.Comments.Add(new Comment
        {
            PoemId = poemId,
            UserId = userId,
            Content = content.Trim()
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Read), new { id = poemId });
    }

    // =========================
    // DELETE COMMENT (Writer/Admin)
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var comment = await db.Comments
            .Include(c => c.Poem)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
            return NotFound();

        bool isAdmin = User.IsInRole("Admin");
        bool isPoemOwner = comment.Poem.AuthorId == userId;

        if (!isAdmin && !isPoemOwner)
            return Forbid();

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Read), new { id = comment.PoemId });
    }

    // =========================
    // FLAG COMMENT 
    // =========================
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlagComment(int commentId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        var alreadyFlagged = await db.CommentFlags
            .AnyAsync(f => f.CommentId == commentId && f.UserId == userId);

        if (alreadyFlagged)
        {
            var flaggedcomment = await db.Comments.FindAsync(commentId);
            return RedirectToAction(nameof(Read), new { id = flaggedcomment!.PoemId });
        }


        db.CommentFlags.Add(new CommentFlag
        {
            CommentId = commentId,
            UserId = userId
        });

        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null)
            return NotFound();

        if (comment.IsFlagged)
        {
            return RedirectToAction(nameof(Read), new { id = comment.PoemId });
        }

        comment.FlagCount++;

        // 🚨 Threshold (example: 3 flags)
        if (comment.FlagCount >= 3)
            comment.IsFlagged = true;

        await db.SaveChangesAsync();

        return RedirectToAction("Read", new { id = comment.PoemId });
    }

    // =========================
    // MOST COMMENT 
    // =========================

    [Authorize]
    public async Task<IActionResult> MostCommented(int days = 0)
    {
        DateTime? fromDate = days switch
        {
            7 => DateTime.UtcNow.AddDays(-7),
            30 => DateTime.UtcNow.AddDays(-30),
            _ => null
        };

        var poems = await db.Poems
            .Where(p => p.IsPublished)
            .Select(p => new MostCommentedPoemVm
            {
                Id = p.Id,
                Title = p.Title,
                AuthorName = p.Author!.AuthorName ?? p.Author.UserName!,
                CreatedAt = p.CreatedAt,

                CommentCount = db.Comments.Count(c =>
                    c.PoemId == p.Id &&
                    !c.IsDeleted &&
                    !c.IsFlagged &&
                    (fromDate == null || c.CreatedAt >= fromDate))
            })
            .OrderByDescending(p => p.CommentCount)
            .ThenByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.Days = days;

        int rank = 1;
        foreach (var poem in poems)
        {
            poem.Rank = rank++;
        }

        return View(poems);
    }






}
