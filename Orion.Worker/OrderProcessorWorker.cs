using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Orion.Api.Data;
using Orion.Api.Models;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

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

            _logger.LogInformation("--> Received enhanced order message for Order ID: {OrderId} with {ItemCount} items", 
                orderEvent!.OrderId, orderEvent.Items?.Count ?? 0);

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

    private async Task ProcessOrder(OrderPlacedEvent orderEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .FirstOrDefaultAsync(o => o.Id == orderEvent.OrderId);

        if (order == null)
        {
            _logger.LogError("Order with ID {OrderId} not found.", orderEvent.OrderId);
            return;
        }

        _logger.LogInformation("Starting ENHANCED processing for Order ID: {OrderId} (Status: {Status})", 
            order.Id, order.Status);

        order.Status = OrderStatus.Processing;
        await dbContext.SaveChangesAsync();

        string finalStatus = "";
        bool inventoryConfirmed = false;

        try
        {
            _logger.LogInformation("Processing payment for Order ID: {OrderId}, Amount: ${Amount}", 
                order.Id, order.TotalAmount);
            
            await Task.Delay(2000);

            if (order.TotalAmount == 666 || orderEvent.CustomerName.Contains("Cursed"))
            {
                throw new InvalidOperationException("Simulated payment failure - cursed amount!");
            }

            _logger.LogInformation("Payment successful for Order ID: {OrderId}. Confirming inventory...", order.Id);
            
            inventoryConfirmed = await ConfirmInventoryAsync(dbContext, order);
            
            if (!inventoryConfirmed)
            {
                throw new InvalidOperationException("Failed to confirm inventory reservation");
            }

            order.Status = OrderStatus.InventoryConfirmed;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Finalizing order processing for Order ID: {OrderId}...", order.Id);
            await Task.Delay(1000);

            order.Status = OrderStatus.Completed;
            finalStatus = order.Status.ToString();
            
            _logger.LogInformation("✅ COMPLETED processing for Order ID: {OrderId}. Inventory confirmed.", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing Order ID: {OrderId}", order.Id);

            if (!inventoryConfirmed)
            {
                _logger.LogWarning("Rolling back inventory reservation for failed Order ID: {OrderId}", order.Id);
                await RollbackInventoryAsync(dbContext, order);
                order.Status = OrderStatus.InventoryRollback;
            }
            else
            {
                order.Status = OrderStatus.Failed;
            }
            
            finalStatus = order.Status.ToString();
        }
        finally
        {
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Notifying API of final status for Order ID: {OrderId}", order.Id);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _configuration["ApiKey"]);
            
            var payload = new { 
                OrderId = order.Id, 
                UserId = orderEvent.UserId, 
                Status = finalStatus,
                TotalAmount = order.TotalAmount,
                ItemCount = orderEvent.Items?.Count ?? 0
            };
            var apiUrl = $"{_configuration["ApiBaseUrl"]}/api/notifications/order-status";
            
            try
            {
                await client.PostAsJsonAsync(apiUrl, payload);
                _logger.LogInformation("Successfully notified API for Order ID: {OrderId} with status: {Status}", order.Id, finalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify API for Order ID: {OrderId} with status: {Status}", order.Id, finalStatus);
            }
        }
    }

    private async Task<bool> ConfirmInventoryAsync(OrionDbContext dbContext, Order order)
    {
        try
        {
            foreach (var orderItem in order.OrderItems)
            {
                var inventory = orderItem.Inventory;
                inventory.ReservedQuantity -= orderItem.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;

                if (inventory.ReservedQuantity < 0)
                {
                    return false;
                }
            }

            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm inventory for Order ID: {OrderId}", order.Id);
            return false;
        }
    }

    private async Task<bool> RollbackInventoryAsync(OrionDbContext dbContext, Order order)
    {
        try
        {
            foreach (var orderItem in order.OrderItems)
            {
                var inventory = orderItem.Inventory;
                inventory.AvailableQuantity += orderItem.Quantity;
                inventory.ReservedQuantity -= orderItem.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback inventory for Order ID: {OrderId}", order.Id);
            return false;
        }
    }

    public override void Dispose()
    {
        _channel.CloseAsync().GetAwaiter().GetResult();
        _connection.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}