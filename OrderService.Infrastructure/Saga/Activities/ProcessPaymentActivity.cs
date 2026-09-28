using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Saga.Activities;

public sealed class ProcessPaymentActivity(ILogger<ProcessPaymentActivity> logger) : IOrderProcessingActivity
{
    public string Name => "ProcessPayment";

    public Task<ActivityResult> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SAGA activity {ActivityName} for {OrderId}", Name, orderId);
        // Placeholder for payment processing integration.
        return Task.FromResult(new ActivityResult(true, null, Guid.NewGuid().ToString("N")));
    }
}
