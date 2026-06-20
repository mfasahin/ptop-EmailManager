using EmailManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EmailManager.Web.Pages.Account;

/// <summary>
/// Login sayfasının PageModel'i.
/// ASP.NET Core Identity <see cref="SignInManager{TUser}"/> ile cookie auth sağlar.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/Email/Send");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Email/Send");

        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Username, Input.Password,
            isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("Kullanıcı giriş yaptı: {Username}", Input.Username);
            
            var user = await _signInManager.UserManager.FindByNameAsync(Input.Username);
            if (user is not null && await _signInManager.UserManager.IsInRoleAsync(user, "Admin") && returnUrl.EndsWith("/Email/Send"))
            {
                returnUrl = Url.Content("~/Admin/Index");
            }
            
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
        return Page();
    }
}
