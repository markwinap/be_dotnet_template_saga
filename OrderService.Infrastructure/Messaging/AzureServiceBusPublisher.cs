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
            var body = payload is string payloadString
                ? payloadString
                : JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(body)
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
