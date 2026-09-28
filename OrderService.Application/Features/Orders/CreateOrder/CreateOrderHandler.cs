using System.Text.Json;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Features.Orders.CreateOrder;

public static class CreateOrderHandler
{
    public static async Task<OrderDto> Handle(
        CreateOrderCommand command,
        IOrderRepository repository,
        ICacheService cacheService,
        IAuditRepository auditRepository,
        IAuditContext auditContext,
        OrderSubmissionPolicyOptions policyOptions,
        CancellationToken cancellationToken)
    {
        var order = Order.Create(
            command.CustomerId,
            new Address(command.ShippingLine1, command.City, command.State, command.PostalCode, command.Country),
            policyOptions.DraftExpiration);

        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductCode, item.Quantity, new Money(item.UnitPrice, item.Currency));
        }

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"customer:{order.CustomerId}:orders:list", cancellationToken);

        var trace = OrderStateTrace.Create(
            order.Id,
            null,
            order.Status.ToString(),
            auditContext.ActorId,
            null,
            auditContext.CorrelationId,
            JsonSerializer.Serialize(new { order.OrderNumber, order.CustomerId, Total = order.GetTotal() }));
        await auditRepository.AddAsync(trace, cancellationToken);
        await auditRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Status.ToString(),
            order.CreatedAtUtc,
            order.GetTotal(),
            order.Items.Select(x => new OrderItemDto(x.ProductCode, x.Quantity, x.UnitPrice.Amount, x.UnitPrice.Currency)).ToArray());
}
