namespace OrderService.Application.Common.Interfaces;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(string topicName, string messageId, T payload, CancellationToken cancellationToken = default);
}
