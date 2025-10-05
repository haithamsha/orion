using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.Aggregates;
using Orion.Api.Models.Events;
using Orion.Api.Services.EventSourcing;

namespace Orion.Api.Tests.Unit.EventSourcing;

public class EventStoreTests : IDisposable
{
    private readonly OrionDbContext _context;
    private readonly EventStore _eventStore;
    private readonly Mock<ILogger<EventStore>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public EventStoreTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<OrionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrionDbContext(options);
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<EventStore>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _eventStore = new EventStore(_context, _mockLogger.Object, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task SaveEventsAsync_WithNewAggregate_ShouldSaveEventsSuccessfully()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 2, 
                UnitPrice = 10.00m,
                TotalPrice = 20.00m
            }
        };

        var orderCreatedEvent = new OrderCreatedEvent(
            aggregateId,
            "user123",
            20.00m,
            items);

        var events = new List<BaseEvent> { orderCreatedEvent };

        // Act
        await _eventStore.SaveEventsAsync(aggregateId, events, 0);

        // Assert
        var savedEvents = await _context.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId)
            .ToListAsync();

        Assert.Single(savedEvents);
        Assert.Equal(aggregateId, savedEvents[0].AggregateId);
        Assert.Equal(1, savedEvents[0].AggregateVersion);
        Assert.Equal(nameof(OrderCreatedEvent), savedEvents[0].EventType);
    }

    [Fact]
    public async Task SaveEventsAsync_WithConcurrencyConflict_ShouldThrowConcurrencyException()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 1, 
                UnitPrice = 10.00m,
                TotalPrice = 10.00m
            }
        };

        // First save an event to establish version 1
        var firstEvent = new OrderCreatedEvent(aggregateId, "user123", 10.00m, items);
        await _eventStore.SaveEventsAsync(aggregateId, new[] { firstEvent }, 0);

        // Try to save another event with wrong expected version
        var secondEvent = new OrderStatusChangedEvent(aggregateId, 2, OrderStatus.Pending, OrderStatus.Processing);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            _eventStore.SaveEventsAsync(aggregateId, new[] { secondEvent }, 0));
    }

    [Fact]
    public async Task GetEventsAsync_WithExistingEvents_ShouldReturnEventsInOrder()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 1, 
                UnitPrice = 10.00m,
                TotalPrice = 10.00m
            }
        };

        var events = new List<BaseEvent>
        {
            new OrderCreatedEvent(aggregateId, "user123", 10.00m, items),
            new OrderStatusChangedEvent(aggregateId, 2, OrderStatus.Pending, OrderStatus.Processing),
            new OrderCompletedEvent(aggregateId, 3, DateTime.UtcNow)
        };

        // Save events one by one to simulate proper sequencing
        await _eventStore.SaveEventsAsync(aggregateId, new[] { events[0] }, 0);
        await _eventStore.SaveEventsAsync(aggregateId, new[] { events[1] }, 1);
        await _eventStore.SaveEventsAsync(aggregateId, new[] { events[2] }, 2);

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        var eventList = retrievedEvents.ToList();
        Assert.Equal(3, eventList.Count);
        Assert.IsType<OrderCreatedEvent>(eventList[0]);
        Assert.IsType<OrderStatusChangedEvent>(eventList[1]);
        Assert.IsType<OrderCompletedEvent>(eventList[2]);

        // Verify order by checking aggregate versions
        Assert.Equal(1, eventList[0].AggregateVersion);
        Assert.Equal(2, eventList[1].AggregateVersion);
        Assert.Equal(3, eventList[2].AggregateVersion);
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithExistingEvents_ShouldReturnCorrectVersion()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 1, 
                UnitPrice = 10.00m,
                TotalPrice = 10.00m
            }
        };

        var event1 = new OrderCreatedEvent(aggregateId, "user123", 10.00m, items);
        var event2 = new OrderStatusChangedEvent(aggregateId, 2, OrderStatus.Pending, OrderStatus.Processing);

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, 0);
        await _eventStore.SaveEventsAsync(aggregateId, new[] { event2 }, 1);

        // Act
        var currentVersion = await _eventStore.GetCurrentVersionAsync(aggregateId);

        // Assert
        Assert.Equal(2, currentVersion);
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithNoEvents_ShouldReturnZero()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var currentVersion = await _eventStore.GetCurrentVersionAsync(aggregateId);

        // Assert
        Assert.Equal(0, currentVersion);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}