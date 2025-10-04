using Orion.Api.Models.Events;

namespace Orion.Api.Services.EventSourcing;

/// <summary>
/// Interface for the Event Store - responsible for persisting and retrieving events
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Save a list of events for a specific aggregate
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <param name="events">The events to save</param>
    /// <param name="expectedVersion">The expected current version of the aggregate (for optimistic concurrency)</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<BaseEvent> events, int expectedVersion);

    /// <summary>
    /// Get all events for a specific aggregate, ordered by version
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <param name="fromVersion">Optional: Start from this version (inclusive)</param>
    /// <returns>All events for the aggregate</returns>
    Task<IEnumerable<BaseEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0);

    /// <summary>
    /// Get the current version of an aggregate (highest version number)
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <returns>The current version, or 0 if no events exist</returns>
    Task<int> GetCurrentVersionAsync(Guid aggregateId);
}