using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Orion.Api.Models;

namespace Orion.Api.Services;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory() { HostName = configuration["RabbitMq:HostName"] };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    // Keep your existing Publish method for backward compatibility
    public void Publish<T>(T message)
    {
        _channel.ExchangeDeclareAsync(exchange: "order-events", type: ExchangeType.Fanout).GetAwaiter().GetResult();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublishAsync(
            exchange: "order-events",
            routingKey: "",
            body: body).GetAwaiter().GetResult();

        Console.WriteLine($"--> Published Message: {json}");
    }

    // NEW: Add this method for the OrderPlacedEvent
    public async Task PublishOrderPlacedAsync(OrderPlacedEvent orderEvent)
    {
        await _channel.ExchangeDeclareAsync(exchange: "order-events", type: ExchangeType.Fanout);

        var json = JsonSerializer.Serialize(orderEvent);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: "order-events",
            routingKey: "",
            body: body);

        Console.WriteLine($"--> Published OrderPlacedEvent: {json}");
    }
}