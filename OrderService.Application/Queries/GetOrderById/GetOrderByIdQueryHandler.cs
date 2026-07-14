using MediatR;
using OrderService.Application.DTOs;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrderRepository repository) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
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
