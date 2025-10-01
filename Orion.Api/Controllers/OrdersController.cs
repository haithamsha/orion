using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Models.DTOs;
using Orion.Api.Services;
using System.Security.Claims;

namespace Orion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class OrdersController : ControllerBase
{
    private readonly OrionDbContext _dbContext;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IInventoryService _inventoryService;
    private readonly IEmailService _emailService; // ADD THIS LINE
    private readonly ILogger<OrdersController> _logger;


    public OrdersController(
        OrionDbContext dbContext,
        IMessagePublisher messagePublisher,
        IInventoryService inventoryService,
        IEmailService emailService, // ADD THIS LINE
        ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _messagePublisher = messagePublisher;
        _inventoryService = inventoryService;
        _emailService = emailService; // ADD THIS LINE
        _logger = logger;
    }

    [HttpPost("fast")]
    public async Task<ActionResult<OrderResponse>> CreateOrderFast([FromBody] CreateOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("Creating order for user {UserId} with {ItemCount} items", userId, request.Items.Count);

        try
        {
            // STEP 1: Validate inventory availability
            var inventoryCheck = await ValidateInventoryAsync(request.Items);
            if (!inventoryCheck.IsValid)
            {
                return BadRequest(inventoryCheck.ErrorMessage);
            }

            // STEP 2: Reserve inventory
            var reservationRequests = request.Items.Select(item => 
                new InventoryReservationRequest(item.ProductSku, item.Quantity)).ToList();
            
            var reservationResult = await _inventoryService.ReserveInventoryAsync(reservationRequests);
            if (!reservationResult.Success)
            {
                return BadRequest($"Inventory reservation failed: {reservationResult.Message}");
            }

            // STEP 3: Create order with reserved inventory
            var order = await CreateOrderWithItemsAsync(userId, request, inventoryCheck.InventoryItems);

            // STEP 4: Update order status to InventoryReserved
            order.Status = OrderStatus.InventoryReserved;
            await _dbContext.SaveChangesAsync();

            // STEP 5: Send order confirmation email
            await SendOrderConfirmationEmailAsync(order, request.CustomerName);

            // STEP 6: Publish enhanced event for background processing
            var orderEvent = new OrderPlacedEvent(
                order.Id,
                userId,
                request.CustomerName,
                order.TotalAmount,
                order.OrderItems.Select(oi => new OrderItemData(
                    oi.ProductSku,
                    oi.ProductName,
                    oi.Quantity,
                    oi.UnitPrice
                )).ToList()
            );

            await _messagePublisher.PublishOrderPlacedAsync(orderEvent);

            _logger.LogInformation("Order {OrderId} created successfully with inventory reserved and confirmation email sent", order.Id);


            // STEP 7: Return order response
            return Ok(MapToOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for user {UserId}", userId);
            return StatusCode(500, "An error occurred while creating the order");
        }
    }

      // ADD THIS NEW METHOD:
    private async Task SendOrderConfirmationEmailAsync(Order order, string customerName)
    {
        try
        {
            // For demo purposes, we'll use a demo email
            // In production, you'd get the user's actual email from the database or JWT claims
            var customerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "customer@demo.com";

            var orderEmailData = new OrderEmailData(
                order.Id,
                customerName,
                order.TotalAmount,
                order.CreatedAt,
                order.OrderItems.Select(oi => new OrderItemResponse(
                    oi.ProductName,
                    oi.ProductSku,
                    oi.UnitPrice,
                    oi.Quantity,
                    oi.TotalPrice
                )).ToList(),
                order.Status.ToString()
            );

            var emailSent = await _emailService.SendOrderConfirmationEmailAsync(
                customerEmail, 
                customerName, 
                orderEmailData
            );

            if (emailSent)
            {
                _logger.LogInformation("✅ Order confirmation email sent successfully for Order {OrderId}", order.Id);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to send order confirmation email for Order {OrderId}", order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending order confirmation email for Order {OrderId}", order.Id);
            // Don't fail the order creation if email fails
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
        {
            return NotFound($"Order {id} not found");
        }

        return Ok(MapToOrderResponse(order));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<List<Inventory>>> GetAvailableProducts()
    {
        var products = await _inventoryService.GetAvailableProductsAsync();
        return Ok(products);
    }

    // PRIVATE HELPER METHODS

    private async Task<(bool IsValid, string? ErrorMessage, List<Inventory> InventoryItems)> ValidateInventoryAsync(List<OrderItemRequest> items)
    {
        var inventoryItems = new List<Inventory>();

        foreach (var item in items)
        {
            var product = await _inventoryService.GetProductBySkuAsync(item.ProductSku);
            if (product == null)
            {
                return (false, $"Product with SKU '{item.ProductSku}' not found", inventoryItems);
            }

            if (!await _inventoryService.IsProductAvailableAsync(item.ProductSku, item.Quantity))
            {
                return (false, $"Insufficient stock for product '{item.ProductSku}'. Available: {product.AvailableQuantity}, Requested: {item.Quantity}", inventoryItems);
            }

            inventoryItems.Add(product);
        }

        return (true, null, inventoryItems);
    }

    private async Task<Order> CreateOrderWithItemsAsync(string userId, CreateOrderRequest request, List<Inventory> inventoryItems)
    {
        var order = new Order
        {
            UserId = userId,
            CustomerName = request.CustomerName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        decimal totalAmount = 0;

        for (int i = 0; i < request.Items.Count; i++)
        {
            var requestItem = request.Items[i];
            var inventory = inventoryItems[i];

            var orderItem = new OrderItem
            {
                Order = order,
                InventoryId = inventory.Id,
                Inventory = inventory,
                ProductName = inventory.ProductName,
                ProductSku = inventory.ProductSku,
                UnitPrice = inventory.Price,
                Quantity = requestItem.Quantity
            };

            order.OrderItems.Add(orderItem);
            totalAmount += orderItem.TotalPrice;
        }

        order.TotalAmount = totalAmount;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        return order;
    }

    private static OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.CustomerName,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.OrderItems.Select(oi => new OrderItemResponse(
                oi.ProductName,
                oi.ProductSku,
                oi.UnitPrice,
                oi.Quantity,
                oi.TotalPrice
            )).ToList()
        );
    }
}