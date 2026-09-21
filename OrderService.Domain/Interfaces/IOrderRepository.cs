using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, string customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> ListAsync(string customerId, CancellationToken cancellationToken = default);
    void Remove(Order order);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
