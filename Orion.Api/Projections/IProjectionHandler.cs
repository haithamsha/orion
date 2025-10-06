using Orion.Api.Models.Events;

namespace Orion.Api.Projections;

/// <summary>
/// Interface for projection handlers that update read models based on events
/// </summary>
public interface IProjectionHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for all projection handlers
/// </summary>
public interface IProjectionHandler
{
    Task HandleAsync(BaseEvent @event, CancellationToken cancellationToken = default);
}