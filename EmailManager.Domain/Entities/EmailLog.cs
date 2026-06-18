using EmailManager.Domain.Enums;

namespace EmailManager.Domain.Entities;

/// <summary>
/// Bir e-posta gönderim denemesinin kayıt entity'si.
/// EF Core tarafından <c>EmailLogs</c> tablosuna karşılık gelir.
/// </summary>
public class EmailLog
{
    /// <summary>Birincil anahtar.</summary>
    public int Id { get; set; }

    /// <summary>E-postanın gönderildiği alıcı adresi.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>E-postanın konu satırı.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gönderim durumu (Pending, Sent, Failed).</summary>
    public EmailStatus Status { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// Gönderim başarısız olduğunda hata mesajı.
    /// Başarılı gönderimde <c>null</c>'dır.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gönderim tarih/saat bilgisi (UTC).</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
