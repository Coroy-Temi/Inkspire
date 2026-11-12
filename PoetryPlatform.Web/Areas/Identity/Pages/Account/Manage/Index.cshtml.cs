using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int WritingCount { get; set; }
    public int TotalViews { get; set; }
    public int TotalLikes { get; set; }
    public int TotalComments { get; set; }

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Display(Name = "Author / Pen Name")]
        public string AuthorName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        Username = user.UserName ?? string.Empty;
        DisplayName = string.IsNullOrWhiteSpace(user.AuthorName) ? Username : user.AuthorName;
        ProfileImagePath = user.ProfileImagePath;

        Input = new InputModel
        {
            PhoneNumber = user.PhoneNumber,
            AuthorName = user.AuthorName
        };

        await LoadStatsAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? profileImage)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (profileImage != null && !profileImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("profileImage", "Upload a valid image file for your profile photo.");
        }

        if (!ModelState.IsValid)
        {
            Username = user.UserName ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(user.AuthorName) ? Username : user.AuthorName;
            ProfileImagePath = user.ProfileImagePath;
            await LoadStatsAsync(user);
            return Page();
        }

        user.PhoneNumber = Input.PhoneNumber;
        user.AuthorName = (Input.AuthorName ?? string.Empty).Trim();

        if (profileImage != null && profileImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(profileImage.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
            var fileName = $"{Guid.NewGuid():N}{safeExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await profileImage.CopyToAsync(stream);

            user.ProfileImagePath = $"/uploads/profiles/{fileName}";
        }

        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    private async Task LoadStatsAsync(ApplicationUser user)
    {
        JoinedAt = user.JoinedAt;
        TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        var poems = await _db.Poems
            .Where(p => p.AuthorId == user.Id)
            .Select(p => new
            {
                p.Id,
                p.Views
            })
            .ToListAsync();

        WritingCount = poems.Count;
        TotalViews = poems.Sum(p => p.Views);

        var poemIds = poems.Select(p => p.Id).ToList();

        TotalLikes = poemIds.Count == 0
            ? 0
            : await _db.PoemLikes.CountAsync(l => poemIds.Contains(l.PoemId));

        TotalComments = poemIds.Count == 0
            ? 0
            : await _db.Comments.CountAsync(c => poemIds.Contains(c.PoemId) && !c.IsDeleted);
    }
}
