using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using OrderService.Application.DTOs;
using OrderService.Application.Features.Orders.CreateOrder;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class CreateOrderEndpoint
{
    [WolverinePost("/api/orders")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Post(CreateOrderCommand command, ClaimsPrincipal user, IMessageBus bus)
    {
        var order = await bus.InvokeAsync<OrderDto>(command with { CustomerId = GetCustomerId(user) });
        return Results.Created($"/api/orders/{order.Id}", order);
    }

    private static string GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");
}
