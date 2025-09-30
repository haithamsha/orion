using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Make sure this is included
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Add this using statement

namespace Orion.Api.Controllers;

public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

// New DTO for the status response
public record OrderStatusResponse(int OrderId, string Status, DateTime CreatedAt);

// Define the event payload we will publish
public record OrderPlacedEvent(int OrderId, string UserId, string CustomerName, decimal TotalAmount);


[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- THIS PROTECTS THE ENTIRE CONTROLLER
public class OrdersController : ControllerBase
{
    private readonly OrionDbContext _dbContext;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrionDbContext dbContext,
        IMessagePublisher messagePublisher, ILogger<OrdersController> logger) // INJECTED
    {
        _dbContext = dbContext;
        _messagePublisher = messagePublisher;
        _logger = logger;
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

         // NEW: Get the user ID from the token's 'sub' (subject) claim
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // This should not happen if the [Authorize] attribute is working correctly
            return Unauthorized();
        }

        // 1. Create the order with the new UserId.
        var order = new Order
        {
            UserId = userId, // <-- SAVE THE USER ID
            CustomerName = request.CustomerName,
            TotalAmount = request.TotalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // 2. Create the event payload with the UserId
        var orderEvent = new OrderPlacedEvent(order.Id, order.UserId, order.CustomerName, order.TotalAmount);

        // 3. Publish the event to the message broker.
        _messagePublisher.Publish(orderEvent);
        
        // 4. Return response to the user.
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