using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public sealed class AzureServiceBusPublisher(ServiceBusClient client, ILogger<AzureServiceBusPublisher> logger) : IServiceBusPublisher
{
    public async Task PublishAsync<T>(string topicName, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sender = client.CreateSender(topicName);
            var message = new ServiceBusMessage(JsonSerializer.Serialize(payload));
            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Service Bus publish failed for topic {TopicName}.", topicName);
        }
    }
}
