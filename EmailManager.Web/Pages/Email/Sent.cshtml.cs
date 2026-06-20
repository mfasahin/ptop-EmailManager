using EmailManager.Application.DTOs;
using EmailManager.Application.Interfaces;
using EmailManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmailManager.Web.Pages.Email;

[Authorize]
public class SentModel : PageModel
{
    private readonly IInternalMailRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SentModel> _logger;

    private const int PageSize = 15;

    public SentModel(
        IInternalMailRepository repository,
        IEncryptionService encryptionService,
        ILogger<SentModel> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public List<InternalMailDto> Mails { get; private set; } = new();
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = Math.Max(1, page);
        var email = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com";

        var (items, total) = await _repository.GetSentAsync(email, CurrentPage, PageSize, HttpContext.RequestAborted);

        TotalCount = total;
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);

        foreach (var item in items)
        {
            var decryptedSubject = "Şifreli Konu (Çözülemedi)";
            try
            {
                decryptedSubject = _encryptionService.Decrypt(item.EncryptedSubject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gönderilen dahili mail (ID: {Id}) konusu çözülemedi.", item.Id);
            }

            Mails.Add(new InternalMailDto
            {
                Id = item.Id,
                FromEmail = item.FromEmail,
                ToEmail = item.ToEmail,
                Subject = decryptedSubject,
                Body = string.Empty, // Liste sayfasında gövde çözülmez
                IsHtml = item.IsHtml,
                IsRead = item.IsRead,
                SentAt = item.SentAt
            });
        }
    }

    /// <summary>
    /// AJAX ile çağrılan detay endpoint'i.
    /// </summary>
    public async Task<IActionResult> OnGetDetailsAsync(int id)
    {
        var email = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com";
        var mail = await _repository.GetByIdAsync(id, HttpContext.RequestAborted);

        if (mail is null || (mail.FromEmail != email && !User.IsInRole("Admin")))
        {
            return NotFound(new { message = "Mesaj bulunamadı veya erişim yetkiniz yok." });
        }

        var subject = "Şifreli Konu";
        var body = "Şifreli Gövde";
        try
        {
            subject = _encryptionService.Decrypt(mail.EncryptedSubject);
            body = _encryptionService.Decrypt(mail.EncryptedBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gönderilen mesaj (ID: {Id}) deşifre edilemedi.", id);
            body = "E-posta içeriği güvenli anahtarla deşifre edilemedi.";
        }

        return new JsonResult(new
        {
            id = mail.Id,
            fromEmail = mail.FromEmail,
            toEmail = mail.ToEmail,
            subject = subject,
            body = body,
            isHtml = mail.IsHtml,
            sentAt = mail.SentAt.ToString("g")
        });
    }
}
