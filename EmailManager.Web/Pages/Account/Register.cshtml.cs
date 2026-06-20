using EmailManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EmailManager.Web.Pages.Account;

/// <summary>
/// Yeni kullanıcı kaydı PageModel'i.
/// E-posta adresi otomatik olarak @ptop.com uzantılı oluşturulur.
/// </summary>
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    private const string EmailDomain = "ptop.com";

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının seçtiği e-posta ön eki.
        /// Sistem buna @ptop.com ekleyerek tam adresi oluşturur.
        /// Örnek: "ali.veli" → "ali.veli@ptop.com"
        /// </summary>
        [Required(ErrorMessage = "E-posta ön eki zorunludur.")]
        [MaxLength(64, ErrorMessage = "E-posta ön eki en fazla 64 karakter olabilir.")]
        [RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9._-]*[a-zA-Z0-9]$|^[a-zA-Z0-9]$",
            ErrorMessage = "Yalnızca harf, rakam, nokta (.), tire (-) ve alt çizgi (_) kullanılabilir.")]
        [Display(Name = "E-posta ön eki")]
        public string EmailPrefix { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        [Display(Name = "Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var fullEmail = $"{Input.EmailPrefix.ToLower()}@{EmailDomain}";
        var userName = Input.EmailPrefix.ToLower();

        // E-posta veya kullanıcı adı zaten kullanılıyor mu?
        var existingUser = await _userManager.FindByEmailAsync(fullEmail);
        if (existingUser is not null)
        {
            ModelState.AddModelError("Input.EmailPrefix",
                "Bu e-posta adresi zaten kullanımda. Farklı bir ön ek seçin.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = fullEmail,
            EmailConfirmed = true,   // @ptop.com domain'i güvenilir kabul edilir
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Email}", fullEmail);

            // Yeni kullanıcıya "User" rolü ata
            await _userManager.AddToRoleAsync(user, "User");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Email/Send");
        }

        // Identity hata mesajlarını form'a ekle
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
