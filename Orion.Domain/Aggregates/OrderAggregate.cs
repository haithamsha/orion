using Orion.Domain.Models;
using Orion.Domain.Events;

namespace Orion.Domain.Aggregates;

/// <summary>
/// Base class for all aggregate roots in Event Sourcing
/// Handles common functionality like tracking changes and versioning
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<BaseEvent> _uncommittedEvents = new();

    /// <summary>
    /// Unique identifier for this aggregate
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Current version of the aggregate (number of events applied)
    /// </summary>
    public int Version { get; protected set; } = 0;

    /// <summary>
    /// Get all uncommitted events that need to be saved
    /// </summary>
    public IEnumerable<BaseEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Mark all events as committed (called after saving to event store)
    /// </summary>
    public void MarkEventsAsCommitted() => _uncommittedEvents.Clear();

    /// <summary>
    /// Apply an event to this aggregate and add it to uncommitted events
    /// </summary>
    protected void ApplyChange(BaseEvent @event)
    {
        ApplyEvent(@event);
        _uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// Apply an event from the event store (already committed)
    /// </summary>
    public void ApplyEvent(BaseEvent @event)
    {
        ApplyEventInternal(@event);
        Version = @event.AggregateVersion;
    }

    /// <summary>
    /// Override this method to handle specific event types
    /// </summary>
    protected abstract void ApplyEventInternal(BaseEvent @event);
}

/// <summary>
/// Order Aggregate - represents the complete state of an order rebuilt from events
/// This is the Event Sourcing equivalent of the Order entity
/// </summary>
public class OrderAggregate : AggregateRoot
{
    // Current state properties - rebuilt from events
    public string CustomerUserId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public OrderStatus Status { get; private set; }
    public List<OrderItemDataDetailed> Items { get; private set; } = new();
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public bool IsRecoverable { get; private set; } = true;

    // Private constructor for EF and event replay
    private OrderAggregate() { }

    /// <summary>
    /// Create a new order aggregate by applying the OrderCreatedEvent
    /// </summary>
    public static OrderAggregate CreateNew(
        Guid orderId,
        string customerUserId,
        decimal totalAmount,
        List<OrderItemDataDetailed> items,
        string? notes = null,
        string? triggeredByUserId = null)
    {
        var aggregate = new OrderAggregate();
        var @event = new OrderCreatedEvent(
            orderId,
            customerUserId,
            totalAmount,
            items,
            notes,
            triggeredByUserId: triggeredByUserId);

        aggregate.ApplyChange(@event);
        return aggregate;
    }

    /// <summary>
    /// Reconstruct an order aggregate from its event history
    /// </summary>
    public static OrderAggregate FromEvents(IEnumerable<BaseEvent> events)
    {
        var aggregate = new OrderAggregate();
        foreach (var @event in events.OrderBy(e => e.AggregateVersion))
        {
            aggregate.ApplyEvent(@event);
        }
        return aggregate;
    }

    /// <summary>
    /// Change the status of the order
    /// </summary>
    public void ChangeStatus(OrderStatus newStatus, string? reason = null)
    {
        if (Status == OrderStatus.Completed || Status == OrderStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot change status from {Status} to {newStatus}.");
        }

        var @event = new OrderStatusChangedEvent(Id, Version + 1, Status, newStatus, reason, CustomerUserId);
        ApplyChange(@event);
    }

    public void SetUserId(string userId)
    {
        if (string.IsNullOrEmpty(CustomerUserId))
        {
            CustomerUserId = userId;
        }
    }

    /// <summary>
    /// Mark the order as completed
    /// </summary>
    public void Complete(string? completionNotes = null, string? triggeredByUserId = null)
    {
        if (Status == OrderStatus.Completed)
        {
            return; // Already completed
        }

        if (Status == OrderStatus.Failed)
        {
            throw new InvalidOperationException("Cannot complete a failed order");
        }

        var @event = new OrderCompletedEvent(
            Id,
            Version + 1,
            DateTime.UtcNow,
            completionNotes,
            triggeredByUserId);

        ApplyChange(@event);
    }

    /// <summary>
    /// Mark the order as failed
    /// </summary>
    public void Fail(string reason, bool isRecoverable = true, string? triggeredByUserId = null)
    {
        if (Status == OrderStatus.Failed)
        {
            return; // Already failed
        }

        if (Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Cannot fail a completed order");
        }

        var @event = new OrderFailedEvent(
            Id,
            Version + 1,
            reason,
            DateTime.UtcNow,
            isRecoverable,
            triggeredByUserId);

        ApplyChange(@event);
    }

    /// <summary>
    /// Start processing the order
    /// </summary>
    public void StartProcessing(string? triggeredByUserId = null)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start processing order in {Status} status");
        }

        var @event = new OrderProcessingStartedEvent(
            Id,
            Version + 1,
            DateTime.UtcNow,
            triggeredByUserId);

        ApplyChange(@event);
    }

    /// <summary>
    /// Apply events to rebuild the aggregate state
    /// This is where the "magic" of Event Sourcing happens
    /// </summary>
    protected override void ApplyEventInternal(BaseEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent orderCreated:
                Apply(orderCreated);
                break;
            case OrderStatusChangedEvent statusChanged:
                Apply(statusChanged);
                break;
            case OrderCompletedEvent orderCompleted:
                Apply(orderCompleted);
                break;
            case OrderFailedEvent orderFailed:
                Apply(orderFailed);
                break;
            case OrderProcessingStartedEvent processingStarted:
                Apply(processingStarted);
                break;

            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    // Event application methods - these rebuild the state
    private void Apply(OrderCreatedEvent @event)
    {
        Id = @event.AggregateId;
        CustomerUserId = @event.CustomerUserId;
        TotalAmount = @event.TotalAmount;
        Currency = @event.Currency;
        Status = @event.Status;
        Items = @event.Items.ToList();
        Notes = @event.Notes;
        CreatedAt = @event.OccurredAt;
    }

    private void Apply(OrderStatusChangedEvent @event)
    {
        Status = @event.NewStatus;
    }

    private void Apply(OrderCompletedEvent @event)
    {
        Status = OrderStatus.Completed;
        CompletedAt = @event.CompletedAt;
    }

    private void Apply(OrderFailedEvent @event)
    {
        Status = OrderStatus.Failed;
        FailedAt = @event.FailedAt;
        FailureReason = @event.Reason;
        IsRecoverable = @event.IsRecoverable;
    }

    private void Apply(OrderProcessingStartedEvent @event)
    {
        Status = OrderStatus.Processing;
    }
}