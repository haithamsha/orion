using SendGrid;
using SendGrid.Helpers.Mail;
using Orion.Api.Models.DTOs;

namespace Orion.Api.Services;

public class SimpleEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<SimpleEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SimpleEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SimpleEmailService> logger)
    {
        var apiKey = configuration["SendGrid:ApiKey"] ?? "DEMO_API_KEY";
        _sendGridClient = new SendGridClient(httpClient, apiKey);
        _logger = logger;
        _fromEmail = configuration["Email:FromEmail"] ?? "orders@orion.com";
        _fromName = configuration["Email:FromName"] ?? "Orion Team";
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendUniversalEmailAsync(
            toEmail,
            customerName,
            "Order Confirmation",
            "🎉 Thank you for your order! We've received it and are getting everything ready for you.",
            "confirmation",
            new Dictionary<string, object> { ["orderData"] = orderData }
        );
    }

    public async Task<bool> SendOrderProcessingEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendUniversalEmailAsync(
            toEmail,
            customerName,
            "Order Processing",
            "🔄 Great news! We're now processing your order and preparing your items for shipment.",
            "processing",
            new Dictionary<string, object> { ["orderData"] = orderData }
        );
    }

    public async Task<bool> SendOrderCompletedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendUniversalEmailAsync(
            toEmail,
            customerName,
            "Order Completed! 🎉",
            "✅ Congratulations! Your order has been completed successfully and is ready for shipment!",
            "completed",
            new Dictionary<string, object> { ["orderData"] = orderData }
        );
    }

    public async Task<bool> SendOrderFailedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        return await SendUniversalEmailAsync(
            toEmail,
            customerName,
            "Order Issue",
            "❌ We encountered an issue processing your order. Don't worry - no charges were made and your items have been returned to inventory.",
            "failed",
            new Dictionary<string, object> { ["orderData"] = orderData }
        );
    }

    public async Task<bool> SendInventoryLowAlertAsync(string toEmail, string productName, int currentStock)
    {
        return await SendUniversalEmailAsync(
            toEmail,
            "Inventory Manager",
            "Low Inventory Alert",
            $"🚨 Low stock alert: {productName} is running low with only {currentStock} units remaining.",
            "inventory",
            new Dictionary<string, object> 
            { 
                ["productName"] = productName,
                ["currentStock"] = currentStock 
            }
        );
    }

    public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
    {
        return await SendUniversalEmailAsync(
            emailRequest.ToEmail,
            emailRequest.ToName,
            "Notification",
            "You have a new notification from Orion.",
            emailRequest.EmailType.ToString().ToLower(),
            emailRequest.TemplateData
        );
    }

    private async Task<bool> SendUniversalEmailAsync(
        string toEmail,
        string toName,
        string title,
        string message,
        string emailType,
        Dictionary<string, object> templateData)
    {
        try
        {
            // For development, just log the email
            if (_sendGridClient.ToString().Contains("DEMO"))
            {
                _logger.LogInformation("📧 [DEVELOPMENT] Email would be sent:");
                _logger.LogInformation("To: {ToEmail} ({ToName})", toEmail, toName);
                _logger.LogInformation("Subject: {Title}", title);
                _logger.LogInformation("Type: {EmailType}", emailType);
                await Task.Delay(200); // Simulate processing
                return true;
            }

            var htmlContent = UniversalEmailTemplate.GetResponsiveTemplate(
                emailType, toName, title, message, templateData);

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail, toName);
            var subject = $"{title} - Orion";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

            // Add tracking
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
                return true;
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("❌ Email failed to {Email}. Status: {Status}, Error: {Error}", 
                    toEmail, response.StatusCode, errorBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending email to {Email}", toEmail);
            return false;
        }
    }
}