namespace Orion.Api.Models.Events;

public abstract record BaseEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
}