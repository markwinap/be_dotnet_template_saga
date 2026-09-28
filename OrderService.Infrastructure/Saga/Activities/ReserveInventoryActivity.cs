using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Saga.Activities;

public sealed class ReserveInventoryActivity(ILogger<ReserveInventoryActivity> logger) : IOrderProcessingActivity
{
    public string Name => "ReserveInventory";

    public Task<ActivityResult> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SAGA activity {ActivityName} for {OrderId}", Name, orderId);
        // Placeholder for inventory reservation integration.
        return Task.FromResult(new ActivityResult(true, null, Guid.NewGuid().ToString("N")));
    }
}
