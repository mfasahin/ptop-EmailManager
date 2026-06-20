using EmailManager.Domain.Entities;

namespace EmailManager.Domain.Interfaces;

/// <summary>
/// Dahili posta sistemi için repository sözleşmesi.
/// Gelen kutusu, gönderilenler ve yönetim operasyonlarını tanımlar.
/// </summary>
public interface IInternalMailRepository
{
    /// <summary>Yeni dahili mail kaydeder.</summary>
    Task AddAsync(InternalMail mail, CancellationToken ct = default);

    /// <summary>Belirli bir kullanıcının gelen kutusunu sayfalı getirir.</summary>
    Task<(IReadOnlyList<InternalMail> Items, int Total)> GetInboxAsync(
        string toEmail, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Belirli bir kullanıcının gönderilenleri sayfalı getirir.</summary>
    Task<(IReadOnlyList<InternalMail> Items, int Total)> GetSentAsync(
        string fromEmail, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Tüm mesajları sayfalı getirir (admin).</summary>
    Task<(IReadOnlyList<InternalMail> Items, int Total)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>ID ile tek mail getirir.</summary>
    Task<InternalMail?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Mesajı okundu olarak işaretler.</summary>
    Task MarkAsReadAsync(int id, CancellationToken ct = default);

    /// <summary>Alıcının gelen kutusundaki okunmamış mesaj sayısı.</summary>
    Task<int> GetUnreadCountAsync(string toEmail, CancellationToken ct = default);

    /// <summary>Mesajı alıcı açısından siler (soft delete).</summary>
    Task DeleteByRecipientAsync(int id, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
