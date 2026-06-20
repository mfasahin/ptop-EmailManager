using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Enums;
using EmailManager.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailManager.Infrastructure.Services;

/// <summary>
/// <see cref="IEmailService"/> implementasyonu.
/// @ptop.com adresleri dahili DB'ye (şifreli) yönlendirilir;
/// dış adresler MailKit SMTP üzerinden gönderilir.
/// </summary>
public class EmailService : IEmailService
{
    private const string InternalDomain = "ptop.com";

    private readonly EmailSettings _settings;
    private readonly IEmailLogRepository _logRepository;
    private readonly IInternalMailRepository _internalMailRepository;
    private readonly IEncryptionService _encryption;
    private readonly UserManager<Identity.ApplicationUser> _userManager;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        IEmailLogRepository logRepository,
        IInternalMailRepository internalMailRepository,
        IEncryptionService encryption,
        UserManager<Identity.ApplicationUser> userManager,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logRepository = logRepository;
        _internalMailRepository = internalMailRepository;
        _encryption = encryption;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        // Alıcı @ptop.com kullanıcısı mı? → Dahili teslim
        if (message.To.EndsWith($"@{InternalDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return await DeliverInternallyAsync(message, ct);
        }

        // Dış adres → SMTP
        return await SendViaSmtpAsync(message, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dahili teslim: @ptop.com → @ptop.com (DB'ye şifreli kayıt)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<bool> DeliverInternallyAsync(EmailMessage message, CancellationToken ct)
    {
        // Alıcı gerçekten sistemde kayıtlı mı?
        var recipient = await _userManager.FindByEmailAsync(message.To);
        if (recipient is null)
        {
            _logger.LogWarning("Dahili teslim başarısız: {To} bulunamadı.", message.To);

            // EmailLog'a da yaz (Failed)
            await _logRepository.AddAsync(new EmailLog
            {
                To = message.To,
                Subject = message.Subject,
                Status = EmailStatus.Failed,
                ErrorMessage = "Alıcı kullanıcı sistemde kayıtlı değil.",
                SentAt = DateTime.UtcNow
            }, CancellationToken.None);

            return false;
        }

        try
        {
            var internalMail = new InternalMail
            {
                FromEmail = message.From ?? _settings.SenderEmail,
                ToEmail = message.To,
                EncryptedSubject = _encryption.Encrypt(message.Subject),
                EncryptedBody = _encryption.Encrypt(message.Body),
                IsHtml = message.IsHtml,
                SentAt = DateTime.UtcNow
            };

            await _internalMailRepository.AddAsync(internalMail, ct);

            _logger.LogInformation("Dahili posta iletildi: {From} → {To}", internalMail.FromEmail, internalMail.ToEmail);

            // EmailLog kaydı (Subject plaintext burada log'a yazılmıyor — gizlilik)
            await _logRepository.AddAsync(new EmailLog
            {
                To = message.To,
                Subject = "[Dahili Posta]",
                Status = EmailStatus.Sent,
                SentAt = DateTime.UtcNow
            }, CancellationToken.None);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dahili posta kaydedilemedi: {To}", message.To);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SMTP teslim (dış adresler)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<bool> SendViaSmtpAsync(EmailMessage message, CancellationToken ct)
    {
        var log = new EmailLog
        {
            To = message.To,
            Subject = message.Subject,
            Status = EmailStatus.Pending,
            SentAt = DateTime.UtcNow
        };

        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;
            mimeMessage.Body = message.IsHtml
                ? new TextPart("html") { Text = message.Body }
                : new TextPart("plain") { Text = message.Body };

            using var client = new SmtpClient();

            SecureSocketOptions socketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _settings.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            _logger.LogInformation("SMTP bağlantısı: {Host}:{Port} [{Opts}]",
                _settings.Host, _settings.Port, socketOptions);

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrWhiteSpace(_settings.Password))
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password, ct);

            await client.SendAsync(mimeMessage, ct);
            await client.DisconnectAsync(true, ct);

            log.Status = EmailStatus.Sent;
            _logger.LogInformation("E-posta gönderildi (SMTP) → {To}", message.To);
            return true;
        }
        catch (Exception ex)
        {
            log.Status = EmailStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "E-posta gönderilemedi → {To}", message.To);
            return false;
        }
        finally
        {
            await _logRepository.AddAsync(log, CancellationToken.None);
        }
    }
}
