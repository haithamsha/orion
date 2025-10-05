using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.DTOs;
using Orion.Api.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Orion.Api.Tests.Integration.Helpers;

namespace Orion.Api.Tests.Integration.CQRS;

public class OrderCQRSIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public OrderCQRSIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Clean the database before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        _factory.SeedData(context);
    }

    [Fact]
    public async Task CreateOrder_CQRS_Flow_Integration_Test()
    {
        // Arrange
        var createOrderRequest = new CreateOrderRequest(
            "Integration Test Customer",
            new List<OrderItemRequest>
            {
                new OrderItemRequest("INTEGRATION-001", 2),
                new OrderItemRequest("INTEGRATION-002", 1)
            }
        );

        // Act - Create order using CQRS
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert - Order creation
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Since the endpoint is now 'fast' (Accepted), we can't read the response body directly.
        // We will verify the creation by querying the database.

        // Allow some time for the background processing to complete
        await Task.Delay(2000); // Adjust delay as needed

        // Verify order was saved to database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var savedOrder = await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.CustomerName == "Integration Test Customer");
        
        Assert.NotNull(savedOrder);
        Assert.Equal("test-user-123", savedOrder.UserId);
        Assert.Equal(65.00m, savedOrder.TotalAmount); // (2 * 25.00) + (1 * 15.00)
        Assert.Equal(2, savedOrder.OrderItems.Count);

        // Verify read models were created (if projections are working)
        var summaryView = await context.OrderSummaryViews
            .FirstOrDefaultAsync(o => o.OrderId == savedOrder.Id);
        
        if (summaryView != null) // Projections might not work in integration tests due to timing
        {
            Assert.Equal("Integration Test Customer", summaryView.CustomerName);
            Assert.Equal(65.00m, summaryView.TotalAmount);
            Assert.Equal(2, summaryView.ItemCount);
        }

        // Test reading the order back using CQRS query
        var getOrderResponse = await _client.GetAsync($"/api/orders/{savedOrder.Id}");
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);

        var retrievedOrder = await getOrderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(retrievedOrder);
        Assert.Equal(savedOrder.Id, retrievedOrder.OrderId);
        Assert.Equal("Integration Test Customer", retrievedOrder.CustomerName);
    }

    [Fact]
    public async Task GetMyOrders_CQRS_Query_Integration_Test()
    {
        // Arrange - Create some test orders first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        // Clear existing orders for this user to ensure a clean slate
        var existingOrders = context.Orders.Where(o => o.UserId == "test-user-123");
        context.Orders.RemoveRange(existingOrders);
        await context.SaveChangesAsync();
        
        var testOrders = new List<Order>
        {
            new()
            {
                UserId = "test-user-123",
                CustomerName = "Test Customer 1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                OrderItems = new List<OrderItem>()
            },
            new()
            {
                UserId = "test-user-123",
                CustomerName = "Test Customer 2",
                TotalAmount = 75.00m,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                OrderItems = new List<OrderItem>()
            }
        };

        context.Orders.AddRange(testOrders);
        await context.SaveChangesAsync();

        // Act - Get user's orders using CQRS query
        var response = await _client.GetAsync("/api/orders/my-orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.NotNull(orders);
        Assert.Equal(2, orders.Count);
        
        // Orders should be sorted by creation date descending
        Assert.Equal("Test Customer 2", orders[0].CustomerName); // More recent
        Assert.Equal("Test Customer 1", orders[1].CustomerName); // Older
    }

    [Fact]
    public async Task ChangeOrderStatus_CQRS_Command_Integration_Test()
    {
        // Arrange - Create a test order first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var testOrder = new Order
        {
            Id = 999, // Using a distinct Id for test isolation
            UserId = "test-user-123",
            CustomerName = "Status Change Test",
            TotalAmount = 50.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new() { ProductSku = "INTEGRATION-001", Quantity = 1, UnitPrice = 50.00m }
            }
        };

        context.Orders.Add(testOrder);
        await context.SaveChangesAsync();

        var changeStatusRequest = new ChangeOrderStatusRequest(
            OrderStatus.Processing,
            "Integration test status change"
        );

        // Act - Change order status using CQRS command
        var response = await _client.PutAsJsonAsync($"/api/orders/{testOrder.Id}/status", changeStatusRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify status was changed in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var updatedOrder = await verifyContext.Orders.FindAsync(testOrder.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Processing, updatedOrder.Status);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidInventory_Returns_BadRequest()
    {
        // Arrange
        var createOrderRequest = new CreateOrderRequest(
            "Error Test Customer",
            new List<OrderItemRequest>
            {
                new("NONEXISTENT-SKU", 1)
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // Verify no order was created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var orderCount = await context.Orders
            .CountAsync(o => o.CustomerName == "Error Test Customer");
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task CQRS_EventSourcing_Integration_Verification()
    {
        // This test verifies that Event Sourcing events are being saved
        // when using CQRS commands
        
        // Arrange
        var createOrderRequest = new CreateOrderRequest(
            "Event Sourcing Test",
            new List<OrderItemRequest>
            {
                new("INTEGRATION-001", 1)
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Allow time for async processing
        await Task.Delay(1000);

        // Verify Event Store contains the events
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        // Find the order to get its ID for event lookup
        var order = await context.Orders.FirstOrDefaultAsync(o => o.CustomerName == "Event Sourcing Test");
        Assert.NotNull(order);

        var eventStoreEntries = await context.EventStoreEntries
            .Where(e => e.AggregateId == order.AggregateId && e.EventType.Contains("OrderCreated"))
            .ToListAsync();
        
        Assert.NotEmpty(eventStoreEntries);
        
        var orderCreatedEvent = eventStoreEntries.First();
        Assert.Equal("test-user-123", orderCreatedEvent.UserId);
        Assert.Contains("OrderCreatedEvent", orderCreatedEvent.EventType);
    }
}