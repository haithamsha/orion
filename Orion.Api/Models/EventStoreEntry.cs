using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Models;

/// <summary>
/// Entity Framework model for storing events in the database
/// Represents a single event in the event store
/// </summary>
public class EventStoreEntry
{
    /// <summary>
    /// Primary key for the database table
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The ID of the aggregate this event belongs to
    /// </summary>
    [Required]
    public Guid AggregateId { get; set; }

    /// <summary>
    /// The version of the aggregate after this event was applied
    /// </summary>
    public int AggregateVersion { get; set; }

    /// <summary>
    /// The unique ID of this specific event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// The type name of the event (used for deserialization)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized event data
    /// </summary>
    [Required]
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// User who triggered this event (optional)
    /// </summary>
    [MaxLength(50)]
    public string? UserId { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}