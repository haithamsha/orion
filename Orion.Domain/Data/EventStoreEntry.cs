namespace Orion.Domain.Data;

public class EventStoreEntry
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
