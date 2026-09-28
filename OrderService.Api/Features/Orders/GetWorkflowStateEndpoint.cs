using Microsoft.AspNetCore.RateLimiting;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Orders.GetWorkflowState;
using Wolverine;
using Wolverine.Http;

namespace OrderService.Api.Features.Orders;

public static class GetWorkflowStateEndpoint
{
    [WolverineGet("/api/orders/{id:guid}/workflow-state")]
    [EnableRateLimiting("api")]
    public static async Task<IResult> Get(Guid id, IMessageBus bus)
    {
        var state = await bus.InvokeAsync<OrderWorkflowState?>(new GetWorkflowStateQuery(id));
        return state is null ? Results.NotFound() : Results.Ok(state);
    }
}
