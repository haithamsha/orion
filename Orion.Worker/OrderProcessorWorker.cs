using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Orion.Api.Data;
using Orion.Api.Models;
using System.Net.Http.Json;

namespace Orion.Worker;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public OrderProcessorWorker(ILogger<OrderProcessorWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        // 1. Connect to RabbitMQ
        var factory = new ConnectionFactory() { HostName = configuration["RabbitMq:HostName"] };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // 2. Ensure the exchange exists
        _channel.ExchangeDeclareAsync(exchange: "order-events", type: ExchangeType.Fanout).GetAwaiter().GetResult();

        // 3. Declare a queue to consume messages from
        var queueName = "order-processing-queue";
        _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null).GetAwaiter().GetResult();

        // 4. Bind the queue to the exchange.
        _channel.QueueBindAsync(queue: queueName, exchange: "order-events", routingKey: "").GetAwaiter().GetResult();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        // 5. Define the code that runs when a message is received
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

            _logger.LogInformation("--> Received message for Order ID: {OrderId}", orderEvent!.OrderId);

            try
            {
                await ProcessOrder(orderEvent);
                // 6. Acknowledge the message (Success)
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Order ID {OrderId}", orderEvent!.OrderId);
                // We still acknowledge the message to prevent it from being re-processed indefinitely.
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        // 7. Start consuming messages from the queue
        _channel.BasicConsumeAsync(queue: "order-processing-queue", autoAck: false, consumer: consumer).GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    // This method has no RabbitMQ dependencies, so it remains unchanged.
    private async Task ProcessOrder(OrderPlacedEvent orderEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();

        var order = await dbContext.Orders.FindAsync(orderEvent.OrderId);
        if (order == null)
        {
            _logger.LogError("Order with ID {OrderId} not found.", orderEvent.OrderId);
            return;
        }

        _logger.LogInformation("Starting BACKGROUND processing for Order ID: {OrderId}", order.Id);
        order.Status = OrderStatus.Processing;
        await dbContext.SaveChangesAsync();

        string finalStatus = "";
        try
        {
            await Task.Delay(3000);
            if (order.TotalAmount == 666) throw new InvalidOperationException("Simulated payment failure.");
            await Task.Delay(500);
            await Task.Delay(2000);
            order.Status = OrderStatus.Completed;
            finalStatus = order.Status.ToString();
            _logger.LogInformation("FINISHED BACKGROUND processing for Order ID: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Order ID: {OrderId}", order.Id);
            order.Status = OrderStatus.Failed;
            finalStatus = order.Status.ToString();
        }
        finally
        {
            await dbContext.SaveChangesAsync();

            // NEW: Call the API to push real-time notification
            _logger.LogInformation("Notifying API of final status for Order ID: {OrderId}", order.Id);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _configuration["ApiKey"]);
            
            var payload = new { OrderId = order.Id, UserId = orderEvent.UserId, Status = finalStatus };
            var apiUrl = $"{_configuration["ApiBaseUrl"]}/api/notifications/order-status";
            
            try
            {
                await client.PostAsJsonAsync(apiUrl, payload);
                _logger.LogInformation("Successfully notified API for Order ID: {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify API for Order ID: {OrderId}", order.Id);
            }
        }
    }

    public override void Dispose()
    {
        _channel.CloseAsync().GetAwaiter().GetResult();
        _connection.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}