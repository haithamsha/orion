namespace Orion.Api.Services;
using Orion.Api.Models.Events;
public interface IEventStore
{
    Task SaveEventsAsync(string aggregateId, IEnumerable<BaseEvent> events, int expectedVersion);
    Task<IEnumerable<BaseEvent>> GetEventsAsync(string aggregateId);
    Task<IEnumerable<BaseEvent>> GetEventsAsync(string aggregateId, int fromVersion);
}