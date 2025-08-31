using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Services.Massaging;

public sealed class MassageBusService : IMessageBusService, IAsyncDisposable
{
    
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
    
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;
    
    public MassageBusService(IOptions<RabbitMqOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;

        var connectionFactory  = new ConnectionFactory
        {
            HostName = _options.Host,
            UserName = _options.UserName,
            Password = _options.Password
        };
        
        _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _exchangeName = _options.ExchangeName; // e.g., "tradeguard_exchange"
        
        _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName, 
            type: ExchangeType.Direct,
            durable: true, 
            autoDelete: false, 
            arguments: null
            ).GetAwaiter().GetResult();
        
        // Basic QoS: don't flood the consumer
        _channel.BasicQosAsync(0, prefetchCount: 20, global: false).GetAwaiter().GetResult();
    }
    
    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(message);
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            Persistent  = true,
            ContentType = "application/json; charset=utf-8"
        };

        await _channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );
    }

    public async Task SubscribeAsync<T>(
        string queueName, 
        string routingKey, 
        Func<T, Task> handler, 
        CancellationToken ct)
    {
        await _channel.QueueDeclareAsync(
            queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        await _channel.QueueBindAsync(
            queue: queueName, exchange: _exchangeName, routingKey: routingKey, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (object sender , BasicDeliverEventArgs delivery) =>
        {
            // If app is stopping, NACK (no requeue) to avoid looping
            if (ct.IsCancellationRequested)
            {
                await _channel.BasicRejectAsync(delivery.DeliveryTag, requeue: true);
                return;
            }

            try
            {
                // Deserialize with enum support ("Buy", "Market", etc.)
                var payload = JsonSerializer.Deserialize<T>(delivery.Body.Span, JsonOpts);

                if (payload is not null)
                    await handler(payload);

                await _channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
            }
            
            catch
            {
                // Poison message: drop (or route to DLQ if you add one)
                await _channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false);
            }
        };
        
        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct
        );

    }
    
    public async ValueTask DisposeAsync()
    {
        try { await _channel.CloseAsync(); } catch { }
        try { await _connection.CloseAsync(); } catch { }
        try { await _channel.DisposeAsync(); } catch { }
        try { await _connection.DisposeAsync(); } catch { }
        GC.SuppressFinalize(this);
        
    }
}