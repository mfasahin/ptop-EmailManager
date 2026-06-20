using EmailManager.Application.DTOs;
using EmailManager.Application.Interfaces;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmailManager.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AllMailsModel : PageModel
{
    private readonly IInternalMailRepository _internalRepository;
    private readonly IEmailLogRepository _externalRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AllMailsModel> _logger;

    private const int PageSize = 15;

    public AllMailsModel(
        IInternalMailRepository internalRepository,
        IEmailLogRepository externalRepository,
        IEncryptionService encryptionService,
        ILogger<AllMailsModel> logger)
    {
        _internalRepository = internalRepository;
        _externalRepository = externalRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public List<InternalMailDto> InternalMails { get; private set; } = new();
    public List<EmailLog> ExternalLogs { get; private set; } = new();

    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }
    public string ActiveTab { get; private set; } = "internal";

    public async Task OnGetAsync(string tab = "internal", int page = 1)
    {
        ActiveTab = tab == "external" ? "external" : "internal";
        CurrentPage = Math.Max(1, page);

        if (ActiveTab == "internal")
        {
            var (items, total) = await _internalRepository.GetAllAsync(CurrentPage, PageSize, HttpContext.RequestAborted);
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
                    _logger.LogWarning(ex, "Dahili mail (ID: {Id}) konusu deşifre edilemedi.", item.Id);
                }

                InternalMails.Add(new InternalMailDto
                {
                    Id = item.Id,
                    FromEmail = item.FromEmail,
                    ToEmail = item.ToEmail,
                    Subject = decryptedSubject,
                    Body = string.Empty, // Gövdeyi detaya tıklayınca AJAX ile getireceğiz
                    IsHtml = item.IsHtml,
                    IsRead = item.IsRead,
                    SentAt = item.SentAt
                });
            }
        }
        else
        {
            var (items, total) = await _externalRepository.GetPagedAsync(CurrentPage, PageSize, HttpContext.RequestAborted);
            TotalCount = total;
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
            ExternalLogs = items.ToList();
        }
    }

    /// <summary>
    /// Dahili e-posta içeriğini deşifre edip getiren AJAX endpoint'i.
    /// </summary>
    public async Task<IActionResult> OnGetDecryptDetailsAsync(int id)
    {
        var mail = await _internalRepository.GetByIdAsync(id, HttpContext.RequestAborted);
        if (mail is null)
        {
            return NotFound(new { message = "Mesaj bulunamadı." });
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
            _logger.LogError(ex, "Yönetici dahili mesaj deşifre hatası. ID: {Id}", id);
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
