
using Orion.Domain.Models;

namespace Orion.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedInventoryAsync(OrionDbContext context)
    {
        // Check if we already have inventory data
        if (context.Inventories.Any())
        {
            return; // Data already exists
        }

        var inventoryItems = new List<Inventory>
        {
            new Inventory
            {
                ProductName = "Wireless Headphones",
                ProductSku = "WH-001",
                Price = 99.99m,
                AvailableQuantity = 50,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductName = "Bluetooth Speaker",
                ProductSku = "BS-002",
                Price = 79.99m,
                AvailableQuantity = 30,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductName = "Smartphone Case",
                ProductSku = "SC-003",
                Price = 24.99m,
                AvailableQuantity = 100,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductName = "USB-C Cable",
                ProductSku = "UC-004",
                Price = 12.99m,
                AvailableQuantity = 200,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Inventories.AddRange(inventoryItems);
        await context.SaveChangesAsync();
    }
}