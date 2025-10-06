using Orion.Domain.Models;

namespace Orion.Application.Interfaces;

public interface IProjectionService
{
    Task UpdateOrderProjectionsAsync(Order order);
    Task DeleteOrderProjectionsAsync(int orderId);
}