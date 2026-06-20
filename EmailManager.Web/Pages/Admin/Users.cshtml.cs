using EmailManager.Infrastructure.Identity;
using EmailManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmailManager.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsersModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public List<UserViewModel> UsersList { get; private set; } = new();

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            UsersList.Add(new UserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = u.CreatedAt
            });
        }
    }

    /// <summary>
    /// Kullanıcının rolünü değiştirir (Admin <-> User).
    /// </summary>
    public async Task<IActionResult> OnPostToggleRoleAsync(string userId)
    {
        var currentAdminId = _userManager.GetUserId(User);
        if (userId == currentAdminId)
        {
            return new JsonResult(new { success = false, message = "Kendi yöneticilik rolünüzü kaldıramazsınız!" })
            { StatusCode = StatusCodes.Status400BadRequest };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var isAlreadyAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAlreadyAdmin)
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            await _userManager.AddToRoleAsync(user, "User");
            _logger.LogInformation("Kullanıcı {Email} rolü User yapıldı.", user.Email);
            return new JsonResult(new { success = true, newRole = "User" });
        }
        else
        {
            await _userManager.RemoveFromRoleAsync(user, "User");
            await _userManager.AddToRoleAsync(user, "Admin");
            _logger.LogInformation("Kullanıcı {Email} rolü Admin yapıldı.", user.Email);
            return new JsonResult(new { success = true, newRole = "Admin" });
        }
    }

    /// <summary>
    /// Kullanıcıyı sistemden siler.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var currentAdminId = _userManager.GetUserId(User);
        if (userId == currentAdminId)
        {
            return new JsonResult(new { success = false, message = "Kendi hesabınızı silemezsiniz!" })
            { StatusCode = StatusCodes.Status400BadRequest };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("Kullanıcı {Email} sistemden silindi.", user.Email);
            return new JsonResult(new { success = true });
        }

        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        return new JsonResult(new { success = false, message = errors })
        { StatusCode = StatusCodes.Status500InternalServerError };
    }
}
