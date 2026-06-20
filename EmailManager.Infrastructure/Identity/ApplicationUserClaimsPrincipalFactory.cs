using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EmailManager.Infrastructure.Identity;

/// <summary>
/// Kullanıcı oturumu açıldığında Claims'e FullName ve Email ekler.
/// Bu sayede Razor Pages'da <c>User.FindFirst("FullName")</c> ile erişilebilir.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Tam ad claim'i ekle
        identity.AddClaim(new Claim("FullName", user.FullName));

        // Kurumsal e-posta claim'i ekle
        if (!string.IsNullOrEmpty(user.Email))
            identity.AddClaim(new Claim("PtopEmail", user.Email));

        return identity;
    }
}
