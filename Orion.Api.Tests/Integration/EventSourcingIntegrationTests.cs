using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.Aggregates;
using Orion.Api.Models.Events;
using Orion.Api.Services.EventSourcing;
using Xunit;

namespace Orion.Api.Tests.Integration;

public class EventSourcingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public EventSourcingIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EventStore_ShouldPersistEventsToDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var aggregateId = Guid.NewGuid();
        var baseItem = new OrderItemData("ITEM-001", "Test Item", 2, 10.50m);
        var orderItems = new List<OrderItemDataDetailed>
        {
            new(baseItem, Guid.NewGuid())
        };
        
        // Create an order aggregate and generate events
        var orderAggregate = OrderAggregate.CreateNew(
            aggregateId,
            "test-user-123", // Use the authenticated user
            21.00m, // totalAmount: 2 * 10.50
            orderItems,
            triggeredByUserId: "test-user-123"
        );

        // Act - Save events to database
        await eventStore.SaveEventsAsync(aggregateId, orderAggregate.GetUncommittedEvents(), 0);
        
        // Assert - Check if events are persisted in database
        var eventsInDb = await dbContext.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.AggregateVersion)
            .ToListAsync();
        
        Assert.Single(eventsInDb);
        Assert.Equal("OrderCreatedEvent", eventsInDb[0].EventType);
        Assert.Equal(1, eventsInDb[0].AggregateVersion);
        Assert.Equal("test-user-123", eventsInDb[0].UserId); // Assert against the correct user
        Assert.Contains("ITEM-001", eventsInDb[0].EventData);
    }

    [Fact]
    public async Task EventStore_ShouldReconstructAggregateFromPersistedEvents()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        
        var aggregateId = Guid.NewGuid();
        var baseItem = new OrderItemData("ITEM-002", "Test Item 2", 1, 25.00m);
        var orderItems = new List<OrderItemDataDetailed>
        {
            new(baseItem, Guid.NewGuid())
        };
        
        // Create and persist initial order
        var originalOrder = OrderAggregate.CreateNew(aggregateId, "test-user-123", 25.00m, orderItems);
        await eventStore.SaveEventsAsync(aggregateId, originalOrder.GetUncommittedEvents(), 0);
        
        // Perform some business operations
        originalOrder.MarkEventsAsCommitted();
        originalOrder.StartProcessing();
        await eventStore.SaveEventsAsync(aggregateId, originalOrder.GetUncommittedEvents(), 1);
        
        originalOrder.MarkEventsAsCommitted();
        originalOrder.Complete();
        await eventStore.SaveEventsAsync(aggregateId, originalOrder.GetUncommittedEvents(), 2);

        // Act - Reconstruct aggregate from events
        var events = await eventStore.GetEventsAsync(aggregateId);
        var reconstructedOrder = OrderAggregate.FromEvents(events);

        // Assert
        Assert.Equal(aggregateId, reconstructedOrder.Id);
        Assert.Equal(OrderStatus.Completed, reconstructedOrder.Status);
        Assert.Equal(3, reconstructedOrder.Version);
        Assert.Equal("test-user-123", reconstructedOrder.CustomerUserId);
        Assert.Single(reconstructedOrder.Items);
        Assert.Equal("ITEM-002", reconstructedOrder.Items.First().ProductSku);
    }

    [Fact]
    public async Task EventStore_ShouldHandleMultipleOrdersAndEvents()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
        
        var order1Id = Guid.NewGuid();
        var order2Id = Guid.NewGuid();
        
        // Create first order
        var baseItem1 = new OrderItemData("SKU-1", "Product 1", 1, 15.00m);
        var order1 = OrderAggregate.CreateNew(order1Id, "test-user-123", 15.00m, new List<OrderItemDataDetailed>
        {
            new(baseItem1, Guid.NewGuid())
        });
        
        // Create second order
        var baseItem2 = new OrderItemData("SKU-2", "Product 2", 3, 8.50m);
        var order2 = OrderAggregate.CreateNew(order2Id, "test-user-456", 25.50m, new List<OrderItemDataDetailed>
        {
            new(baseItem2, Guid.NewGuid())
        });

        // Act - Save both orders
        await eventStore.SaveEventsAsync(order1Id, order1.GetUncommittedEvents(), 0);
        await eventStore.SaveEventsAsync(order2Id, order2.GetUncommittedEvents(), 0);
        
        // Add more events to first order
        order1.MarkEventsAsCommitted();
        order1.StartProcessing();
        await eventStore.SaveEventsAsync(order1Id, order1.GetUncommittedEvents(), 1);

        // Assert - Check total events in database
        var totalEvents = await dbContext.EventStoreEntries.CountAsync();
        var order1Events = await dbContext.EventStoreEntries
            .Where(e => e.AggregateId == order1Id)
            .CountAsync();
        var order2Events = await dbContext.EventStoreEntries
            .Where(e => e.AggregateId == order2Id)
            .CountAsync();
        
        Assert.True(totalEvents >= 3); // At least 3 events from this test
        Assert.Equal(2, order1Events); // OrderCreated + ProcessingStarted
        Assert.Equal(1, order2Events); // OrderCreated only
    }
}