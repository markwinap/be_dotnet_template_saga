using Microsoft.AspNetCore.RateLimiting;
using OrderService.Application.Features.Orders.GetAuditTrace;
using OrderService.Domain.Entities.Audit;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class GetAuditTraceEndpoint
{
    [WolverineGet("/api/orders/{id:guid}/audit")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Get(Guid id, IMessageBus bus)
    {
        var traces = await bus.InvokeAsync<IReadOnlyCollection<OrderStateTrace>>(new GetAuditTraceQuery(id));
        return Results.Ok(traces);
    }
}
