using EmailManager.Domain.Entities;

namespace EmailManager.Domain.Interfaces;

/// <summary>
/// E-posta log kayıtlarına erişim için repository sözleşmesi.
/// Infrastructure katmanında EF Core ile implemente edilir,
/// testlerde Moq ile kolayca mock'lanabilir.
/// </summary>
public interface IEmailLogRepository
{
    /// <summary>
    /// Yeni bir <see cref="EmailLog"/> kaydı veritabanına ekler.
    /// </summary>
    /// <param name="log">Eklenecek log kaydı.</param>
    /// <param name="ct">İptal token'ı.</param>
    Task AddAsync(EmailLog log, CancellationToken ct = default);

    /// <summary>
    /// Sayfalı şekilde log kayıtlarını döndürür; en yeniden eskiye sıralanır.
    /// </summary>
    /// <param name="page">Sayfa numarası (1 tabanlı).</param>
    /// <param name="size">Sayfa başına kayıt sayısı.</param>
    /// <param name="ct">İptal token'ı.</param>
    Task<(IEnumerable<EmailLog> Items, int TotalCount)> GetPagedAsync(
        int page, int size, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen id'ye sahip log kaydını döndürür; bulunamazsa <c>null</c> döner.
    /// </summary>
    /// <param name="id">Log kaydının birincil anahtarı.</param>
    /// <param name="ct">İptal token'ı.</param>
    Task<EmailLog?> GetByIdAsync(int id, CancellationToken ct = default);
}
