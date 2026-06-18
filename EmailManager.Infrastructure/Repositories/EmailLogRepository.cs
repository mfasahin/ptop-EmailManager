using EmailManager.Domain.Entities;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManager.Infrastructure.Repositories;

/// <summary>
/// <see cref="IEmailLogRepository"/> arayüzünün EF Core implementasyonu.
/// </summary>
public class EmailLogRepository : IEmailLogRepository
{
    private readonly AppDbContext _context;

    public EmailLogRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(EmailLog log, CancellationToken ct = default)
    {
        await _context.EmailLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<EmailLog> Items, int TotalCount)> GetPagedAsync(
        int page, int size, CancellationToken ct = default)
    {
        // Sayfa numarası en az 1 olmalıdır.
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = _context.EmailLogs.AsNoTracking();
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.SentAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<EmailLog?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.EmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}
