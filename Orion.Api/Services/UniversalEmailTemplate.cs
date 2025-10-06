using Orion.Api.Models.DTOs;

namespace Orion.Api.Services;

public static class UniversalEmailTemplate
{
    public static string GetResponsiveTemplate(
        string emailType,
        string customerName,
        string mainTitle,
        string mainMessage,
        Dictionary<string, object> templateData)
    {
        var styling = GetEmailStyling(emailType);
        
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='x-apple-disable-message-reformatting'>
    <title>{mainTitle}</title>
    <style>
        @media only screen and (max-width: 600px) {{
            .container {{ width: 100% !important; }}
            .content {{ padding: 20px !important; }}
            .header {{ padding: 20px !important; }}
            .order-table {{ font-size: 14px !important; }}
            .order-table th, .order-table td {{ padding: 8px 4px !important; }}
            .mobile-hide {{ display: none !important; }}
            .mobile-center {{ text-align: center !important; }}
        }}
    </style>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4;'>
    
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background-color: #f4f4f4;'>
        <tr>
            <td style='padding: 20px 0;'>
                
                <table class='container' role='presentation' cellspacing='0' cellpadding='0' border='0' width='600' style='margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                    
                    <!-- Header -->
                    <tr>
                        <td class='header' style='background: linear-gradient(135deg, {styling.primaryColor} 0%, {styling.secondaryColor} 100%); color: white; padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;'>
                            <h1 style='margin: 0; font-size: 32px; font-weight: 300;'>
                                {styling.icon} Orion
                            </h1>
                            <h2 style='margin: 15px 0 0 0; font-size: 20px; font-weight: normal; opacity: 0.9;'>
                                {mainTitle}
                            </h2>
                        </td>
                    </tr>
                    
                    <!-- Main Content -->
                    <tr>
                        <td class='content' style='padding: 40px 30px;'>
                            
                            <!-- Greeting -->
                            <h3 style='color: {styling.primaryColor}; margin: 0 0 20px 0; font-size: 18px;'>
                                Hi {customerName}! 👋
                            </h3>
                            
                            <!-- Main Message -->
                            <p style='font-size: 16px; margin: 0 0 25px 0; color: #555;'>
                                {mainMessage}
                            </p>
                            
                            {GenerateOrderDetailsSection(templateData)}
                            
                            {GenerateActionSection(emailType, templateData)}
                            
                            {GenerateNextStepsSection(emailType)}
                            
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px; border-top: 1px solid #e9ecef;'>
                            
                            <p style='margin: 0 0 15px 0; color: #666; font-size: 14px;'>
                                Need help? Contact us at 
                                <a href='mailto:support@orion.com' style='color: {styling.primaryColor}; text-decoration: none;'>support@orion.com</a>
                            </p>
                            
                            <p style='margin: 0; color: #888; font-size: 12px; line-height: 1.4;'>
                                © 2025 Orion E-Commerce. All rights reserved.<br>
                                123 Commerce Street, Tech City, TC 12345<br>
                                <a href='https://orion.com/unsubscribe' style='color: #888; text-decoration: underline;'>Unsubscribe</a> | 
                                <a href='https://orion.com/privacy' style='color: #888; text-decoration: underline;'>Privacy Policy</a>
                            </p>
                            
                        </td>
                    </tr>
                    
                </table>
                
            </td>
        </tr>
    </table>
    
</body>
</html>";
    }

    private static (string primaryColor, string secondaryColor, string icon) GetEmailStyling(string emailType)
    {
        return emailType.ToLower() switch
        {
            "confirmation" => ("#667eea", "#764ba2", "🎉"),
            "processing" => ("#f093fb", "#f5576c", "🔄"),
            "completed" => ("#4caf50", "#45a049", "✅"),
            "failed" => ("#f44336", "#d32f2f", "❌"),
            "inventory" => ("#ff9800", "#f57c00", "🚨"),
            _ => ("#667eea", "#764ba2", "🚀")
        };
    }

    private static string GenerateOrderDetailsSection(Dictionary<string, object> templateData)
    {
        if (!templateData.ContainsKey("orderData"))
            return "";

        var orderData = templateData["orderData"] as OrderEmailData;
        if (orderData == null)
            return "";

        var itemsHtml = string.Join("", orderData.Items.Select(item => 
            $@"
            <tr>
                <td style='padding: 12px 8px; border-bottom: 1px solid #eee; color: #333;'>
                    <strong>{item.ProductName}</strong><br>
                    <small style='color: #666;'>SKU: {item.ProductSku}</small>
                </td>
                <td style='padding: 12px 8px; border-bottom: 1px solid #eee; text-align: center; color: #333;'>
                    {item.Quantity}
                </td>
                <td style='padding: 12px 8px; border-bottom: 1px solid #eee; text-align: right; color: #333;'>
                    ${item.UnitPrice:F2}
                </td>
                <td style='padding: 12px 8px; border-bottom: 1px solid #eee; text-align: right; font-weight: 600; color: #333;'>
                    ${item.TotalPrice:F2}
                </td>
            </tr>"
        ));

        return $@"
        <div style='background: white; border: 1px solid #e9ecef; border-radius: 8px; margin: 25px 0; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
            
            <div style='background: #f8f9fa; padding: 20px; border-bottom: 1px solid #e9ecef;'>
                <h4 style='margin: 0 0 10px 0; color: #495057; font-size: 18px;'>📦 Order Details</h4>
                <div>
                    <div style='display: inline-block; margin-right: 20px;'>
                        <strong style='color: #6c757d; font-size: 12px;'>Order ID</strong><br>
                        <span style='font-size: 16px; font-weight: 600; color: #495057;'>#{orderData.OrderId}</span>
                    </div>
                    <div style='display: inline-block; margin-right: 20px;'>
                        <strong style='color: #6c757d; font-size: 12px;'>Date</strong><br>
                        <span style='font-size: 16px; color: #495057;'>{orderData.OrderDate:MMM dd, yyyy}</span>
                    </div>
                    <div style='display: inline-block;'>
                        <strong style='color: #6c757d; font-size: 12px;'>Status</strong><br>
                        <span style='background: #e8f5e8; color: #2d5016; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600;'>{orderData.Status}</span>
                    </div>
                </div>
            </div>
            
            <table class='order-table' role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='border-collapse: collapse;'>
                <thead>
                    <tr style='background: #495057; color: white;'>
                        <th style='padding: 15px 8px; text-align: left; font-weight: 600; font-size: 14px;'>Product</th>
                        <th style='padding: 15px 8px; text-align: center; font-weight: 600; font-size: 14px;'>Qty</th>
                        <th style='padding: 15px 8px; text-align: right; font-weight: 600; font-size: 14px;'>Price</th>
                        <th style='padding: 15px 8px; text-align: right; font-weight: 600; font-size: 14px;'>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                    <tr style='background: #f8f9fa;'>
                        <td colspan='3' style='padding: 20px 8px; text-align: right; font-weight: 600; font-size: 16px; color: #495057;'>
                            Total Amount:
                        </td>
                        <td style='padding: 20px 8px; text-align: right; font-weight: 700; font-size: 18px; color: #28a745;'>
                            ${orderData.TotalAmount:F2}
                        </td>
                    </tr>
                </tbody>
            </table>
            
        </div>";
    }

    private static string GenerateActionSection(string emailType, Dictionary<string, object> templateData)
    {
        var orderData = templateData.GetValueOrDefault("orderData") as OrderEmailData;
        
        return emailType.ToLower() switch
        {
            "confirmation" => $@"
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://orion.com/orders/{orderData?.OrderId}' 
                   style='background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; font-weight: 600; display: inline-block;'>
                    View Order Details
                </a>
            </div>",
            
            "completed" => $@"
            <div style='text-align: center; margin: 30px 0;'>
                <div style='background: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                    <strong>🎉 Your order is complete and ready for shipment!</strong>
                </div>
                <a href='https://orion.com/orders/{orderData?.OrderId}/track' 
                   style='background: #28a745; color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; font-weight: 600; display: inline-block;'>
                    Track Your Order
                </a>
            </div>",
            
            "failed" => $@"
            <div style='text-align: center; margin: 30px 0;'>
                <div style='background: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                    <strong>We're sorry!</strong> There was an issue processing your order, but no charges were made.
                </div>
                <a href='https://orion.com/checkout/retry/{orderData?.OrderId}' 
                   style='background: #dc3545; color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; font-weight: 600; display: inline-block;'>
                    Try Again
                </a>
            </div>",
            
            _ => ""
        };
    }

    private static string GenerateNextStepsSection(string emailType)
    {
        var steps = emailType.ToLower() switch
        {
            "confirmation" => new[]
            {
                "We're processing your payment securely",
                "Your items are being prepared for shipment", 
                "You'll receive updates via email and notifications",
                "Expected processing time: 1-2 business days"
            },
            "processing" => new[]
            {
                "✅ Payment verification in progress",
                "🔄 Inventory allocation",
                "📦 Packaging preparation",
                "🚚 Shipping arrangement"
            },
            "completed" => new[]
            {
                "Your items are being packaged",
                "Shipping label has been created",
                "You'll receive tracking information soon",
                "Expected delivery: 3-5 business days"
            },
            "failed" => new[]
            {
                "Payment processing encountered an issue",
                "Your inventory has been restored",
                "No charges were applied to your account",
                "You can try placing the order again"
            },
            _ => Array.Empty<string>()
        };

        if (steps.Length == 0)
            return "";

        var stepsHtml = string.Join("", steps.Select(step => 
            $"<li style='margin: 8px 0; color: #555;'>{step}</li>"));

        return $@"
        <div style='background: #f8f9fa; border-left: 4px solid #667eea; padding: 20px; margin: 25px 0; border-radius: 0 8px 8px 0;'>
            <h4 style='margin: 0 0 15px 0; color: #495057; font-size: 16px;'>⏱️ What's Next?</h4>
            <ul style='margin: 0; padding-left: 20px; line-height: 1.6;'>
                {stepsHtml}
            </ul>
        </div>";
    }
}