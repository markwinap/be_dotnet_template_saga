using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IOrderWorkflowExecutionStore
{
    Task<OrderWorkflowExecution?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderWorkflowExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderWorkflowExecution execution, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
