using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using OrderService.Application.Features.Orders.DeleteOrder;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class DeleteOrderEndpoint
{
    [WolverineDelete("/api/orders/{id:guid}")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Delete(Guid id, ClaimsPrincipal user, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(new DeleteOrderCommand(id, GetCustomerId(user)));
            return Results.NoContent();
        }
        catch (OrderService.Application.Common.Exceptions.NotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static string GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException("The access token is missing the subject claim.");
}
