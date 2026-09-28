using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Data.Repositories;

public sealed class OrderWorkflowExecutionStore(OrderDbContext dbContext) : IOrderWorkflowExecutionStore
{
    public Task<OrderWorkflowExecution?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return dbContext.OrderWorkflowExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(OrderWorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderWorkflowExecutions.AddAsync(execution, cancellationToken);
    }

    public Task UpdateAsync(OrderWorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        dbContext.OrderWorkflowExecutions.Update(execution);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
