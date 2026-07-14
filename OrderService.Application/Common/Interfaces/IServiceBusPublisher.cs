namespace OrderService.Application.Common.Interfaces;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(string topicName, T payload, CancellationToken cancellationToken = default);
}
