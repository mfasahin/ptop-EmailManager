using EmailManager.Domain.Entities;
using EmailManager.Domain.Interfaces;
using EmailManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManager.Infrastructure.Repositories;

/// <summary>
/// <see cref="IInternalMailRepository"/> EF Core implementasyonu.
/// </summary>
public class InternalMailRepository : IInternalMailRepository
{
    private readonly AppDbContext _db;

    public InternalMailRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(InternalMail mail, CancellationToken ct = default)
    {
        await _db.InternalMails.AddAsync(mail, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<InternalMail> Items, int Total)> GetInboxAsync(
        string toEmail, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.InternalMails
            .Where(m => m.ToEmail == toEmail && !m.IsDeletedByRecipient)
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<InternalMail> Items, int Total)> GetSentAsync(
        string fromEmail, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.InternalMails
            .Where(m => m.FromEmail == fromEmail && !m.IsDeletedBySender)
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<InternalMail> Items, int Total)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.InternalMails
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<InternalMail?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.InternalMails.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task MarkAsReadAsync(int id, CancellationToken ct = default)
    {
        var mail = await _db.InternalMails.FindAsync(new object[] { id }, ct);
        if (mail is not null && !mail.IsRead)
        {
            mail.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetUnreadCountAsync(string toEmail, CancellationToken ct = default)
        => await _db.InternalMails
            .CountAsync(m => m.ToEmail == toEmail && !m.IsRead && !m.IsDeletedByRecipient, ct);

    public async Task DeleteByRecipientAsync(int id, CancellationToken ct = default)
    {
        var mail = await _db.InternalMails.FindAsync(new object[] { id }, ct);
        if (mail is not null)
        {
            mail.IsDeletedByRecipient = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
