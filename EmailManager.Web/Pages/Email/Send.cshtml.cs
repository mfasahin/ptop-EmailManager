using EmailManager.Application.Interfaces;
using EmailManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EmailManager.Web.Pages.Email;

/// <summary>
/// E-posta gönderme sayfasının PageModel'i.
/// POST isteği Fetch API üzerinden JSON olarak gelir, yanıt da JSON döndürülür.
/// </summary>
[Authorize]
[ValidateAntiForgeryToken]
public class SendModel : PageModel
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendModel> _logger;

    public SendModel(IEmailService emailService, ILogger<SendModel> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public void OnGet() { }

    /// <summary>
    /// Fetch API'den gelen JSON POST isteğini işler.
    /// </summary>
    public async Task<IActionResult> OnPostAsync([FromBody] SendEmailInput input)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return new JsonResult(new { message = string.Join("; ", errors) })
            { StatusCode = StatusCodes.Status400BadRequest };
        }

        var message = new EmailMessage
        {
            From = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com",
            To = input.To,
            Subject = input.Subject,
            Body = input.Body,
            IsHtml = input.IsHtml
        };

        var success = await _emailService.SendAsync(message, HttpContext.RequestAborted);

        if (success)
        {
            _logger.LogInformation("Web üzerinden e-posta gönderildi → {To}", input.To);
            return new JsonResult(new { message = "E-posta başarıyla gönderildi." });
        }

        return new JsonResult(new { message = "E-posta gönderilemedi. Lütfen log kayıtlarını kontrol edin." })
        { StatusCode = StatusCodes.Status500InternalServerError };
    }

    /// <summary>Form/JSON input modeli.</summary>
    public class SendEmailInput
    {
        [Required][EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required][MaxLength(512)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = false;
    }
}
