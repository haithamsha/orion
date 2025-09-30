using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Make sure this is included
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Orion.Api.Controllers;

public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

// New DTO for the status response
public record OrderStatusResponse(int OrderId, string Status, DateTime CreatedAt);


[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- THIS PROTECTS THE ENTIRE CONTROLLER
public class OrdersController : ControllerBase
{
    private readonly OrionDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public OrdersController(
        OrionDbContext dbContext,
        ILogger<OrdersController> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _dbContext = dbContext;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    // --- NEW METHOD ---
    [HttpGet("{id:int}")]
    [AllowAnonymous] // <-- ALLOWS PUBLIC ACCESS TO THIS SPECIFIC ENDPOINT
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _dbContext.Orders
            .AsNoTracking() // Use AsNoTracking for read-only queries for better performance
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var response = new OrderStatusResponse(
            order.Id,
            order.Status.ToString(), // Convert enum to string for clean JSON
            order.CreatedAt
        );

        return Ok(response);
    }
    // --- END OF NEW METHOD ---


    [HttpPost("fast")]
    public async Task<IActionResult> CreateOrderFast([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Starting FAST order creation for {Customer}", request.CustomerName);

        var order = new Order
        {
            CustomerName = request.CustomerName,
            TotalAmount = request.TotalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order saved to DB with ID {OrderId}. Enqueuing background job.", order.Id);
        
        _backgroundJobClient.Enqueue<IOrderProcessingService>(service => service.ProcessOrder(order.Id));
        
        _logger.LogInformation("API request finished in milliseconds.");

        // We now return the URL to the new status endpoint in the 'Location' header
        return AcceptedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
    
    // --- The old slow method is still here for comparison ---
    [HttpPost("slow")]
    public async Task<IActionResult> CreateOrderSlow([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Starting SLOW order creation for {Customer}", request.CustomerName);
        await Task.Delay(3000); 
        _logger.LogInformation("Payment processed.");
        await Task.Delay(500);
        _logger.LogInformation("Inventory updated.");
        var order = new Order
        {
            CustomerName = request.CustomerName,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Completed
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order saved to database with ID {OrderId}", order.Id);
        await Task.Delay(2000);
        _logger.LogInformation("Confirmation email sent.");
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}