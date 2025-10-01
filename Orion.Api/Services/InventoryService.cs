using Microsoft.EntityFrameworkCore;
using Orion.Api.Data;
using Orion.Api.Models;

namespace Orion.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(OrionDbContext dbContext, ILogger<InventoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsProductAvailableAsync(string productSku, int quantity)
    {
        var product = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductSku == productSku);

        return product != null && product.AvailableQuantity >= quantity;
    }

    public async Task<Inventory?> GetProductBySkuAsync(string productSku)
    {
        return await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductSku == productSku);
    }

    public async Task<InventoryReservationResult> ReserveInventoryAsync(List<InventoryReservationRequest> items)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            foreach (var item in items)
            {
                var product = await _dbContext.Inventories
                    .FirstOrDefaultAsync(i => i.ProductSku == item.ProductSku);

                if (product == null)
                {
                    return new InventoryReservationResult(false, $"Product {item.ProductSku} not found");
                }

                if (product.AvailableQuantity < item.Quantity)
                {
                    return new InventoryReservationResult(false, 
                        $"Insufficient stock for {item.ProductSku}. Available: {product.AvailableQuantity}, Requested: {item.Quantity}");
                }

                // Reserve the inventory
                product.AvailableQuantity -= item.Quantity;
                product.ReservedQuantity += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully reserved inventory for {ItemCount} items", items.Count);
            return new InventoryReservationResult(true, "Inventory reserved successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to reserve inventory");
            return new InventoryReservationResult(false, "Failed to reserve inventory");
        }
    }

    public async Task<bool> ConfirmReservationAsync(int orderId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            var orderItems = await _dbContext.OrderItems
                .Include(oi => oi.Inventory)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var orderItem in orderItems)
            {
                // Move from reserved to sold (remove from reserved, don't add back to available)
                orderItem.Inventory.ReservedQuantity -= orderItem.Quantity;
                orderItem.Inventory.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully confirmed inventory reservation for Order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to confirm inventory reservation for Order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<bool> RollbackReservationAsync(int orderId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            var orderItems = await _dbContext.OrderItems
                .Include(oi => oi.Inventory)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var orderItem in orderItems)
            {
                // Return reserved inventory back to available
                orderItem.Inventory.AvailableQuantity += orderItem.Quantity;
                orderItem.Inventory.ReservedQuantity -= orderItem.Quantity;
                orderItem.Inventory.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully rolled back inventory reservation for Order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to rollback inventory reservation for Order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<List<Inventory>> GetAvailableProductsAsync()
    {
        return await _dbContext.Inventories
            .Where(i => i.AvailableQuantity > 0)
            .OrderBy(i => i.ProductName)
            .ToListAsync();
    }
}