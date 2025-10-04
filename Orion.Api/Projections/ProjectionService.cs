using Orion.Api.Models.Events;

namespace Orion.Api.Projections;

/// <summary>
/// Service that coordinates projection updates when events occur
/// </summary>
public interface IProjectionService
{
    Task ProjectEventAsync(BaseEvent @event, CancellationToken cancellationToken = default);
    Task ProjectEventsAsync(IEnumerable<BaseEvent> events, CancellationToken cancellationToken = default);
}

public class ProjectionService : IProjectionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectionService> _logger;

    public ProjectionService(IServiceProvider serviceProvider, ILogger<ProjectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProjectEventAsync(BaseEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing projection for event: {EventType} - {AggregateId}", 
            @event.GetType().Name, @event.AggregateId);

        try
        {
            // Route events to appropriate handlers
            switch (@event)
            {
                case OrderCreatedEvent orderCreated:
                    await HandleEvent(orderCreated, cancellationToken);
                    break;
                
                case OrderStatusChangedEvent statusChanged:
                    await HandleEvent(statusChanged, cancellationToken);
                    break;
                
                case OrderCompletedEvent orderCompleted:
                    await HandleEvent(orderCompleted, cancellationToken);
                    break;
                
                case OrderFailedEvent orderFailed:
                    await HandleEvent(orderFailed, cancellationToken);
                    break;
                
                default:
                    _logger.LogWarning("No projection handler found for event type: {EventType}", @event.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing projection for event: {EventType} - {AggregateId}", 
                @event.GetType().Name, @event.AggregateId);
            throw;
        }
    }

    public async Task ProjectEventsAsync(IEnumerable<BaseEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await ProjectEventAsync(@event, cancellationToken);
        }
    }

    private async Task HandleEvent<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : BaseEvent
    {
        var handlers = _serviceProvider.GetServices<IProjectionHandler<TEvent>>();
        
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}