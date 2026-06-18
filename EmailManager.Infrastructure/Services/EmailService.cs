using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Enums;
using EmailManager.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailManager.Infrastructure.Services;

/// <summary>
/// <see cref="IEmailService"/> arayüzünün MailKit tabanlı implementasyonu.
/// System.Net.Mail.SmtpClient kullanılmamaktadır.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IEmailLogRepository _logRepository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        IEmailLogRepository logRepository,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logRepository = logRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        // Log kaydı başlangıçta Pending olarak oluşturulur.
        var log = new EmailLog
        {
            To = message.To,
            Subject = message.Subject,
            Status = EmailStatus.Pending,
            SentAt = DateTime.UtcNow
        };

        try
        {
            // MimeMessage oluştur
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;

            // Gövde içeriği: HTML veya düz metin
            mimeMessage.Body = message.IsHtml
                ? new TextPart("html") { Text = message.Body }
                : new TextPart("plain") { Text = message.Body };

            // MailKit async SmtpClient kullanımı
            using var client = new SmtpClient();

            // Port'a göre otomatik SSL/STARTTLS seçimi
            // Port 465 → SslOnConnect (doğrudan SSL)
            // Port 587 → StartTls (STARTTLS negotiate)
            // Diğer → None veya ayara göre
            SecureSocketOptions socketOptions;
            if (_settings.UseSsl)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (_settings.UseStartTls)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                socketOptions = SecureSocketOptions.None;
            }

            _logger.LogInformation("SMTP bağlantısı kuruluyor: {Host}:{Port} [{Options}]",
                _settings.Host, _settings.Port, socketOptions);

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password, ct);
            await client.SendAsync(mimeMessage, ct);
            await client.DisconnectAsync(true, ct);

            log.Status = EmailStatus.Sent;
            _logger.LogInformation("E-posta başarıyla gönderildi → {To}", message.To);

            // TODO: Polly ile WaitAndRetryAsync eklemek için bu noktada retry policy uygulanabilir.
            // Örnek:
            // await retryPolicy.ExecuteAsync(async () => { ... SendAsync ... });

            return true;
        }
        catch (Exception ex)
        {
            // Exception fırlatılmaz, log kaydedilir ve false döndürülür.
            log.Status = EmailStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "E-posta gönderilemedi → {To}: {Error}", message.To, ex.Message);
            return false;
        }
        finally
        {
            // Her durumda (başarı/hata) log kaydı veritabanına yazılır.
            await _logRepository.AddAsync(log, CancellationToken.None);
        }
    }
}
