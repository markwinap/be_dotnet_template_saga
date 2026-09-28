using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using OrderService.Domain.Interfaces;

namespace OrderService.Domain.Services;

public sealed class OrderSubmissionPolicy(
    OrderSubmissionPolicyOptions options,
    ICustomerOrderQuery customerOrderQuery,
    TimeProvider timeProvider) : IOrderSubmissionPolicy
{
    public async Task ValidateAsync(Order order, string customerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var submissionsInLastMinute = await customerOrderQuery.GetSubmissionCountInLastMinuteAsync(customerId, nowUtc, cancellationToken);
        var submissionsToday = await customerOrderQuery.GetSubmissionCountTodayAsync(customerId, nowUtc, cancellationToken);

        if (order.Status != Enums.OrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be submitted.");
        }

        if (!order.Items.Any())
        {
            throw new DomainException("Cannot submit an order without items.");
        }

        if (order.GetTotal() < options.MinimumOrderTotal)
        {
            throw new DomainException($"Order total must be at least {options.MinimumOrderTotal:C}.");
        }

        if (order.Items.Any(item => item.Quantity > options.MaximumItemQuantity))
        {
            throw new DomainException($"No item may exceed a quantity of {options.MaximumItemQuantity}.");
        }

        if (nowUtc >= order.ExpiresAtUtc)
        {
            throw new DomainException("The draft order has expired and can no longer be submitted.");
        }

        if (!options.BusinessHours.IsWithinBusinessHours(nowUtc, options.TimeZone))
        {
            throw new DomainException("Orders can only be submitted during business hours (08:00-20:00).");
        }

        if (submissionsInLastMinute >= options.MaxSubmissionsPerMinute)
        {
            throw new DomainException("You can only submit one order per minute. Please wait before trying again.");
        }

        if (submissionsToday >= options.MaxSubmissionsPerDay)
        {
            throw new DomainException($"You have reached the daily limit of {options.MaxSubmissionsPerDay} orders.");
        }
    }
}
