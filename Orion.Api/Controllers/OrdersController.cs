using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orion.Api.Models;
using Orion.Api.Models.DTOs;
using Orion.Api.Services.CQRS.Commands;
using Orion.Api.Services.CQRS.Queries;
using MediatR;
using System.Security.Claims;

namespace Orion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IMediator mediator,
        ILogger<OrdersController> logger)
    {
        _mediator = mediator;
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
            var command = new CreateOrderCommand(userId, request.CustomerName, request.Items);
            var result = await _mediator.Send(command);
            
            _logger.LogInformation("Order created successfully for user {UserId}", userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Order creation failed for user {UserId}: {Error}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for user {UserId}", userId);
            return StatusCode(500, "An error occurred while creating the order");
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(int orderId)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        try
        {
            var query = new GetOrderByIdQuery(orderId);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound($"Order {orderId} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order {OrderId}", orderId);
            return StatusCode(500, "An error occurred while retrieving the order");
        }
    }

    [HttpGet("my-orders")]
    public async Task<ActionResult<List<OrderResponse>>> GetMyOrders([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("Getting orders for user {UserId} (Skip: {Skip}, Take: {Take})", userId, skip, take);

        try
        {
            var query = new GetOrdersByUserQuery(userId, skip, take);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    [HttpPatch("{orderId}/status")]
    public async Task<ActionResult> ChangeOrderStatus(int orderId, [FromBody] ChangeOrderStatusRequest request)
    {
        _logger.LogInformation("Changing status for order {OrderId} to {Status}", orderId, request.NewStatus);

        try
        {
            var command = new ChangeOrderStatusCommand(orderId, request.NewStatus, request.Reason);
            await _mediator.Send(command);

            _logger.LogInformation("Order {OrderId} status changed to {Status}", orderId, request.NewStatus);
            return Ok(new { message = "Order status updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to change order {OrderId} status: {Error}", orderId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change order {OrderId} status", orderId);
            return StatusCode(500, "An error occurred while updating the order status");
        }
    }
}