namespace EmailManager.Domain.Entities;

/// <summary>
/// Gönderilecek e-posta içeriğini taşıyan POCO sınıfı.
/// EF Core entity değildir; yalnızca servis katmanları arasında veri taşımak için kullanılır.
/// </summary>
public class EmailMessage
{
    /// <summary>Gönderenin e-posta adresi (dahili postalar için zorunlu).</summary>
    public string? From { get; set; }

    /// <summary>Alıcının e-posta adresi.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>E-postanın konu satırı.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>E-posta gövdesi. HTML veya düz metin olabilir.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> ise gövde HTML olarak yorumlanır; 
    /// <c>false</c> ise düz metin olarak gönderilir.
    /// </summary>
    public bool IsHtml { get; set; } = false;
}
