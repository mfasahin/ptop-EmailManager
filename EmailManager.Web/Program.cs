using EmailManager.Infrastructure;
using EmailManager.Infrastructure.Persistence;
using EmailManager.Web.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

// ─────────────────────────────────────────────────────────────────────────────
// Serilog: Başlangıç logger (host oluşturulmadan önce hata loglama için)
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("EmailManager uygulaması başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // ─────────────────────────────────────────────────────────────────────────
    // Serilog entegrasyonu (appsettings.json konfigürasyonundan okunur)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    // ─────────────────────────────────────────────────────────────────────────
    // Infrastructure servisleri (EF Core, EmailService, Repository)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ─────────────────────────────────────────────────────────────────────────
    // ASP.NET Core Identity (cookie tabanlı, tek kullanıcı senaryosu)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Geliştirme ortamı için gevşetilmiş şifre politikası
        // Production'da güçlendirin
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Cookie auth yönlendirme ayarları
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

    // ─────────────────────────────────────────────────────────────────────────
    // Rate Limiter: IP başına dakikada maksimum 5 istek
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.AddSlidingWindowLimiter("email-send", config =>
        {
            config.PermitLimit = 5;
            config.Window = TimeSpan.FromMinutes(1);
            config.SegmentsPerWindow = 6;
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 0;
        });

        // Limit aşıldığında 429 Too Many Requests döndür
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                "{\"error\": \"Çok fazla istek gönderdiniz. Lütfen bir dakika bekleyip tekrar deneyin.\"}",
                cancellationToken);
        };
    });

    // ─────────────────────────────────────────────────────────────────────────
    // Razor Pages
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddRazorPages(options =>
    {
        // Tüm sayfalar varsayılan olarak authorize
        options.Conventions.AuthorizeFolder("/");
        // Login sayfası herkese açık
        options.Conventions.AllowAnonymousToPage("/Account/Login");
    });

    // ─────────────────────────────────────────────────────────────────────────
    // API key doğrulama için configuration erişimi (Minimal API'de kullanılır)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // ─────────────────────────────────────────────────────────────────────────
    // Veritabanı migration'larını uygula + seed admin kullanıcı
    // ─────────────────────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Admin kullanıcıyı seed et (appsettings.json'dan)
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminUsername = config["AdminCredentials:Username"] ?? "admin";
        var adminPassword = config["AdminCredentials:Password"] ?? "Admin123!";

        if (await userManager.FindByNameAsync(adminUsername) is null)
        {
            var adminUser = new IdentityUser
            {
                UserName = adminUsername,
                Email = $"{adminUsername}@emailmanager.local",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                Log.Information("Admin kullanıcı oluşturuldu: {Username}", adminUsername);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Middleware Pipeline
    // ─────────────────────────────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging(); // HTTP istek loglaması

    app.UseRouting();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();

    // Root URL'i E-posta Gönder sayfasına yönlendir
    app.MapGet("/", () => Results.Redirect("/Email/Send"));

    // ─────────────────────────────────────────────────────────────────────────
    // Minimal API Endpoint'leri
    // ─────────────────────────────────────────────────────────────────────────
    app.MapEmailEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "EmailManager başlatma sırasında beklenmedik hata oluştu.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
