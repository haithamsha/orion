using Microsoft.AspNetCore.Mvc;
using Orion.Api.Models.DTOs;
using Orion.Api.Services;

namespace Orion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send-order-status")]
    public async Task<IActionResult> SendOrderStatusEmail([FromBody] OrderStatusEmailRequest request)
    {
        // Validate API key
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (apiKey != "orion-super-secret-api-key-2024")
        {
            return Unauthorized("Invalid API key");
        }

        try
        {
            var orderEmailData = new OrderEmailData(
                request.OrderId,
                request.CustomerName,
                request.TotalAmount,
                request.OrderDate,
                request.Items,
                request.Status
            );

            bool emailSent = request.EmailType switch
            {
                "Processing" => await _emailService.SendOrderProcessingEmailAsync(
                    request.CustomerEmail, request.CustomerName, orderEmailData),
                "Completed" => await _emailService.SendOrderCompletedEmailAsync(
                    request.CustomerEmail, request.CustomerName, orderEmailData),
                "Failed" => await _emailService.SendOrderFailedEmailAsync(
                    request.CustomerEmail, request.CustomerName, orderEmailData),
                _ => false
            };

            if (emailSent)
            {
                _logger.LogInformation("✅ {EmailType} email sent for Order {OrderId}", request.EmailType, request.OrderId);
                return Ok(new { success = true, message = "Email sent successfully" });
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to send {EmailType} email for Order {OrderId}", request.EmailType, request.OrderId);
                return BadRequest(new { success = false, message = "Failed to send email" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending {EmailType} email for Order {OrderId}", request.EmailType, request.OrderId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

public record OrderStatusEmailRequest(
    int OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime OrderDate,
    List<OrderItemResponse> Items,
    string Status,
    string EmailType
);