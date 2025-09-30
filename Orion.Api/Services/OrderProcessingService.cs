using Orion.Api.Data;
using Orion.Api.Models;

namespace Orion.Api.Services;

public class OrderProcessingService : IOrderProcessingService
{
    private readonly ILogger<OrderProcessingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderProcessingService(ILogger<OrderProcessingService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessOrder(int orderId)
    {
        // Hangfire jobs run in a different scope, so we must create a new one
        // to get a fresh DbContext instance.
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();

        var order = await dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogError("Order with ID {OrderId} not found.", orderId);
            return;
        }

        _logger.LogInformation("Starting BACKGROUND processing for Order ID: {OrderId}", orderId);
        order.Status = OrderStatus.Processing;
        await dbContext.SaveChangesAsync();

        try // --- START OF THE NEW TRY/CATCH BLOCK ---
        {
            // 1. Simulate calling a slow payment gateway API (3 seconds)
            _logger.LogInformation("Processing payment for Order ID: {OrderId}...", orderId);
            await Task.Delay(3000);

            // *** SIMULATE FAILURE FOR A SPECIFIC CASE ***
            if (order.TotalAmount == 666)
            {
                throw new InvalidOperationException("Simulated payment gateway failure: Invalid card.");
            }

            // 2. Simulate updating inventory (0.5 seconds)
            _logger.LogInformation("Updating inventory for Order ID: {OrderId}...", orderId);
            await Task.Delay(500);

            // 4. Simulate calling a slow email service API (2 seconds)
            _logger.LogInformation("Sending confirmation email for Order ID: {OrderId}...", orderId);
            await Task.Delay(2000);

            // If we reach here, everything was successful
            order.Status = OrderStatus.Completed;
            _logger.LogInformation("FINISHED BACKGROUND processing for Order ID: {OrderId}", orderId);
        }
        catch (Exception ex) // --- CATCH ANY FAILURE ---
        {
            _logger.LogError(ex, "Error processing Order ID: {OrderId}", orderId);
            order.Status = OrderStatus.Failed; // Set the status to Failed
            // NOTE: In a real app, you might also store the error message in the order record.
        }
        finally // --- ALWAYS RUN THIS ---
        {
            // Save the final status (Completed or Failed) to the database
            await dbContext.SaveChangesAsync();
        }
    }
}