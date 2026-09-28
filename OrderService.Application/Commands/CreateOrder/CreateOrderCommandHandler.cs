using System.Text.Json;
using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Entities.Audit;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IOrderRepository repository,
    ICacheService cacheService,
    IAuditRepository auditRepository,
    IAuditContext auditContext,
    OrderSubmissionPolicyOptions policyOptions) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(
            request.CustomerId,
            new Address(request.ShippingLine1, request.City, request.State, request.PostalCode, request.Country),
            policyOptions.DraftExpiration);

        foreach (var item in request.Items)
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

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Status.ToString(),
            order.CreatedAtUtc,
            order.GetTotal(),
            order.Items.Select(x => new OrderItemDto(x.ProductCode, x.Quantity, x.UnitPrice.Amount, x.UnitPrice.Currency)).ToArray());
    }
}
