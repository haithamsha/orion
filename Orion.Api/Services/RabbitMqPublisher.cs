using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

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

    public void Publish<T>(T message)
    {
        // We'll declare an "exchange" which is responsible for routing messages.
        // A "fanout" exchange sends a copy of the message to all queues that are bound to it.
        _channel.ExchangeDeclareAsync(exchange: "order-events", type: ExchangeType.Fanout).GetAwaiter().GetResult();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublishAsync(
            exchange: "order-events",
            routingKey: "", // Not used for fanout exchanges
            body: body).GetAwaiter().GetResult();

        Console.WriteLine($"--> Published Message: {json}");
    }
}