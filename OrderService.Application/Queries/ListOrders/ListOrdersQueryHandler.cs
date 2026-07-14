using MediatR;
using OrderService.Application.DTOs;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Queries.ListOrders;

public sealed class ListOrdersQueryHandler(IOrderRepository repository)
    : IRequestHandler<ListOrdersQuery, IReadOnlyCollection<OrderDto>>
{
    public async Task<IReadOnlyCollection<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.ListAsync(cancellationToken);

        return orders
            .Select(order => new OrderDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.Status.ToString(),
                order.CreatedAtUtc,
                order.GetTotal(),
                order.Items.Select(x => new OrderItemDto(x.ProductCode, x.Quantity, x.UnitPrice.Amount, x.UnitPrice.Currency)).ToArray()))
            .ToArray();
    }
}
