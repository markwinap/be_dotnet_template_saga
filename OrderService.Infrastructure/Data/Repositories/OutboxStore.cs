using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Data.Repositories;

public sealed class OutboxStore(OrderDbContext dbContext) : IOutboxStore
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.ProcessedAtUtc, DateTime.UtcNow)
                    .SetProperty(m => m.Error, (string?)null),
                cancellationToken);
    }

    public Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        return dbContext.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Error, error)
                    .SetProperty(m => m.RetryCount, m => m.RetryCount + 1),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
