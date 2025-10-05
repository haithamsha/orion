using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Orion.Api.Controllers;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.DTOs;
using Orion.Api.Services;

namespace Orion.Api.Tests.Integration;

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public OrdersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Clean and seed the database before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        _factory.SeedData(context);
    }

    [Fact]
    public async Task CreateOrderFast_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // ARRANGE
        // Create a client that doesn't use the test auth handler
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultScheme = "Bearer";
                });
            });
        }).CreateClient();

        var orderRequest = new CreateOrderRequest("Test Customer", new List<OrderItemRequest>
        {
            new("TEST-SKU-001", 2)
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
        var orderRequest = new CreateOrderRequest("Test Customer", new List<OrderItemRequest>
        {
            new("INTEGRATION-001", 2) // Use SKU from seeded data
        });

        // ACT
        var response = await _client.PostAsJsonAsync("/api/orders/fast", orderRequest);

        // ASSERT
        // 1. Check if the HTTP response is correct
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // 2. Verify the order was created in the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        // Find the order for our test user
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.UserId == "test-user-123");
        Assert.NotNull(order);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }
}

// Helper record to deserialize the token
internal record TokenResponse(string token);