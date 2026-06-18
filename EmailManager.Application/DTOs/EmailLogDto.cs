using EmailManager.Domain.Enums;

namespace EmailManager.Application.DTOs;

/// <summary>
/// API response için düzleştirilmiş e-posta log DTO'su.
/// Domain entity'sini dışarıya sızdırmamak için kullanılır.
/// </summary>
public class EmailLogDto
{
    /// <summary>Log kaydının birincil anahtarı.</summary>
    public int Id { get; set; }

    /// <summary>Alıcı e-posta adresi.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>E-posta konu satırı.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gönderim durumu.</summary>
    public EmailStatus Status { get; set; }

    /// <summary>Durum adı (string). Örnek: "Sent", "Failed".</summary>
    public string StatusName => Status.ToString();

    /// <summary>Hata mesajı (yalnızca Failed durumunda dolu olur).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gönderim tarihi/saati (UTC).</summary>
    public DateTime SentAt { get; set; }
}
