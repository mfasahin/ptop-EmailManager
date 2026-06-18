using EmailManager.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailManager.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı.
/// ASP.NET Core Identity ile birlikte çalışır.
/// </summary>
public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>E-posta gönderim log kayıtları.</summary>
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EmailLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.To).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.Property(e => e.Status).HasConversion<string>();
            // Sorgu performansı için index
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.Status);
        });
    }

    /// <summary>
    /// EF Core CLI migration araçları için design-time factory.
    /// <c>dotnet ef migrations add</c> komutu bu sınıfı kullanır.
    /// </summary>
    public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // Design-time için SQLite varsayılan; production'da değiştirilir.
            optionsBuilder.UseSqlite("Data Source=emailmanager_design.db");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
