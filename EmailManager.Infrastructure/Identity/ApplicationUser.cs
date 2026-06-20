using Microsoft.AspNetCore.Identity;

namespace EmailManager.Infrastructure.Identity;

/// <summary>
/// Uygulamaya özel kullanıcı sınıfı. ASP.NET Core Identity'nin
/// <see cref="IdentityUser"/>'ını genişleterek ad, soyad ve oluşturulma
/// tarihi bilgilerini ekler.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Kullanıcının adı.</summary>
    public string? FirstName { get; set; }

    /// <summary>Kullanıcının soyadı.</summary>
    public string? LastName { get; set; }

    /// <summary>Hesabın oluşturulduğu tarih (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ad ve soyadı birleştirerek tam adı döndürür.
    /// Ad/soyad yoksa kullanıcı adını döndürür.
    /// </summary>
    public string FullName =>
        !string.IsNullOrWhiteSpace(FirstName)
            ? $"{FirstName} {LastName}".Trim()
            : UserName ?? string.Empty;

    /// <summary>
    /// Kullanıcının @ptop.com uzantılı kurumsal e-posta adresi.
    /// Örnek: kullanici@ptop.com
    /// </summary>
    public string PtopEmail => Email ?? string.Empty;
}
