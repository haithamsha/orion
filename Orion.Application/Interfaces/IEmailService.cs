using Orion.Application.DTOs;

namespace Orion.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string customerName, OrderEmailData orderData);
    Task<bool> SendOrderProcessingEmailAsync(string toEmail, string customerName, OrderEmailData orderData);
    Task<bool> SendOrderCompletedEmailAsync(string toEmail, string customerName, OrderEmailData orderData);
    Task<bool> SendOrderFailedEmailAsync(string toEmail, string customerName, OrderEmailData orderData);
    Task<bool> SendInventoryLowAlertAsync(string toEmail, string productName, int currentStock);
    Task<bool> SendEmailAsync(EmailRequest emailRequest);
}