using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _pollingInterval;

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger, TimeSpan? pollingInterval = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher batch failed.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var publisher = scope.ServiceProvider.GetRequiredService<IServiceBusPublisher>();

        var messages = await outboxStore.GetUnprocessedMessagesAsync(50, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    message.Topic,
                    message.MessageId,
                    message.Payload,
                    cancellationToken);

                await outboxStore.MarkProcessedAsync(message.Id, cancellationToken);
                await outboxStore.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Published outbox message {MessageId} to {Topic}", message.MessageId, message.Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.MessageId);
                await outboxStore.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
                await outboxStore.SaveChangesAsync(cancellationToken);
            }
        }
    }

}
