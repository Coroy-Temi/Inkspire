using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Models;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .Select(u => new AdminUserVm
            {
                Id = u.Id,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                AuthorName = u.AuthorName
            })
            .ToListAsync();

        return View(users);
    }
}
