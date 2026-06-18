using EmailManager.Application.Interfaces;
using EmailManager.Application.Settings;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Persistence;
using EmailManager.Infrastructure.Repositories;
using EmailManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManager.Infrastructure;

/// <summary>
/// Infrastructure katmanının DI kayıtlarını merkezi olarak yöneten extension sınıfı.
/// Web projesinin Program.cs dosyasından tek bir çağrıyla tüm servisleri kaydeder.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure servislerini DI container'a kaydeder.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Uygulama yapılandırması.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core: SQLite (geliştirme), production'da provider değiştirilebilir.
        services.AddDbContext<AppDbContext>(options =>
        {
            var connStr = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=emailmanager.db";
            options.UseSqlite(connStr);

            // Production'da PostgreSQL için:
            // options.UseNpgsql(connStr);
        });

        // E-posta ayarları (IOptions<EmailSettings>)
        services.Configure<EmailSettings>(
            configuration.GetSection(nameof(EmailSettings)));

        // Repository kayıtları
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();

        // E-posta servisi (scoped: her request için ayrı instance)
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
