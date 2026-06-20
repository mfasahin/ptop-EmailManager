using EmailManager.Domain.Entities;
using EmailManager.Infrastructure.Identity;
using EmailManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmailManager.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public int TotalUsersCount { get; private set; }
    public int AdminUsersCount { get; private set; }
    public int RegularUsersCount { get; private set; }
    
    public int TotalInternalMailsCount { get; private set; }
    public int TotalExternalMailsCount { get; private set; }
    
    public List<ApplicationUser> RecentUsers { get; private set; } = new();
    public List<EmailLog> RecentExternalLogs { get; private set; } = new();
    public List<InternalMail> RecentInternalMails { get; private set; } = new();

    public async Task OnGetAsync()
    {
        // Kullanıcı Sayıları
        TotalUsersCount = await _userManager.Users.CountAsync(HttpContext.RequestAborted);
        
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is not null)
        {
            AdminUsersCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id, HttpContext.RequestAborted);
        }
        RegularUsersCount = TotalUsersCount - AdminUsersCount;

        // Posta Sayıları
        TotalInternalMailsCount = await _db.InternalMails.CountAsync(HttpContext.RequestAborted);
        TotalExternalMailsCount = await _db.EmailLogs.CountAsync(HttpContext.RequestAborted);

        // Son Kayıt Olan 5 Kullanıcı
        RecentUsers = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync(HttpContext.RequestAborted);

        // Son 5 Dış Posta Logu (SMTP)
        RecentExternalLogs = await _db.EmailLogs
            .OrderByDescending(l => l.SentAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync(HttpContext.RequestAborted);

        // Son 5 Dahili Posta
        RecentInternalMails = await _db.InternalMails
            .OrderByDescending(m => m.SentAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync(HttpContext.RequestAborted);
    }
}
