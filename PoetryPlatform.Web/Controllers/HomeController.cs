using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexVm
        {
            PublishedPoems = await _db.Poems.CountAsync(p => p.IsPublished),
            ActiveAuthors = await _db.Poems
                .Where(p => p.IsPublished)
                .Select(p => p.AuthorId)
                .Distinct()
                .CountAsync(),
            TotalReads = await _db.PoemViews.CountAsync(),
            TotalComments = await _db.Comments.CountAsync(c => !c.IsDeleted && !c.IsFlagged),
            BookListings = await _db.Poems.CountAsync(p => p.IsPublished && p.PdfPath != null),
            LatestReleases = await _db.Poems
                .AsNoTracking()
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
                    Likes = _db.PoemLikes.Count(l => l.PoemId == p.Id),
                    Comments = _db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged)
                })
                .Take(4)
                .ToListAsync(),
            TrendingNow = await _db.Poems
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .Select(p => new TrendingPoemVm
                {
                    Id = p.Id,
                    Title = p.Title,
                    AuthorName = p.Author!.AuthorName == string.Empty ? p.Author.UserName! : p.Author.AuthorName,
                    AuthorProfileImagePath = p.Author!.ProfileImagePath,
                    CreatedAt = p.CreatedAt,
                    UniqueReaders = _db.PoemViews.Count(v => v.PoemId == p.Id),
                    Likes = _db.PoemLikes.Count(l => l.PoemId == p.Id),
                    Comments = _db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged),
                    Score = _db.PoemViews.Count(v => v.PoemId == p.Id)
                        + (_db.PoemLikes.Count(l => l.PoemId == p.Id) * 3)
                        + (_db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged) * 2)
                })
                .OrderByDescending(p => p.Score)
                .Take(3)
                .ToListAsync(),
            ConversationStarters = await _db.Poems
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .Select(p => new MostCommentedPoemVm
                {
                    Id = p.Id,
                    Title = p.Title,
                    AuthorName = p.Author!.AuthorName == string.Empty ? p.Author.UserName! : p.Author.AuthorName,
                    AuthorProfileImagePath = p.Author!.ProfileImagePath,
                    CreatedAt = p.CreatedAt,
                    CommentCount = _db.Comments.Count(c => c.PoemId == p.Id && !c.IsDeleted && !c.IsFlagged)
                })
                .OrderByDescending(p => p.CommentCount)
                .ThenByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync()
        };

        var rank = 1;
        foreach (var poem in model.ConversationStarters)
        {
            poem.Rank = rank++;
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
