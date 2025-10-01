using Orion.Api.Models;

namespace Orion.Api.Services;

public interface IMessagePublisher
{
    Task PublishOrderPlacedAsync(OrderPlacedEvent orderEvent);
}