using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Controllers;

[Authorize]
public class WriterController(ApplicationDbContext db) : Controller
{
    // =========================
    // DASHBOARD
    // =========================
    public IActionResult Dashboard()
    {
        return View();
    }

    // =========================
    // MY WRITINGS
    // =========================
    public async Task<IActionResult> Poems()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poems = await db.Poems
            .Where(p => p.AuthorId == userId)
            .Select(p => new WriterPoemVm
            {
                Id = p.Id,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                Views = p.Views,

                Likes = db.PoemLikes.Count(l => l.PoemId == p.Id),
                Comments = db.Comments.Count(c => c.PoemId == p.Id)
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(poems);
    }


    // =========================
    // CREATE (GET)
    // =========================
    public IActionResult Create()
    {
        return View(new Poem());
    }

    // =========================
    // CREATE (POST)
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Poem poem, IFormFile? pdfFile)
    {
        if (!ModelState.IsValid)
            return View(poem);

        poem.AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        poem.CreatedAt = DateTime.UtcNow;
        poem.IsPublished = true;

        // =========================
        // PDF UPLOAD (OPTIONAL)
        // =========================
        if (pdfFile != null && pdfFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/pdfs");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await pdfFile.CopyToAsync(stream);

            poem.PdfPath = "/uploads/pdfs/" + fileName;
        }

        db.Poems.Add(poem);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }

    // =========================
    // EDIT (GET)
    // =========================
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == userId);

        if (poem == null)
            return NotFound();

        return View(poem);
    }

    // =========================
    // EDIT (POST)
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Poem poem)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var existingPoem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == poem.Id && p.AuthorId == userId);

        if (existingPoem == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(poem);

        existingPoem.Title = poem.Title;
        existingPoem.Content = poem.Content;

        // 🚧 FUTURE FEATURES (PARKED)
        // existingPoem.AccessType = poem.AccessType;
        // existingPoem.Price = poem.Price;
        // existingPoem.ExternalPurchaseUrl = poem.ExternalPurchaseUrl;

        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }

    // =========================
    // DELETE
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == userId);

        if (poem == null)
            return NotFound();

        db.Poems.Remove(poem);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }
}
