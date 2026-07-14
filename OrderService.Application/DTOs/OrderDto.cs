namespace OrderService.Application.DTOs;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerId,
    string Status,
    DateTime CreatedAtUtc,
    decimal Total,
    IReadOnlyCollection<OrderItemDto> Items);
