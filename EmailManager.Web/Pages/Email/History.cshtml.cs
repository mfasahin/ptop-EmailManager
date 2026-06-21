using EmailManager.Domain.Entities;
using EmailManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmailManager.Web.Pages.Email;

/// <summary>
/// Gönderim geçmişi sayfasının PageModel'i.
/// Server-side sayfalama Skip/Take ile uygulanır.
/// </summary>
[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly IEmailLogRepository _repository;
    private readonly ILogger<HistoryModel> _logger;

    /// <summary>Sayfa başına gösterilecek kayıt sayısı.</summary>
    private const int PageSize = 20;

    public HistoryModel(IEmailLogRepository repository, ILogger<HistoryModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Mevcut sayfadaki log kayıtları.</summary>
    public IEnumerable<EmailLog> Logs { get; private set; } = [];

    /// <summary>Toplam kayıt sayısı.</summary>
    public int TotalCount { get; private set; }

    /// <summary>Toplam sayfa sayısı.</summary>
    public int TotalPages { get; private set; }

    /// <summary>Aktif sayfa numarası.</summary>
    public int CurrentPage { get; private set; }

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = Math.Max(1, page);

        var (items, total) = await _repository.GetPagedAsync(
            CurrentPage, PageSize, HttpContext.RequestAborted);

        Logs = items;
        TotalCount = total;
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);

        _logger.LogDebug("Geçmiş sayfası yüklendi: Sayfa={Page}, Toplam={Total}",
            CurrentPage, TotalCount);
    }
}
