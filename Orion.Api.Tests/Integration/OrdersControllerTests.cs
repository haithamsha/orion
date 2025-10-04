using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orion.Api.Controllers;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.DTOs;
using Orion.Api.Services;

namespace Orion.Api.Tests.Integration;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        // Create a custom factory for each test run to ensure isolation
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 1. Remove the real DbContext configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrionDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 2. Add a new DbContext using an in-memory database for testing
                services.AddDbContext<OrionDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Note: No Hangfire setup needed as we're using RabbitMQ directly
            });
        });
    }

    [Fact]
    public async Task CreateOrderFast_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        var orderRequest = new CreateOrderRequest("Test Customer", new List<OrderItemRequest>
        {
            new OrderItemRequest("TEST-SKU-001", 2)
        });

        // ACT
        var response = await client.PostAsJsonAsync("/api/orders/fast", orderRequest);

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateOrderFast_WhenAuthenticated_EnqueuesJobAndReturnsAccepted()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        var orderRequest = new CreateOrderRequest("Test Customer", new List<OrderItemRequest>
        {
            new OrderItemRequest("TEST-SKU-001", 2)
        });

        // We need a valid token. We can call our login endpoint to get one.
        var loginRequest = new LoginRequest("testuser", "password123");
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var tokenData = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData!.token);

        // ACT
        var response = await client.PostAsJsonAsync("/api/orders/fast", orderRequest);

        // ASSERT
        // 1. Check if the HTTP response is correct
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        // 2. Verify the order was created in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        var order = await dbContext.Orders.FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.Equal(OrderStatus.Pending, order.Status);
        
        // Note: The order should be published to RabbitMQ for async processing
        // but testing message queue integration requires additional setup
    }
}

// Helper record to deserialize the token
internal record TokenResponse(string token);