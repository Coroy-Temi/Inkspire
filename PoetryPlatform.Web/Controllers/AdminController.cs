using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    // =========================
    // ADMIN DASHBOARD
    // =========================
    public IActionResult Dashboard()
    {
        return View();
    }

    // =========================
    // USERS LIST
    // =========================
    public IActionResult Users()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    // =========================
    // LOCK USER
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        // 🔒 Lock for 100 years
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Users));
    }

    // =========================
    // UNLOCK USER
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.LockoutEnd = null;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Users));
    }

    // =========================
    // FLAGGED COMMENTS 🚩
    // =========================
    public async Task<IActionResult> FlaggedComments()
    {
        var comments = await _db.Comments
            .Include(c => c.Poem)
            .Where(c => c.IsFlagged && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(comments);
    }

    // =========================
    // SOFT DELETE COMMENT
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _db.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        comment.IsDeleted = true;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FlaggedComments));
    }

    // =========================
    // RESTORE COMMENT
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreComment(int id)
    {
        var comment = await _db.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        comment.IsDeleted = false;
        comment.IsFlagged = false;
        comment.FlagCount = 0;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FlaggedComments));
    }


}
