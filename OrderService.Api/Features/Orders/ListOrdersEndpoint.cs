using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using OrderService.Application.DTOs;
using OrderService.Application.Features.Orders.ListOrders;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class ListOrdersEndpoint
{
    [WolverineGet("/api/orders")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Get(ClaimsPrincipal user, IMessageBus bus)
    {
        var orders = await bus.InvokeAsync<IReadOnlyCollection<OrderDto>>(new ListOrdersQuery(GetCustomerId(user)));
        return Results.Ok(orders);
    }

    private static string GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");
}
