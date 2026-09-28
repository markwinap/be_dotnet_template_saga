using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Saga.Activities;

public sealed class CreateShipmentActivity(ILogger<CreateShipmentActivity> logger) : IOrderProcessingActivity
{
    public string Name => "CreateShipment";

    public Task<ActivityResult> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SAGA activity {ActivityName} for {OrderId}", Name, orderId);
        // Placeholder for shipment creation integration.
        return Task.FromResult(new ActivityResult(true, null, Guid.NewGuid().ToString("N")));
    }
}
