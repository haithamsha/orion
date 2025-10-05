using Microsoft.EntityFrameworkCore;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.Events;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Orion.Api.Services.EventSourcing;

/// <summary>
/// Entity Framework implementation of the Event Store
/// Handles persisting and retrieving events from the database
/// </summary>
public class EventStore : IEventStore
{
    private readonly OrionDbContext _context;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<EventStore> _logger;

    public EventStore(OrionDbContext context, ILogger<EventStore> logger, IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Save events to the database with optimistic concurrency control
    /// </summary>
    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<BaseEvent> events, int expectedVersion)
    {
        var eventsList = events.ToList();
        if (!eventsList.Any())
        {
            _logger.LogWarning("Attempted to save empty events list for aggregate {AggregateId}", aggregateId);
            return;
        }

        _logger.LogInformation("Saving {EventCount} events for aggregate {AggregateId}", eventsList.Count, aggregateId);

        // Check current version for optimistic concurrency control
        var currentVersion = await GetCurrentVersionAsync(aggregateId);
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion);
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Convert domain events to database entities
        var eventEntries = eventsList.Select(evt => new EventStoreEntry
        {
            AggregateId = evt.AggregateId,
            AggregateVersion = evt.AggregateVersion,
            EventId = evt.EventId,
            EventType = evt.EventType,
            EventData = JsonSerializer.Serialize(evt, evt.GetType()),
            OccurredAt = evt.OccurredAt,
            UserId = userId ?? evt.UserId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        try
        {
            await _context.EventStoreEntries.AddRangeAsync(eventEntries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully saved {EventCount} events for aggregate {AggregateId}", 
                eventsList.Count, aggregateId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save events for aggregate {AggregateId}", aggregateId);
            
            // Check if it's a concurrency issue
            var newCurrentVersion = await GetCurrentVersionAsync(aggregateId);
            if (newCurrentVersion != expectedVersion)
            {
                throw new ConcurrencyException(aggregateId, expectedVersion, newCurrentVersion);
            }
            
            throw;
        }
    }

    /// <summary>
    /// Get all events for an aggregate, ordered by version
    /// </summary>
    public async Task<IEnumerable<BaseEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0)
    {
        _logger.LogDebug("Retrieving events for aggregate {AggregateId} from version {FromVersion}", 
            aggregateId, fromVersion);

        var eventEntries = await _context.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId && e.AggregateVersion > fromVersion)
            .OrderBy(e => e.AggregateVersion)
            .ToListAsync();

        _logger.LogDebug("Found {EventCount} events for aggregate {AggregateId}", 
            eventEntries.Count, aggregateId);

        // Deserialize events back to domain events
        var events = new List<BaseEvent>();
        foreach (var entry in eventEntries)
        {
            try
            {
                var eventType = Type.GetType(entry.EventType);
                if (eventType == null)
                {
                    // Try to find the type in the current assembly
                    eventType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == entry.EventType || t.FullName == entry.EventType);
                }

                if (eventType != null && eventType.IsSubclassOf(typeof(BaseEvent)))
                {
                    var deserializedEvent = JsonSerializer.Deserialize(entry.EventData, eventType) as BaseEvent;
                    if (deserializedEvent != null)
                    {
                        events.Add(deserializedEvent);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize event {EventId} of type {EventType}", 
                            entry.EventId, entry.EventType);
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown event type {EventType} for event {EventId}", 
                        entry.EventType, entry.EventId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize event {EventId} of type {EventType}", 
                    entry.EventId, entry.EventType);
            }
        }

        return events;
    }

    /// <summary>
    /// Get the current version of an aggregate
    /// </summary>
    public async Task<int> GetCurrentVersionAsync(Guid aggregateId)
    {
        var maxVersion = await _context.EventStoreEntries
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.AggregateVersion);

        return maxVersion ?? 0;
    }
}