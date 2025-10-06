using Orion.Domain.Models;
using Orion.Domain.Events;

namespace Orion.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishOrderCreatedAsync(Order order);
    Task PublishOrderStatusChangedAsync(Order order);
    Task PublishOrderPlacedAsync(OrderCreatedEvent orderEvent);
}