using SendGrid;
using SendGrid.Helpers.Mail;
using Orion.Api.Models.DTOs;
using Orion.Api.Data;
using Orion.Api.Models;

namespace Orion.Api.Services;

public class ProductionEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<ProductionEmailService> _logger;
    private readonly OrionDbContext _dbContext;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public ProductionEmailService(
        IConfiguration configuration,
        ILogger<ProductionEmailService> logger,
        OrionDbContext dbContext)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        _sendGridClient = new SendGridClient(apiKey);
        _logger = logger;
        _dbContext = dbContext;
        _fromEmail = configuration["Email:FromEmail"] ?? "noreply@orion.com";
        _fromName = configuration["Email:FromName"] ?? "Orion Team";
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendEmailWithTemplateAsync(
            toEmail,
            customerName,
            "d-12345678", // SendGrid template ID
            new
            {
                order_id = orderData.OrderId,
                customer_name = customerName,
                total_amount = orderData.TotalAmount,
                order_date = orderData.OrderDate.ToString("MMMM dd, yyyy"),
                items = orderData.Items.Select(i => new
                {
                    product_name = i.ProductName,
                    quantity = i.Quantity,
                    unit_price = i.UnitPrice,
                    total_price = i.TotalPrice
                }).ToArray()
            }
        );
    }

    public async Task<bool> SendOrderProcessingEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendEmailWithTemplateAsync(
            toEmail,
            customerName,
            "d-87654321", // SendGrid template ID for processing
            new
            {
                order_id = orderData.OrderId,
                customer_name = customerName,
                total_amount = orderData.TotalAmount
            }
        );
    }

    public async Task<bool> SendOrderCompletedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendEmailWithTemplateAsync(
            toEmail,
            customerName,
            "d-11223344", // SendGrid template ID for completion
            new
            {
                order_id = orderData.OrderId,
                customer_name = customerName,
                total_amount = orderData.TotalAmount,
                tracking_url = $"https://orion.com/orders/{orderData.OrderId}/track"
            }
        );
    }

    public async Task<bool> SendOrderFailedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendEmailWithTemplateAsync(
            toEmail,
            customerName,
            "d-55667788", // SendGrid template ID for failure
            new
            {
                order_id = orderData.OrderId,
                customer_name = customerName,
                total_amount = orderData.TotalAmount,
                support_url = "https://orion.com/support"
            }
        );
    }

    public async Task<bool> SendInventoryLowAlertAsync(string toEmail, string productName, int currentStock)
    {
        return await SendEmailWithTemplateAsync(
            toEmail,
            "Inventory Manager",
            "d-99887766", // SendGrid template ID for inventory alerts
            new
            {
                product_name = productName,
                current_stock = currentStock,
                reorder_url = $"https://admin.orion.com/inventory/{productName}/reorder"
            }
        );
    }

    public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
    {
        // Implementation for generic email sending
        var templateId = GetTemplateId(emailRequest.EmailType);
        return await SendEmailWithTemplateAsync(
            emailRequest.ToEmail,
            emailRequest.ToName,
            templateId,
            emailRequest.TemplateData
        );
    }

    // CORE SENDGRID IMPLEMENTATION
    private async Task<bool> SendEmailWithTemplateAsync(
        string toEmail, 
        string toName, 
        string templateId, 
        object templateData)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail, toName);

            var msg = MailHelper.CreateSingleTemplateEmail(
                from, 
                to, 
                templateId, 
                templateData
            );

            // Add tracking and analytics
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);
            msg.SetSubscriptionTracking(true);

            // Add custom headers for tracking
            msg.AddCustomArg("environment", "production");
            msg.AddCustomArg("service", "orion-api");
            msg.AddCustomArg("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Email sent successfully via SendGrid to {Email}. MessageId: {MessageId}", 
                    toEmail, response.Headers.GetValues("X-Message-Id").FirstOrDefault());
                
                await LogEmailDeliveryAsync(toEmail, templateId, true, null);
                return true;
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("❌ SendGrid email failed to {Email}. Status: {Status}, Error: {Error}", 
                    toEmail, response.StatusCode, errorBody);
                
                await LogEmailDeliveryAsync(toEmail, templateId, false, errorBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending email via SendGrid to {Email}", toEmail);
            await LogEmailDeliveryAsync(toEmail, templateId, false, ex.Message);
            return false;
        }
    }

    private string GetTemplateId(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.OrderConfirmation => "d-12345678",
            EmailType.OrderProcessing => "d-87654321", 
            EmailType.OrderCompleted => "d-11223344",
            EmailType.OrderFailed => "d-55667788",
            EmailType.InventoryLow => "d-99887766",
            _ => throw new ArgumentException($"No template configured for {emailType}")
        };
    }

    private async Task LogEmailDeliveryAsync(string toEmail, string templateId, bool success, string? errorMessage)
    {
        try
        {
            var log = new EmailDeliveryLog
            {
                ToEmail = toEmail,
                TemplateId = templateId,
                Provider = "SendGrid",
                Success = success,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.EmailDeliveryLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email delivery");
        }
    }
}