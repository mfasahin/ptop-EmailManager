using EmailManager.Infrastructure;
using EmailManager.Infrastructure.Identity;
using EmailManager.Infrastructure.Persistence;
using EmailManager.Web.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("EmailManager uygulaması başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddInfrastructure(builder.Configuration);

    // ─── Identity ───────────────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddScoped<
        Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<ApplicationUser>,
        ApplicationUserClaimsPrincipalFactory>();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Cookie güvenlik ayarları
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    // ─── Rate Limiter ────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.AddSlidingWindowLimiter("email-send", config =>
        {
            config.PermitLimit = 10;
            config.Window = TimeSpan.FromMinutes(1);
            config.SegmentsPerWindow = 6;
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 0;
        });
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                "{\"error\":\"Çok fazla istek. Bir dakika bekleyip tekrar deneyin.\"}", ct);
        };
    });

    // ─── Razor Pages ─────────────────────────────────────────────────────────
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Account/Login");
        options.Conventions.AllowAnonymousToPage("/Account/Register");
        // Admin sayfaları sadece Admin rolüne
        options.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdmin",
            policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // ─── Seed: Roller + Admin kullanıcı ──────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Rolleri seed et
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Log.Information("Rol oluşturuldu: {Role}", role);
            }
        }

        // Admin kullanıcıyı seed et
        var adminUsername = config["AdminCredentials:Username"] ?? "admin";
        var adminPassword = config["AdminCredentials:Password"] ?? "Admin123!";
        var adminEmail = $"{adminUsername}@ptop.com";

        var adminUser = await userManager.FindByNameAsync(adminUsername);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Kullanıcı",
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                Log.Information("Admin kullanıcı oluşturuldu: {Email}", adminEmail);
        }

        // Admin rolü yoksa ata
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Log.Information("Admin rolü atandı: {Username}", adminUsername);
        }

        // Mevcut kullanıcıları "User" rolüne ata (yoksa)
        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any())
            {
                await userManager.AddToRoleAsync(user, "User");
                Log.Information("User rolü atandı: {Email}", user.Email);
            }
        }
    }

    // ─── Güvenlik Başlıkları ──────────────────────────────────────────────────
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        ctx.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net fonts.googleapis.com; " +
            "font-src 'self' fonts.gstatic.com cdn.jsdelivr.net; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";
        await next();
    });

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();

    app.UseRouting();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapGet("/", () => Results.Redirect("/Email/Send"));
    app.MapEmailEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "EmailManager başlatma hatası.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
