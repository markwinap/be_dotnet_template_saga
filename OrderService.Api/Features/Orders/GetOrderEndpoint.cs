using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using OrderService.Application.DTOs;
using OrderService.Application.Features.Orders.GetOrder;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class GetOrderEndpoint
{
    [WolverineGet("/api/orders/{id:guid}")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Get(Guid id, ClaimsPrincipal user, IMessageBus bus)
    {
        var order = await bus.InvokeAsync<OrderDto?>(new GetOrderQuery(id, GetCustomerId(user)));
        return order is null ? Results.NotFound() : Results.Ok(order);
    }

    private static string GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");
}
