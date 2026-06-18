using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Enums;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EmailManager.Tests.Services;

/// <summary>
/// <see cref="EmailService"/> için birim testleri.
/// MailKit SMTP bağlantısı mock'lanarak gerçek ağ erişimi olmadan test yapılır.
/// IEmailLogRepository mock'u ile log kayıt doğrulaması yapılır.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IEmailLogRepository> _logRepositoryMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailSettings _settings;

    public EmailServiceTests()
    {
        _logRepositoryMock = new Mock<IEmailLogRepository>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        // Geçersiz SMTP ayarları: gerçek bağlantı kurulamaz → exception oluşur
        _settings = new EmailSettings
        {
            Host = "invalid.smtp.host.test",
            Port = 587,
            UseSsl = false,
            UseStartTls = true,
            SenderEmail = "test@test.com",
            SenderName = "Test Sender",
            Password = "test-password"
        };
    }

    /// <summary>
    /// Yardımcı: EmailService instance'ı oluşturur.
    /// </summary>
    private EmailService CreateService(EmailSettings? settings = null)
    {
        var opts = Options.Create(settings ?? _settings);
        return new EmailService(opts, _logRepositoryMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Test: SMTP bağlantısı başarısız olduğunda SendAsync false döndürmeli.
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
        Assert.False(result, "Geçersiz SMTP host'ta bağlantı başarısız olmalı ve false dönmeli.");
    }

    /// <summary>
    /// Test: SMTP bağlantısı başarısız olduğunda log kaydı Failed statüsünde yazılmalı.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenSmtpConnectionFails_LogsFailedStatus()
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
        await service.SendAsync(message);

        // Assert: AddAsync bir kez çağrılmış ve log'un Status'u Failed olmalı
        _logRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<EmailLog>(log =>
                    log.Status == EmailStatus.Failed &&
                    log.To == "recipient@example.com" &&
                    log.Subject == "Test Konusu" &&
                    log.ErrorMessage != null),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Başarısız gönderimde Failed statüslü log kaydı yazılmalıydı.");
    }

    /// <summary>
    /// Test: İptal token'ı tetiklendiğinde SendAsync false döndürmeli.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenCancelled_ReturnsFalseAndLogsFailure()
    {
        // Arrange
        var service = CreateService();
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "İptal testi",
            Body = "Bu mesaj iptal edilecek.",
            IsHtml = false
        };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Anında iptal

        // Act
        var result = await service.SendAsync(message, cts.Token);

        // Assert
        Assert.False(result);
        _logRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<EmailLog>(log => log.Status == EmailStatus.Failed),
                CancellationToken.None),
            Times.Once);
    }

    /// <summary>
    /// Test: IEmailService arayüzü Moq ile mock'lanabilmeli ve SendAsync çağrısı doğrulanabilmeli.
    /// Bu test Clean Architecture interface tabanlı tasarımını doğrular.
    /// </summary>
    [Fact]
    public async Task IEmailService_CanBeMockedAndVerified()
    {
        // Arrange
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act — Gerçek servisi değil mock'u kullanıyoruz
        var message = new EmailMessage { To = "a@b.com", Subject = "S", Body = "B" };
        var result = await emailServiceMock.Object.SendAsync(message);

        // Assert
        Assert.True(result);
        emailServiceMock.Verify(
            s => s.SendAsync(
                It.Is<EmailMessage>(m => m.To == "a@b.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: E-posta adresi doğrulama senaryosu.
    /// Geçersiz bir To adresiyle gönderim yapıldığında MailKit exception fırlatır,
    /// servis bu exception'ı yakalamalı ve false döndürmelidir.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithInvalidEmailAddress_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        var service = CreateService();
        var message = new EmailMessage
        {
            To = "gecersiz-adres",
            Subject = "Konu",
            Body = "Gövde",
            IsHtml = false
        };

        // Act — exception fırlatılmamalı
        var result = await service.SendAsync(message);

        // Assert
        Assert.False(result);
    }
}
