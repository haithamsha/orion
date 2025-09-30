namespace Orion.Api.Services;

public interface IOrderProcessingService
{
    Task ProcessOrder(int orderId);
}