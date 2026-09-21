using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public sealed class AzureServiceBusPublisher(ServiceBusClient client, ILogger<AzureServiceBusPublisher> logger) : IServiceBusPublisher
{
    public async Task PublishAsync<T>(string topicName, string messageId, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sender = client.CreateSender(topicName);
            var message = new ServiceBusMessage(JsonSerializer.Serialize(payload))
            {
                MessageId = messageId,
                ContentType = "application/json"
            };
            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service Bus publish failed for topic {TopicName} and message {MessageId}.", topicName, messageId);
            throw;
        }
    }
}
