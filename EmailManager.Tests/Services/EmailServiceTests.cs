using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Enums;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Identity;
using EmailManager.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EmailManager.Tests.Services;

/// <summary>
/// <see cref="EmailService"/> için birim testleri.
/// Dahili @ptop.com adresleri için veri tabanına şifreli teslimat ile
/// harici adresler için SMTP üzerinden gönderim mantığını test eder.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IEmailLogRepository> _logRepositoryMock;
    private readonly Mock<IInternalMailRepository> _internalMailRepositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailSettings _settings;

    public EmailServiceTests()
    {
        _logRepositoryMock = new Mock<IEmailLogRepository>();
        _internalMailRepositoryMock = new Mock<IInternalMailRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _settings = new EmailSettings
        {
            Host = "invalid.smtp.host.test",
            Port = 587,
            UseSsl = false,
            UseStartTls = true,
            SenderEmail = "noreply@ptop.com",
            SenderName = "Ptop Mail",
            Password = "test-password"
        };
    }

    private EmailService CreateService(EmailSettings? settings = null)
    {
        var opts = Options.Create(settings ?? _settings);
        return new EmailService(
            opts,
            _logRepositoryMock.Object,
            _internalMailRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Test: SMTP bağlantısı başarısız olduğunda harici gönderim false döndürmeli.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenSmtpConnectionFails_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Konusu",
            Body = "Test mesajı",
            IsHtml = false
        };

        // Act
        var result = await service.SendAsync(message);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test: Alıcı @ptop.com uzantılı ise ve sistemde kayıtlıysa, dahili teslimat yapılmalı ve veriler şifrelenmelidir.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenRecipientIsInternal_EncryptsAndDeliversInternally()
    {
        // Arrange
        var service = CreateService();
        var message = new EmailMessage
        {
            From = "sender@ptop.com",
            To = "recipient@ptop.com",
            Subject = "Özel Konu",
            Body = "Çok gizli içerik",
            IsHtml = false
        };

        var recipientUser = new ApplicationUser { Email = "recipient@ptop.com", UserName = "recipient" };
        
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("recipient@ptop.com"))
            .ReturnsAsync(recipientUser);

        _encryptionServiceMock
            .Setup(e => e.Encrypt("Özel Konu"))
            .Returns("ENC_SUBJECT");

        _encryptionServiceMock
            .Setup(e => e.Encrypt("Çok gizli içerik"))
            .Returns("ENC_BODY");

        // Act
        var result = await service.SendAsync(message);

        // Assert
        Assert.True(result);

        // Veritabanına şifreli olarak eklendiğini doğrula
        _internalMailRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<InternalMail>(mail =>
                    mail.FromEmail == "sender@ptop.com" &&
                    mail.ToEmail == "recipient@ptop.com" &&
                    mail.EncryptedSubject == "ENC_SUBJECT" &&
                    mail.EncryptedBody == "ENC_BODY" &&
                    !mail.IsRead),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Log kaydı yazıldığını doğrula (Gizlilik sebebiyle konu '[Dahili Posta]' olarak maskelenir)
        _logRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<EmailLog>(log =>
                    log.To == "recipient@ptop.com" &&
                    log.Subject == "[Dahili Posta]" &&
                    log.Status == EmailStatus.Sent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Dahili alıcı sistemde kayıtlı değilse gönderim başarısız olmalı.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenRecipientIsInternalButNotFound_ReturnsFalseAndLogsFailure()
    {
        // Arrange
        var service = CreateService();
        var message = new EmailMessage
        {
            From = "sender@ptop.com",
            To = "nonexistent@ptop.com",
            Subject = "Deneme",
            Body = "Test",
            IsHtml = false
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("nonexistent@ptop.com"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await service.SendAsync(message);

        // Assert
        Assert.False(result);

        _logRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<EmailLog>(log =>
                    log.To == "nonexistent@ptop.com" &&
                    log.Status == EmailStatus.Failed &&
                    log.ErrorMessage == "Alıcı kullanıcı sistemde kayıtlı değil."),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
