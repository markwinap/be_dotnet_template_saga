using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using OrderService.Application.Features.Orders.SubmitOrder;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class SubmitOrderEndpoint
{
    [WolverinePost("/api/orders/{id:guid}/submit")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Post(Guid id, ClaimsPrincipal user, IMessageBus bus)
    {
        await bus.InvokeAsync<object[]>(new SubmitOrderCommand(id, GetCustomerId(user)));
        return Results.Accepted($"/api/orders/{id}");
    }

    private static string GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");
}
