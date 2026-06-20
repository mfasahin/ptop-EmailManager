namespace EmailManager.Application.DTOs;

/// <summary>
/// Şifresi çözülmüş dahili mail verisi — sayfa modellerine aktarılır.
/// </summary>
public class InternalMailDto
{
    public int Id { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeletedByRecipient { get; set; }
    public bool IsDeletedBySender { get; set; }

    /// <summary>Gönderenin görünen adı (varsa).</summary>
    public string FromDisplayName { get; set; } = string.Empty;
}
