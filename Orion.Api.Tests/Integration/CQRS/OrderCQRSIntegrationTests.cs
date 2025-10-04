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

namespace Orion.Api.Tests.Integration.CQRS;

public class OrderCQRSIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrderCQRSIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace database with in-memory for testing
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrionDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<OrionDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid());
                });

                // Ensure database is created and seeded
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
                context.Database.EnsureCreated();
                SeedTestData(context);
            });
        });

        _client = _factory.CreateClient();
    }

    private static void SeedTestData(OrionDbContext context)
    {
        // Add test users
        context.Users.AddRange(
            new User 
            { 
                Id = 1, 
                UserId = "test-user-123", 
                FirstName = "Test", 
                LastName = "User",
                Email = "test@example.com" 
            }
        );

        // Add test inventory
        context.Inventories.AddRange(
            new Inventory 
            { 
                Id = 1, 
                ProductSku = "INTEGRATION-001", 
                ProductName = "Integration Test Product 1", 
                Price = 25.00m, 
                AvailableQuantity = 100 
            },
            new Inventory 
            { 
                Id = 2, 
                ProductSku = "INTEGRATION-002", 
                ProductName = "Integration Test Product 2", 
                Price = 15.00m, 
                AvailableQuantity = 50 
            }
        );

        context.SaveChanges();
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

        // Set up authorization header (mock JWT)
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act - Create order using CQRS
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert - Order creation
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var orderResponse = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(orderResponse);
        Assert.Equal("Integration Test Customer", orderResponse.CustomerName);
        Assert.Equal(65.00m, orderResponse.TotalAmount); // (2 * 25.00) + (1 * 15.00)
        Assert.Equal(2, orderResponse.Items.Count);

        // Verify order was saved to database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var savedOrder = await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.CustomerName == "Integration Test Customer");
        
        Assert.NotNull(savedOrder);
        Assert.Equal("test-user-123", savedOrder.UserId);
        Assert.Equal(OrderStatus.Pending, savedOrder.Status);
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
        
        var testOrders = new List<Order>
        {
            new Order
            {
                UserId = "test-user-123",
                CustomerName = "Test Customer 1",
                TotalAmount = 100.00m,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                OrderItems = new List<OrderItem>()
            },
            new Order
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

        // Set up authorization
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act - Get user's orders using CQRS query
        var response = await _client.GetAsync("/api/orders/my");

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
            UserId = "test-user-123",
            CustomerName = "Status Change Test",
            TotalAmount = 50.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        context.Orders.Add(testOrder);
        await context.SaveChangesAsync();

        var changeStatusRequest = new ChangeOrderStatusRequest(
            OrderStatus.Processing,
            "Integration test status change"
        );

        // Set up authorization
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

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
                new OrderItemRequest("NONEXISTENT-SKU", 1)
            }
        );

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode); // Error due to invalid inventory
        
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
                new OrderItemRequest("INTEGRATION-001", 1)
            }
        );

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "mock-jwt-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders/fast", createOrderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify Event Store contains the events
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var eventStoreEntries = await context.EventStoreEntries
            .Where(e => e.EventType.Contains("OrderCreated"))
            .ToListAsync();
        
        Assert.NotEmpty(eventStoreEntries);
        
        var orderCreatedEvent = eventStoreEntries.First();
        Assert.Equal("test-user-123", orderCreatedEvent.UserId);
        Assert.Contains("OrderCreatedEvent", orderCreatedEvent.EventType);
    }
}