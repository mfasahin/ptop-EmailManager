using EmailManager.Domain.Entities;
using EmailManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailManager.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı.
/// ASP.NET Core Identity ile birlikte çalışır (<see cref="ApplicationUser"/>).
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>E-posta gönderim log kayıtları.</summary>
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;

    /// <summary>@ptop.com kullanıcıları arasındaki dahili şifreli postalar.</summary>
    public DbSet<InternalMail> InternalMails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ApplicationUser ek alanlarını yapılandır
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EmailLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.To).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<InternalMail>(entity =>
        {
            entity.ToTable("InternalMails");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.FromEmail).IsRequired().HasMaxLength(256);
            entity.Property(m => m.ToEmail).IsRequired().HasMaxLength(256);
            entity.Property(m => m.EncryptedSubject).IsRequired();
            entity.Property(m => m.EncryptedBody).IsRequired();
            entity.HasIndex(m => m.ToEmail);
            entity.HasIndex(m => m.FromEmail);
            entity.HasIndex(m => m.SentAt);
        });
    }

    /// <summary>
    /// EF Core CLI migration araçları için design-time factory.
    /// </summary>
    public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=emailmanager_design.db");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
