using EmailManager.Application.DTOs;
using EmailManager.Application.Interfaces;
using EmailManager.Domain.Entities;
using EmailManager.Domain.Interfaces;

namespace EmailManager.Web.Endpoints;

/// <summary>
/// E-posta ile ilgili Minimal API endpoint tanımları.
/// Tüm endpoint'ler X-Api-Key header ile korunur.
/// </summary>
public static class EmailEndpoints
{
    /// <summary>
    /// Minimal API endpoint'lerini <see cref="WebApplication"/>'a kaydeder.
    /// </summary>
    public static void MapEmailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/email")
            .AddEndpointFilter<ApiKeyFilter>()
            .WithTags("Email API");

        // POST /api/email/send — E-posta gönder
        group.MapPost("/send", SendEmailAsync)
            .RequireRateLimiting("email-send")
            .WithName("SendEmail")
            .WithSummary("E-posta gönderir.");

        // GET /api/email/logs — Sayfalı log listesi
        group.MapGet("/logs", GetLogsAsync)
            .WithName("GetEmailLogs")
            .WithSummary("E-posta gönderim loglarını sayfalı olarak listeler.");

        // GET /api/email/logs/{id} — Tekil log kaydı
        group.MapGet("/logs/{id:int}", GetLogByIdAsync)
            .WithName("GetEmailLogById")
            .WithSummary("Belirtilen id'ye sahip log kaydını döndürür.");
    }

    /// <summary>E-posta gönderir ve sonucu döndürür.</summary>
    private static async Task<IResult> SendEmailAsync(
        SendEmailRequest request,
        IEmailService emailService,
        CancellationToken ct)
    {
        if (!MinimalApiValidator.IsValid(request, out var errors))
            return Results.ValidationProblem(errors);

        var message = new EmailMessage
        {
            To = request.To,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml
        };

        var success = await emailService.SendAsync(message, ct);

        return success
            ? Results.Ok(new { message = "E-posta başarıyla gönderildi." })
            : Results.Problem("E-posta gönderilemedi. Lütfen log kayıtlarını kontrol edin.",
                statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>Sayfalı log listesini döndürür.</summary>
    private static async Task<IResult> GetLogsAsync(
        IEmailLogRepository repository,
        int page = 1,
        int size = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(page, size, ct);

        var dtos = items.Select(MapToDto).ToList();

        return Results.Ok(new
        {
            page,
            size,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)size),
            items = dtos
        });
    }

    /// <summary>Belirtilen id'ye sahip log kaydını döndürür.</summary>
    private static async Task<IResult> GetLogByIdAsync(
        int id,
        IEmailLogRepository repository,
        CancellationToken ct)
    {
        var log = await repository.GetByIdAsync(id, ct);

        return log is null
            ? Results.NotFound(new { message = $"Id={id} olan log kaydı bulunamadı." })
            : Results.Ok(MapToDto(log));
    }

    /// <summary><see cref="EmailLog"/> entity'sini <see cref="EmailLogDto"/>'ya dönüştürür.</summary>
    private static EmailLogDto MapToDto(EmailLog log) => new()
    {
        Id = log.Id,
        To = log.To,
        Subject = log.Subject,
        Status = log.Status,
        ErrorMessage = log.ErrorMessage,
        SentAt = log.SentAt
    };
}

/// <summary>
/// X-Api-Key header kontrolü yapan Minimal API endpoint filter'ı.
/// </summary>
public class ApiKeyFilter : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();

        var expectedApiKey = configuration["ApiKey"] ?? string.Empty;

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            || providedKey != expectedApiKey)
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

/// <summary>
/// Minimal API'de DataAnnotations doğrulaması için yardımcı sınıf.
/// </summary>
internal static class MinimalApiValidator
{
    public static bool IsValid<T>(T model, out Dictionary<string, string[]> errors)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model!);
        var isValid = System.ComponentModel.DataAnnotations.Validator
            .TryValidateObject(model!, context, validationResults, true);

        errors = validationResults
            .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "")
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.ErrorMessage ?? "").ToArray());

        return isValid;
    }
}
