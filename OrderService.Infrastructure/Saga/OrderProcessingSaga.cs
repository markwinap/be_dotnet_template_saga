using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Saga.Activities;
using Polly;
using Polly.Retry;

namespace OrderService.Infrastructure.Saga;

public sealed class OrderProcessingSaga(
    ILogger<OrderProcessingSaga> logger,
    IEnumerable<IOrderProcessingActivity> activities,
    IOrderWorkflowExecutionStore executionStore) : IOrderSagaOrchestrator
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
        var existingExecution = await executionStore.GetByOrderIdAsync(orderId, cancellationToken);
        var isNew = existingExecution is null;
        var execution = existingExecution ?? OrderWorkflowExecution.Start(orderId);

        if (execution.Status is "Completed" or "Failed")
        {
            logger.LogInformation("SAGA for {OrderId} is already {Status}; skipping.", orderId, execution.Status);
            return;
        }

        if (isNew)
        {
            await executionStore.AddAsync(execution, cancellationToken);
            await executionStore.SaveChangesAsync(cancellationToken);
        }

        try
        {
            foreach (var activity in activities.OrderBy(a => a.Name))
            {
                execution.RecordStep(activity.Name, "InProgress");
                await executionStore.UpdateAsync(execution, cancellationToken);
                await executionStore.SaveChangesAsync(cancellationToken);

                var result = await Pipeline.ExecuteAsync(async token =>
                {
                    logger.LogInformation("SAGA step: {ActivityName} for {OrderId}", activity.Name, orderId);
                    return await activity.ExecuteAsync(orderId, token);
                }, cancellationToken);

                if (!result.Succeeded)
                {
                    execution.RecordStep(activity.Name, "Failed", result.Error);
                    await executionStore.UpdateAsync(execution, cancellationToken);
                    await executionStore.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            execution.RecordStep("Completed", "Completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SAGA failed for {OrderId}", orderId);
            execution.RecordStep(execution.CurrentStep, "Failed", ex.Message);
        }
        finally
        {
            await executionStore.UpdateAsync(execution, cancellationToken);
            await executionStore.SaveChangesAsync(cancellationToken);
        }
    }
}
