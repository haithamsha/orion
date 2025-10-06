using Orion.Domain.Models;

namespace Orion.Application.Interfaces;

public record InventoryReservationRequest(string ProductSku, int Quantity);
public record InventoryReservationResult(bool Success, string Message, int? ReservationId = null);

public interface IInventoryService
{
    Task<bool> IsProductAvailableAsync(string productSku, int quantity);
    Task<Inventory?> GetProductBySkuAsync(string productSku);
    Task<InventoryReservationResult> ReserveInventoryAsync(List<InventoryReservationRequest> items);
    Task<bool> ConfirmReservationAsync(int orderId);
    Task<bool> RollbackReservationAsync(int orderId);
    Task<List<Inventory>> GetAvailableProductsAsync();
}