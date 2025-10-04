using Orion.Api.Models;
using Orion.Api.Models.Aggregates;
using Orion.Api.Models.Events;

namespace Orion.Api.Tests.Unit.EventSourcing;

public class OrderAggregateTests
{
    [Fact]
    public void CreateNew_ShouldGenerateOrderCreatedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerUserId = "user123";
        var totalAmount = 25.50m;
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 2, 
                UnitPrice = 12.75m,
                TotalPrice = 25.50m
            }
        };

        // Act
        var aggregate = OrderAggregate.CreateNew(orderId, customerUserId, totalAmount, items);

        // Assert
        Assert.Equal(orderId, aggregate.Id);
        Assert.Equal(customerUserId, aggregate.CustomerUserId);
        Assert.Equal(totalAmount, aggregate.TotalAmount);
        Assert.Equal(OrderStatus.Pending, aggregate.Status);
        Assert.Equal(1, aggregate.Version);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(uncommittedEvents);
        Assert.IsType<OrderCreatedEvent>(uncommittedEvents[0]);
    }

    [Fact]
    public void ChangeStatus_FromPendingToProcessing_ShouldGenerateStatusChangedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted(); // Simulate saving to event store

        // Act
        aggregate.ChangeStatus(OrderStatus.Processing, "Starting order processing");

        // Assert
        Assert.Equal(OrderStatus.Processing, aggregate.Status);
        Assert.Equal(2, aggregate.Version);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(uncommittedEvents);
        var statusChangedEvent = Assert.IsType<OrderStatusChangedEvent>(uncommittedEvents[0]);
        Assert.Equal(OrderStatus.Pending, statusChangedEvent.PreviousStatus);
        Assert.Equal(OrderStatus.Processing, statusChangedEvent.NewStatus);
        Assert.Equal("Starting order processing", statusChangedEvent.Reason);
    }

    [Fact]
    public void Complete_FromProcessingStatus_ShouldGenerateCompletedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();
        aggregate.ChangeStatus(OrderStatus.Processing);
        aggregate.MarkEventsAsCommitted();

        // Act
        aggregate.Complete("Order fulfilled successfully");

        // Assert
        Assert.Equal(OrderStatus.Completed, aggregate.Status);
        Assert.NotNull(aggregate.CompletedAt);
        Assert.Equal(3, aggregate.Version);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(uncommittedEvents);
        var completedEvent = Assert.IsType<OrderCompletedEvent>(uncommittedEvents[0]);
        Assert.Equal("Order fulfilled successfully", completedEvent.CompletionNotes);
    }

    [Fact]
    public void Complete_FromCompletedStatus_ShouldNotGenerateEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();
        aggregate.ChangeStatus(OrderStatus.Processing);
        aggregate.MarkEventsAsCommitted();
        aggregate.Complete();
        aggregate.MarkEventsAsCommitted();

        // Act
        aggregate.Complete("Trying to complete again");

        // Assert
        Assert.Equal(OrderStatus.Completed, aggregate.Status);
        Assert.Empty(aggregate.GetUncommittedEvents());
    }

    [Fact]
    public void Fail_WithValidReason_ShouldGenerateFailedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();

        // Act
        aggregate.Fail("Payment processing failed", isRecoverable: true);

        // Assert
        Assert.Equal(OrderStatus.Failed, aggregate.Status);
        Assert.NotNull(aggregate.FailedAt);
        Assert.Equal("Payment processing failed", aggregate.FailureReason);
        Assert.True(aggregate.IsRecoverable);
        Assert.Equal(2, aggregate.Version);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(uncommittedEvents);
        var failedEvent = Assert.IsType<OrderFailedEvent>(uncommittedEvents[0]);
        Assert.Equal("Payment processing failed", failedEvent.Reason);
        Assert.True(failedEvent.IsRecoverable);
    }

    [Fact]
    public void StartProcessing_FromPendingStatus_ShouldGenerateProcessingStartedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();

        // Act
        aggregate.StartProcessing();

        // Assert
        Assert.Equal(OrderStatus.Processing, aggregate.Status);
        Assert.Equal(2, aggregate.Version);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        Assert.Single(uncommittedEvents);
        Assert.IsType<OrderProcessingStartedEvent>(uncommittedEvents[0]);
    }

    [Fact]
    public void StartProcessing_FromNonPendingStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();
        aggregate.StartProcessing();
        aggregate.MarkEventsAsCommitted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => aggregate.StartProcessing());
    }

    [Fact]
    public void FromEvents_WithEventHistory_ShouldReconstructAggregateCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var items = new List<OrderItemDataDetailed>
        {
            new() 
            { 
                InventoryId = Guid.NewGuid(),
                ProductSku = "TEST-001", 
                ProductName = "Test Product", 
                Quantity = 2, 
                UnitPrice = 15.00m,
                TotalPrice = 30.00m
            }
        };

        var events = new List<BaseEvent>
        {
            new OrderCreatedEvent(orderId, "user123", 30.00m, items),
            new OrderStatusChangedEvent(orderId, 2, OrderStatus.Pending, OrderStatus.Processing),
            new OrderCompletedEvent(orderId, 3, DateTime.UtcNow, "Successfully processed")
        };

        // Act
        var aggregate = OrderAggregate.FromEvents(events);

        // Assert
        Assert.Equal(orderId, aggregate.Id);
        Assert.Equal("user123", aggregate.CustomerUserId);
        Assert.Equal(30.00m, aggregate.TotalAmount);
        Assert.Equal(OrderStatus.Completed, aggregate.Status);
        Assert.Equal(3, aggregate.Version);
        Assert.Single(aggregate.Items);
        Assert.NotNull(aggregate.CompletedAt);
        Assert.Empty(aggregate.GetUncommittedEvents()); // No uncommitted events when reconstructing
    }

    [Fact]
    public void ChangeStatus_FromCompletedToAny_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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

        var aggregate = OrderAggregate.CreateNew(orderId, "user123", 10.00m, items);
        aggregate.MarkEventsAsCommitted();
        aggregate.Complete();
        aggregate.MarkEventsAsCommitted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            aggregate.ChangeStatus(OrderStatus.Processing));
    }
}