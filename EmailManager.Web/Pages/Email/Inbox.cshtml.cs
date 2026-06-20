using EmailManager.Application.DTOs;
using EmailManager.Application.Interfaces;
using EmailManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmailManager.Web.Pages.Email;

[Authorize]
public class InboxModel : PageModel
{
    private readonly IInternalMailRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InboxModel> _logger;

    private const int PageSize = 15;

    public InboxModel(
        IInternalMailRepository repository,
        IEncryptionService encryptionService,
        ILogger<InboxModel> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public List<InternalMailDto> Mails { get; private set; } = new();
    public int UnreadCount { get; private set; }
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = Math.Max(1, page);
        var email = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com";

        UnreadCount = await _repository.GetUnreadCountAsync(email, HttpContext.RequestAborted);
        var (items, total) = await _repository.GetInboxAsync(email, CurrentPage, PageSize, HttpContext.RequestAborted);

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
                _logger.LogWarning(ex, "Dahili mail (ID: {Id}) konusu çözülemedi.", item.Id);
            }

            Mails.Add(new InternalMailDto
            {
                Id = item.Id,
                FromEmail = item.FromEmail,
                ToEmail = item.ToEmail,
                Subject = decryptedSubject,
                Body = string.Empty, // Gövdeyi liste sayfasında çözmüyoruz
                IsHtml = item.IsHtml,
                IsRead = item.IsRead,
                SentAt = item.SentAt
            });
        }
    }

    /// <summary>
    /// AJAX ile çağrılan detay endpoint'i. Mesajı çözer ve okundu olarak işaretler.
    /// </summary>
    public async Task<IActionResult> OnGetDetailsAsync(int id)
    {
        var email = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com";
        var mail = await _repository.GetByIdAsync(id, HttpContext.RequestAborted);

        if (mail is null || (mail.ToEmail != email && !User.IsInRole("Admin")))
        {
            return NotFound(new { message = "Mesaj bulunamadı veya erişim yetkiniz yok." });
        }

        // Okundu olarak işaretle (eğer alıcı ise)
        if (mail.ToEmail == email && !mail.IsRead)
        {
            await _repository.MarkAsReadAsync(id, HttpContext.RequestAborted);
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
            _logger.LogError(ex, "Mesaj (ID: {Id}) deşifre edilemedi.", id);
            body = "E-posta içeriği güvenli anahtarla deşifre edilemedi. Veritabanındaki veri bozulmuş olabilir.";
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

    /// <summary>
    /// E-postayı silme (soft-delete).
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var email = User.FindFirst("PtopEmail")?.Value ?? $"{User.Identity?.Name}@ptop.com";
        var mail = await _repository.GetByIdAsync(id, HttpContext.RequestAborted);

        if (mail is null || mail.ToEmail != email)
        {
            return BadRequest(new { message = "Geçersiz işlem veya mesaj bulunamadı." });
        }

        await _repository.DeleteByRecipientAsync(id, HttpContext.RequestAborted);
        return new JsonResult(new { success = true });
    }
}
