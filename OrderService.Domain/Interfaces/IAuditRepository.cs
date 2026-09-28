using OrderService.Domain.Entities.Audit;

namespace OrderService.Domain.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task AddAsync(OrderStateTrace trace, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OrderStateTrace>> ListOrderTracesAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
