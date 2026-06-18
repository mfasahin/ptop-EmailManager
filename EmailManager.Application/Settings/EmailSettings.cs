namespace EmailManager.Application.Settings;

/// <summary>
/// SMTP e-posta sunucu yapılandırması.
/// appsettings.json'daki "EmailSettings" bölümünden <c>IOptions&lt;EmailSettings&gt;</c>
/// pattern'i ile inject edilir.
/// </summary>
public class EmailSettings
{
    /// <summary>SMTP sunucu adresi. Örnek: smtp.gmail.com</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP port numarası. 465 (SSL) veya 587 (STARTTLS).</summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Doğrudan SSL bağlantısı kullanılsın mı?
    /// Port 465 için <c>true</c>, port 587 için <c>false</c> olarak ayarlanır.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// STARTTLS kullanılsın mı?
    /// Port 587 için önerilir. <see cref="UseSsl"/> ile birlikte kullanılmaz.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Gönderici e-posta adresi.</summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Gönderici görünen adı.</summary>
    public string SenderName { get; set; } = "Email Manager";

    /// <summary>SMTP kimlik doğrulama şifresi. Production'da Secret Manager kullanın.</summary>
    public string Password { get; set; } = string.Empty;
}
