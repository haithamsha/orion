using System.Net;
using System.Net.Mail;
using Orion.Api.Models.DTOs;

namespace Orion.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configure SMTP client
        _smtpClient = new SmtpClient()
        {
            Host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com",
            Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
            EnableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true"),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _configuration["Email:Username"] ?? "test@orion.com",
                _configuration["Email:Password"] ?? "test-password"
            )
        };
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        _logger.LogInformation("Sending order confirmation email to {Email} for Order {OrderId}", toEmail, orderData.OrderId);

        var emailRequest = new EmailRequest(
            toEmail,
            customerName,
            EmailType.OrderConfirmation,
            new Dictionary<string, object> { ["orderData"] = orderData }
        );

        return await SendEmailAsync(emailRequest);
    }

    public async Task<bool> SendOrderProcessingEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        _logger.LogInformation("Sending order processing email to {Email} for Order {OrderId}", toEmail, orderData.OrderId);

        var emailRequest = new EmailRequest(
            toEmail,
            customerName,
            EmailType.OrderProcessing,
            new Dictionary<string, object> { ["orderData"] = orderData }
        );

        return await SendEmailAsync(emailRequest);
    }

    public async Task<bool> SendOrderCompletedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        _logger.LogInformation("Sending order completed email to {Email} for Order {OrderId}", toEmail, orderData.OrderId);

        var emailRequest = new EmailRequest(
            toEmail,
            customerName,
            EmailType.OrderCompleted,
            new Dictionary<string, object> { ["orderData"] = orderData }
        );

        return await SendEmailAsync(emailRequest);
    }

    public async Task<bool> SendOrderFailedEmailAsync(string toEmail, string customerName, OrderEmailData orderData)
    {
        _logger.LogInformation("Sending order failed email to {Email} for Order {OrderId}", toEmail, orderData.OrderId);

        var emailRequest = new EmailRequest(
            toEmail,
            customerName,
            EmailType.OrderFailed,
            new Dictionary<string, object> { ["orderData"] = orderData }
        );

        return await SendEmailAsync(emailRequest);
    }

    public async Task<bool> SendInventoryLowAlertAsync(string toEmail, string productName, int currentStock)
    {
        _logger.LogInformation("Sending inventory low alert email to {Email} for product {ProductName}", toEmail, productName);

        var subject = $"🚨 Low Inventory Alert - {productName}";
        var body = $@"
        <div style='font-family: Arial, sans-serif; padding: 20px;'>
            <h2 style='color: #f44336;'>⚠️ Low Inventory Alert</h2>
            <p><strong>Product:</strong> {productName}</p>
            <p><strong>Current Stock:</strong> {currentStock} units</p>
            <p style='color: #f44336;'><strong>Action Required:</strong> Restock this item soon!</p>
        </div>";

        return await SendRawEmailAsync(toEmail, "Inventory Manager", subject, body);
    }

    public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
    {
        try
        {
            var (subject, htmlBody) = GenerateEmailContent(emailRequest);
            return await SendRawEmailAsync(emailRequest.ToEmail, emailRequest.ToName, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} of type {EmailType}", 
                emailRequest.ToEmail, emailRequest.EmailType);
            return false;
        }
    }

    private async Task<bool> SendRawEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@orion.com";
            var fromName = _configuration["Email:FromName"] ?? "Orion Team";

            var mailMessage = new MailMessage()
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(toEmail, toName));

            // For development, we'll just log the email instead of actually sending
            if (_configuration["Environment"] == "Development")
            {
                _logger.LogInformation("📧 [DEVELOPMENT] Email would be sent:");
                _logger.LogInformation("To: {ToEmail} ({ToName})", toEmail, toName);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body Length: {BodyLength} characters", htmlBody.Length);
                
                // Simulate email sending delay
                await Task.Delay(500);
                return true;
            }
            else
            {
                // In production, actually send the email
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    private (string Subject, string HtmlBody) GenerateEmailContent(EmailRequest emailRequest)
    {
        var orderData = emailRequest.TemplateData["orderData"] as OrderEmailData;

        return emailRequest.EmailType switch
        {
            EmailType.OrderConfirmation => (
                $"🎉 Order Confirmation #{orderData!.OrderId} - Orion",
                EmailTemplates.GetOrderConfirmationTemplate(orderData)
            ),
            EmailType.OrderProcessing => (
                $"🔄 Order Processing #{orderData!.OrderId} - Orion",
                EmailTemplates.GetOrderProcessingTemplate(orderData)
            ),
            EmailType.OrderCompleted => (
                $"✅ Order Completed #{orderData!.OrderId} - Orion",
                EmailTemplates.GetOrderCompletedTemplate(orderData)
            ),
            EmailType.OrderFailed => (
                $"❌ Order Issue #{orderData!.OrderId} - Orion",
                EmailTemplates.GetOrderFailedTemplate(orderData)
            ),
            _ => throw new ArgumentException($"Unsupported email type: {emailRequest.EmailType}")
        };
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}