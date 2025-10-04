namespace Orion.Api.Models.Events;

/// <summary>
/// Base class for all domain events in the system.
/// Contains common metadata that every event should have.
/// </summary>
public abstract record BaseEvent
{
    /// <summary>
    /// Unique identifier for this specific event occurrence
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the aggregate root that this event belongs to
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// The version of the aggregate after this event is applied
    /// Used for optimistic concurrency control
    /// </summary>
    public int AggregateVersion { get; init; }

    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The type name of the event - useful for deserialization
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Optional user ID who triggered this event
    /// </summary>
    public string? UserId { get; init; }

    protected BaseEvent(Guid aggregateId, int aggregateVersion, string? userId = null)
    {
        AggregateId = aggregateId;
        AggregateVersion = aggregateVersion;
        UserId = userId;
        EventType = GetType().Name;
    }
}