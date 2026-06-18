namespace EmailManager.Domain.Enums;

/// <summary>
/// E-posta gönderim durumunu temsil eden enum.
/// </summary>
public enum EmailStatus
{
    /// <summary>Gönderim bekleniyor (henüz işleme alınmadı).</summary>
    Pending = 0,

    /// <summary>E-posta başarıyla gönderildi.</summary>
    Sent = 1,

    /// <summary>Gönderim başarısız oldu.</summary>
    Failed = 2
}
