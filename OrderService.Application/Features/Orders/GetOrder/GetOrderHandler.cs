using OrderService.Application.DTOs;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Features.Orders.GetOrder;

public static class GetOrderHandler
{
    public static async Task<OrderDto?> Handle(GetOrderQuery query, IOrderRepository repository, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(query.OrderId, query.CustomerId, cancellationToken);
        if (order is null)
        {
            return null;
        }

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
