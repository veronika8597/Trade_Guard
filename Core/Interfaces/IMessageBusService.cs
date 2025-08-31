namespace Core.Interfaces;

public interface IMessageBusService
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);
    Task SubscribeAsync<T>(string queueName, string routingKey, Func<T, Task> handler, CancellationToken ct );
}