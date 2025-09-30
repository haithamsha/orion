using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

    public NotificationsController(IHubContext<OrderStatusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("order-status")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusUpdateRequest request)
    {
        // Push a message to the specific user associated with the order
        await _hubContext.Clients.User(request.UserId).SendAsync(
            "OrderStatusUpdated",
            new { orderId = request.OrderId, status = request.Status }
        );

        return Ok();
    }
}