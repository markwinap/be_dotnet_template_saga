namespace OrderService.Application.DTOs;

public sealed record OrderItemDto(string ProductCode, int Quantity, decimal UnitPrice, string Currency);
