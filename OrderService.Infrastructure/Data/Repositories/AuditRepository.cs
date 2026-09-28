using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Data.Repositories;

public sealed class AuditRepository(OrderDbContext dbContext) : IAuditRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task AddAsync(OrderStateTrace trace, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderStateTraces.AddAsync(trace, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OrderStateTrace>> ListOrderTracesAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrderStateTraces
            .AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
