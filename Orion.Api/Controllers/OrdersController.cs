using Microsoft.AspNetCore.Mvc;
using Orion.Api.Data;
using Orion.Api.Models;

namespace Orion.Api.Controllers;

// DTO to represent the incoming request payload
public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrionDbContext dbContext, ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("slow")]
    public async Task<IActionResult> CreateOrderSlow([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Starting SLOW order creation for {Customer}", request.CustomerName);

        // --- Start of the slow and problematic work ---

        // 1. Simulate calling a slow payment gateway API (e.g., 3 seconds)
        _logger.LogInformation("Processing payment...");
        await Task.Delay(3000);
        _logger.LogInformation("Payment processed.");

        // 2. Simulate updating inventory (DB work)
        _logger.LogInformation("Updating inventory...");
        await Task.Delay(500);
        _logger.LogInformation("Inventory updated.");

        // 3. Save the order to our database
        var order = new Order
        {
            CustomerName = request.CustomerName,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order saved to database with ID {OrderId}", order.Id);

        // 4. Simulate calling a slow email service API (e.g., 2 seconds)
        _logger.LogInformation("Sending confirmation email...");
        await Task.Delay(2000);
        _logger.LogInformation("Confirmation email sent.");

        // --- End of the slow work ---

        return CreatedAtAction(nameof(CreateOrderSlow), new { id = order.Id }, order);
    }
    

    [HttpPost("slow")]
    public async Task<IActionResult> CreateOrderSlow([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Starting SLOW order creation for {Customer}", request.CustomerName);
        
        // --- Start of the slow and problematic work ---

        // 1. Simulate calling a slow payment gateway API (e.g., 3 seconds)
        _logger.LogInformation("Processing payment...");
        await Task.Delay(3000); 
        _logger.LogInformation("Payment processed.");

        // 2. Simulate updating inventory (DB work)
        _logger.LogInformation("Updating inventory...");
        await Task.Delay(500);
        _logger.LogInformation("Inventory updated.");

        // 3. Save the order to our database
        var order = new Order
        {
            CustomerName = request.CustomerName,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order saved to database with ID {OrderId}", order.Id);

        // 4. Simulate calling a slow email service API (e.g., 2 seconds)
        _logger.LogInformation("Sending confirmation email...");
        await Task.Delay(2000);
        _logger.LogInformation("Confirmation email sent.");

        // --- End of the slow work ---

        return CreatedAtAction(nameof(CreateOrderSlow), new { id = order.Id }, order);
    }
}