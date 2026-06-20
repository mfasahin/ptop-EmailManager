namespace EmailManager.Domain.Entities;

/// <summary>
/// @ptop.com kullanıcıları arasında gönderilen dahili e-posta mesajı.
/// Konu ve gövde AES-256 ile şifreli saklanır; IV ciphertext ile birlikte
/// "IV_BASE64:CIPHER_BASE64" formatında tutulur.
/// </summary>
public class InternalMail
{
    public int Id { get; set; }

    /// <summary>Gönderenin e-posta adresi (örn. fatih@ptop.com).</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Alıcının e-posta adresi (örn. ali@ptop.com).</summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>AES-256 şifreli konu (IV:Cipher formatında).</summary>
    public string EncryptedSubject { get; set; } = string.Empty;

    /// <summary>AES-256 şifreli gövde (IV:Cipher formatında).</summary>
    public string EncryptedBody { get; set; } = string.Empty;

    /// <summary>HTML içerik mi?</summary>
    public bool IsHtml { get; set; }

    /// <summary>Gönderilme tarihi (UTC).</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Alıcı tarafından okundu mu?</summary>
    public bool IsRead { get; set; }

    /// <summary>Alıcı bu mesajı sildiyse true.</summary>
    public bool IsDeletedByRecipient { get; set; }

    /// <summary>Gönderici bu mesajı gönderilenlerden sildiyse true.</summary>
    public bool IsDeletedBySender { get; set; }
}
