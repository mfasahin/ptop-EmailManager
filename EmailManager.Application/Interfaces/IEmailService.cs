using EmailManager.Domain.Entities;

namespace EmailManager.Application.Interfaces;

/// <summary>
/// E-posta gönderim servisinin sözleşmesi.
/// Infrastructure katmanında MailKit kullanılarak implemente edilir.
/// Testlerde Moq ile kolayca mock'lanabilir.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Belirtilen <see cref="EmailMessage"/>'ı SMTP üzerinden gönderir.
    /// Gönderim sonucu (başarı/başarısız) <see cref="EmailLog"/> kaydı olarak veritabanına yazılır.
    /// </summary>
    /// <param name="message">Gönderilecek e-posta içeriği.</param>
    /// <param name="ct">İptal token'ı.</param>
    /// <returns>Gönderim başarılıysa <c>true</c>, başarısızsa <c>false</c>.</returns>
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}
