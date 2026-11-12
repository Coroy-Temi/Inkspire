using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Controllers;

public class PoemsController(ApplicationDbContext db) : Controller
{
    private const string PageBreakMarker = "[[page-break]]";

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var poems = await db.Poems
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PoemCardVm
            {
                Id = p.Id,
                Title = p.Title,
                AuthorName = p.Author!.AuthorName == string.Empty ? p.Author.UserName! : p.Author.AuthorName,
                AuthorProfileImagePath = p.Author!.ProfileImagePath,
                CreatedAt = p.CreatedAt,
                AccessType = p.AccessType,
                Price = p.Price,
                HasPdf = p.PdfPath != null,
                ExternalPurchaseUrl = p.ExternalPurchaseUrl,
                Views = p.Views,
                Likes = db.PoemLikes.Count(l => l.PoemId == p.Id),
                Comments = db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged)
            })
            .ToListAsync();

        return View(poems);
    }

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
                AuthorProfileImagePath = p.Author!.ProfileImagePath,
                CreatedAt = p.CreatedAt,
                UniqueReaders = db.PoemViews.Count(v => v.PoemId == p.Id),
                Likes = db.PoemLikes.Count(l => l.PoemId == p.Id),
                Comments = db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged),
                Score =
                    db.PoemViews.Count(v => v.PoemId == p.Id) +
                    (db.PoemLikes.Count(l => l.PoemId == p.Id) * 3) +
                    (db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged) * 2)
            })
            .OrderByDescending(p => p.Score)
            .ToListAsync();

        return View(poems);
    }

    [Authorize]
    public async Task<IActionResult> Read(int id, int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .Include(p => p.Author)
            .Include(p => p.Comments.Where(c => !c.IsFlagged && !c.IsDeleted))
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

        if (poem == null)
        {
            return NotFound();
        }

        var alreadyViewed = await db.PoemViews.AnyAsync(v => v.PoemId == id && v.UserId == userId);
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

        var viewerOwnsPoem = poem.AuthorId == userId || User.IsInRole("Admin");
        BookOrder? latestOrder = null;

        if (!viewerOwnsPoem
            && poem.AccessType == ContentAccessType.PlatformPaid
            && !string.IsNullOrWhiteSpace(userId))
        {
            latestOrder = await db.BookOrders
                .Where(o => o.PoemId == id && o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        var pages = SplitIntoPages(poem.Content);
        var safePage = Math.Clamp(page, 1, pages.Count);
        var likeCount = await db.PoemLikes.CountAsync(l => l.PoemId == id);
        poem.Likes = likeCount;

        var model = new PoemReadVm
        {
            Poem = poem,
            Pages = pages,
            CurrentPage = safePage,
            LikeCount = likeCount,
            UserHasLiked = await db.PoemLikes.AnyAsync(l => l.PoemId == id && l.UserId == userId),
            ViewerOwnsPoem = viewerOwnsPoem,
            HasPurchasedAccess = viewerOwnsPoem || latestOrder?.Status == OrderStatus.Paid,
            LatestOrderId = latestOrder?.Id,
            LatestOrderStatus = latestOrder?.Status,
            LatestCheckoutUrl = latestOrder?.Status == OrderStatus.CheckoutCreated ? latestOrder.CheckoutUrl : null,
            LatestFailureReason = latestOrder?.FailureReason,
            UserCommentFlags = await db.CommentFlags
                .Where(f => f.UserId == userId)
                .Select(f => f.CommentId)
                .ToListAsync()
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int poemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existingLike = await db.PoemLikes
            .FirstOrDefaultAsync(l => l.PoemId == poemId && l.UserId == userId);

        var liked = existingLike == null;
        if (liked)
        {
            db.PoemLikes.Add(new PoemLike
            {
                PoemId = poemId,
                UserId = userId
            });
        }
        else
        {
            db.PoemLikes.Remove(existingLike!);
        }

        await db.SaveChangesAsync();

        var count = await db.PoemLikes.CountAsync(l => l.PoemId == poemId);

        if (IsAjaxRequest())
        {
            return Json(new
            {
                liked,
                count,
                label = liked ? "Unlike this piece" : "Like this piece"
            });
        }

        return RedirectToAction(nameof(Read), new { id = poemId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int poemId, string content, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction(nameof(Read), new { id = poemId, page });
        }

        if (content.Trim().Length > 1000)
        {
            return RedirectToAction(nameof(Read), new { id = poemId, page });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        db.Comments.Add(new Comment
        {
            PoemId = poemId,
            UserId = userId,
            Content = content.Trim()
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Read), new { id = poemId, page });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId, int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var comment = await db.Comments
            .Include(c => c.Poem)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Admin");
        var isPoemOwner = comment.Poem.AuthorId == userId;

        if (!isAdmin && !isPoemOwner)
        {
            return Forbid();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Read), new { id = comment.PoemId, page });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlagComment(int commentId, int page = 1)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var alreadyFlagged = await db.CommentFlags
            .AnyAsync(f => f.CommentId == commentId && f.UserId == userId);

        if (alreadyFlagged)
        {
            var flaggedComment = await db.Comments.FindAsync(commentId);
            return RedirectToAction(nameof(Read), new { id = flaggedComment!.PoemId, page });
        }

        db.CommentFlags.Add(new CommentFlag
        {
            CommentId = commentId,
            UserId = userId
        });

        var comment = await db.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return NotFound();
        }

        if (!comment.IsFlagged)
        {
            comment.FlagCount++;
            comment.FlaggedAt = DateTime.UtcNow;
            if (comment.FlagCount >= 3)
            {
                comment.IsFlagged = true;
            }
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Read), new { id = comment.PoemId, page });
    }

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
                AuthorName = p.Author!.AuthorName == string.Empty ? p.Author.UserName! : p.Author.AuthorName,
                AuthorProfileImagePath = p.Author!.ProfileImagePath,
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

        var rank = 1;
        foreach (var poem in poems)
        {
            poem.Rank = rank++;
        }

        return View(poems);
    }

    public static List<string> SplitIntoPages(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [string.Empty];
        }

        var pages = content
            .Split(PageBreakMarker, StringSplitOptions.None)
            .Select(p => p.Replace("\r\n", "\n").Trim('\n', '\r'))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return pages.Count == 0 ? [content] : pages;
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }
}
