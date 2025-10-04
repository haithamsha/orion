namespace Orion.Api.Models.EventStore;

public class EventStoreEntry
{
    public long Id { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty; // JSON
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid EventId { get; set; }
}