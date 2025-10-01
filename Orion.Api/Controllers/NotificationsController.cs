using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orion.Api.Auth;
using Orion.Api.Hubs;

namespace Orion.Api.Controllers;

public record OrderStatusUpdateRequest(int OrderId, string UserId, string Status);

[ApiController]
[Route("api/[controller]")]
[ApiKeyAuthFilter] // <-- PROTECT THIS CONTROLLER WITH OUR API KEY
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<OrderStatusHub> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IHubContext<OrderStatusHub> hubContext, ILogger<NotificationsController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("order-status")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusUpdateRequest request)
    {
        // Log the incoming notification
        _logger.LogInformation("Received order status notification: Order {OrderId}, User {UserId}, Status {Status}", 
            request.OrderId, request.UserId, request.Status);

        // Push a message to the specific user associated with the order
        await _hubContext.Clients.User(request.UserId).SendAsync(
            "OrderStatusUpdated",
            new { orderId = request.OrderId, status = request.Status }
        );

        // Log the SignalR notification sent
        _logger.LogInformation("Sent SignalR notification to user {UserId} for Order {OrderId} with status {Status}", 
            request.UserId, request.OrderId, request.Status);

        return Ok();
    }
}