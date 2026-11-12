using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CommentsController : Controller
{
    private readonly ApplicationDbContext _db;

    public CommentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var comments = await _db.Comments
            .Include(c => c.Poem)
                .ThenInclude(p => p.Author)
            .Include(c => c.Poem)
                .ThenInclude(p => p.Comments)
            .Include(c => c.Poem)
            .Include(c => c.Poem.Author)
            .Include(c => c.Poem)
            .Include(c => c.Poem.Author)
            .Include(c => c.Poem)
            .Select(c => new AdminCommentVm
            {
                CommentId = c.Id,
                Content = c.Content,

                CommenterEmail = _db.Users
                    .Where(u => u.Id == c.UserId)
                    .Select(u => u.Email!)
                    .FirstOrDefault() ?? "Unknown",

                PoemTitle = c.Poem.Title,
                AuthorName = c.Poem.Author!.AuthorName
                             ?? c.Poem.Author.UserName!,

                CreatedAt = c.CreatedAt,
                IsFlagged = c.IsFlagged
            })
            .OrderByDescending(c => c.IsFlagged)
            .ThenByDescending(c =>
                _db.Comments.Count(x => x.Poem.Title == c.PoemTitle))
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(comments);
    }
}
