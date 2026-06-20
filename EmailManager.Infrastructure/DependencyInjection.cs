using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Identity;
using EmailManager.Infrastructure.Repositories;
using EmailManager.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManager.Infrastructure;

/// <summary>
/// Infrastructure katmanının DI kayıtlarını merkezi olarak yöneten extension sınıfı.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core: SQLite (geliştirme)
        services.AddDbContext<Persistence.AppDbContext>(options =>
        {
            var connStr = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=emailmanager.db";
            options.UseSqlite(connStr);
        });

        // E-posta ayarları (IOptions<EmailSettings>)
        services.Configure<EmailSettings>(
            configuration.GetSection(nameof(EmailSettings)));

        // Şifreleme servisi (AES-256)
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Repository kayıtları
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<IInternalMailRepository, InternalMailRepository>();

        // E-posta servisi
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
