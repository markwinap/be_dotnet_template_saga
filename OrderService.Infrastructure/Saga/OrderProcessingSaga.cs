using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using Polly;
using Polly.Retry;

namespace OrderService.Infrastructure.Saga;

public sealed class OrderProcessingSaga(ILogger<OrderProcessingSaga> logger) : IOrderSagaOrchestrator
{
    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(250),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();

    public async Task HandleOrderSubmittedAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await Pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("SAGA step: ReserveInventory for {OrderId}", orderId);
            await Task.Delay(10, token);

            logger.LogInformation("SAGA step: ProcessPayment for {OrderId}", orderId);
            await Task.Delay(10, token);

            logger.LogInformation("SAGA step: CreateShipment for {OrderId}", orderId);
            await Task.Delay(10, token);
        }, cancellationToken);
    }
}
