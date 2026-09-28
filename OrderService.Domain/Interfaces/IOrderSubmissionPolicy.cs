using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IOrderSubmissionPolicy
{
    Task ValidateAsync(Order order, string customerId, CancellationToken cancellationToken = default);
}
