using Orion.Api.Models.DTOs;

namespace Orion.Api.Services;

public static class EmailTemplates
{
    public static string GetOrderConfirmationTemplate(OrderEmailData orderData)
    {
        var itemsHtml = string.Join("", orderData.Items.Select(item => 
            $@"
            <tr>
                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${item.UnitPrice:F2}</td>
                <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>${item.TotalPrice:F2}</td>
            </tr>"
        ));

        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Order Confirmation - Orion</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                <h1 style='margin: 0; font-size: 28px;'>🚀 Orion</h1>
                <h2 style='margin: 10px 0 0 0; font-weight: normal;'>Order Confirmed!</h2>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;'>
                <h3 style='color: #2c5aa0; margin-top: 0;'>Hi {orderData.CustomerName}! 👋</h3>
                
                <p style='font-size: 16px; margin-bottom: 25px;'>
                    Great news! Your order has been confirmed and we're getting it ready for you.
                </p>
                
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;'>
                    <h4 style='margin: 0 0 15px 0; color: #2c5aa0;'>📦 Order Details</h4>
                    <p style='margin: 5px 0;'><strong>Order ID:</strong> #{orderData.OrderId}</p>
                    <p style='margin: 5px 0;'><strong>Order Date:</strong> {orderData.OrderDate:MMMM dd, yyyy 'at' HH:mm}</p>
                    <p style='margin: 5px 0;'><strong>Status:</strong> <span style='background: #e8f5e8; color: #2d5016; padding: 3px 8px; border-radius: 12px; font-size: 12px;'>{orderData.Status}</span></p>
                </div>

                <h4 style='color: #2c5aa0; margin: 25px 0 15px 0;'>🛒 Items Ordered</h4>
                <table style='width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <thead>
                        <tr style='background: #667eea; color: white;'>
                            <th style='padding: 15px 10px; text-align: left;'>Product</th>
                            <th style='padding: 15px 10px; text-align: center;'>Qty</th>
                            <th style='padding: 15px 10px; text-align: right;'>Price</th>
                            <th style='padding: 15px 10px; text-align: right;'>Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsHtml}
                        <tr style='background: #f8f9fa; font-weight: bold; font-size: 16px;'>
                            <td colspan='3' style='padding: 15px 10px; text-align: right;'>Total Amount:</td>
                            <td style='padding: 15px 10px; text-align: right; color: #2c5aa0;'>${orderData.TotalAmount:F2}</td>
                        </tr>
                    </tbody>
                </table>

                <div style='background: #e8f4fd; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #3498db;'>
                    <h4 style='margin: 0 0 10px 0; color: #2980b9;'>⏱️ What's Next?</h4>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>We're processing your payment securely</li>
                        <li>Your items are being prepared for shipment</li>
                        <li>You'll receive updates via email and real-time notifications</li>
                        <li>Expected processing time: 1-2 business days</li>
                    </ul>
                </div>

                <div style='text-align: center; margin: 30px 0;'>
                    <p style='color: #666; font-size: 14px;'>
                        Questions about your order? Contact us at 
                        <a href='mailto:support@orion.com' style='color: #667eea; text-decoration: none;'>support@orion.com</a>
                    </p>
                </div>

                <div style='text-align: center; padding: 20px 0; border-top: 1px solid #eee; margin-top: 30px;'>
                    <p style='color: #888; font-size: 12px; margin: 0;'>
                        Thank you for choosing Orion! 🚀<br>
                        This email was sent automatically. Please do not reply to this email.
                    </p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetOrderProcessingTemplate(OrderEmailData orderData)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                <h1 style='margin: 0; font-size: 28px;'>🚀 Orion</h1>
                <h2 style='margin: 10px 0 0 0; font-weight: normal;'>Order Processing</h2>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;'>
                <h3 style='color: #e91e63; margin-top: 0;'>Hi {orderData.CustomerName}! 👋</h3>
                
                <p style='font-size: 16px;'>
                    Great news! We're now processing your order and preparing your items for shipment.
                </p>
                
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f093fb;'>
                    <h4 style='margin: 0 0 15px 0; color: #e91e63;'>🔄 Order #{orderData.OrderId}</h4>
                    <p style='margin: 5px 0;'><strong>Status:</strong> <span style='background: #fff3e0; color: #e65100; padding: 3px 8px; border-radius: 12px; font-size: 12px;'>Processing</span></p>
                    <p style='margin: 5px 0;'><strong>Total:</strong> ${orderData.TotalAmount:F2}</p>
                </div>

                <div style='background: #fff8e1; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #ffc107;'>
                    <h4 style='margin: 0 0 10px 0; color: #f57c00;'>🏭 Processing Steps</h4>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>✅ Payment verification in progress</li>
                        <li>🔄 Inventory allocation</li>
                        <li>📦 Packaging preparation</li>
                        <li>🚚 Shipping arrangement</li>
                    </ul>
                </div>

                <p style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
                    We'll notify you as soon as your order ships!
                </p>
            </div>
        </body>
        </html>";
    }

    public static string GetOrderCompletedTemplate(OrderEmailData orderData)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #4caf50 0%, #45a049 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                <h1 style='margin: 0; font-size: 28px;'>🚀 Orion</h1>
                <h2 style='margin: 10px 0 0 0; font-weight: normal;'>Order Completed! 🎉</h2>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;'>
                <h3 style='color: #4caf50; margin-top: 0;'>Congratulations {orderData.CustomerName}! 🎉</h3>
                
                <p style='font-size: 16px;'>
                    Your order has been completed successfully and is ready for shipment!
                </p>
                
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50;'>
                    <h4 style='margin: 0 0 15px 0; color: #2e7d32;'>✅ Order #{orderData.OrderId}</h4>
                    <p style='margin: 5px 0;'><strong>Status:</strong> <span style='background: #e8f5e8; color: #2d5016; padding: 3px 8px; border-radius: 12px; font-size: 12px;'>Completed</span></p>
                    <p style='margin: 5px 0;'><strong>Total:</strong> ${orderData.TotalAmount:F2}</p>
                    <p style='margin: 5px 0;'><strong>Completed:</strong> {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm}</p>
                </div>

                <div style='background: #e8f5e8; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #4caf50;'>
                    <h4 style='margin: 0 0 10px 0; color: #2e7d32;'>📦 Next Steps</h4>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>Your items are being packaged</li>
                        <li>Shipping label has been created</li>
                        <li>You'll receive tracking information soon</li>
                        <li>Expected delivery: 3-5 business days</li>
                    </ul>
                </div>

                <div style='text-align: center; margin: 30px 0;'>
                    <div style='background: #4caf50; color: white; padding: 15px 30px; border-radius: 25px; display: inline-block;'>
                        <strong>🚚 Your order is on its way!</strong>
                    </div>
                </div>

                <p style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
                    Thank you for choosing Orion! We hope you love your purchase.
                </p>
            </div>
        </body>
        </html>";
    }

    public static string GetOrderFailedTemplate(OrderEmailData orderData)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #f44336 0%, #d32f2f 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                <h1 style='margin: 0; font-size: 28px;'>🚀 Orion</h1>
                <h2 style='margin: 10px 0 0 0; font-weight: normal;'>Order Issue</h2>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;'>
                <h3 style='color: #f44336; margin-top: 0;'>Hi {orderData.CustomerName},</h3>
                
                <p style='font-size: 16px;'>
                    We encountered an issue processing your order. Don't worry - no charges were made and your items have been returned to inventory.
                </p>
                
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f44336;'>
                    <h4 style='margin: 0 0 15px 0; color: #d32f2f;'>❌ Order #{orderData.OrderId}</h4>
                    <p style='margin: 5px 0;'><strong>Status:</strong> <span style='background: #ffebee; color: #c62828; padding: 3px 8px; border-radius: 12px; font-size: 12px;'>Failed</span></p>
                    <p style='margin: 5px 0;'><strong>Amount:</strong> ${orderData.TotalAmount:F2} (Not charged)</p>
                </div>

                <div style='background: #fff3e0; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #ff9800;'>
                    <h4 style='margin: 0 0 10px 0; color: #f57c00;'>🔄 What Happened?</h4>
                    <ul style='margin: 10px 0; padding-left: 20px;'>
                        <li>Payment processing encountered an issue</li>
                        <li>Your inventory has been restored</li>
                        <li>No charges were applied to your account</li>
                        <li>You can try placing the order again</li>
                    </ul>
                </div>

                <div style='text-align: center; margin: 30px 0;'>
                    <p style='background: #e3f2fd; color: #1976d2; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <strong>💡 Want to try again?</strong><br>
                        You can place a new order anytime. Your items are still available!
                    </p>
                </div>

                <p style='text-align: center; color: #666; font-size: 14px; margin-top: 30px;'>
                    Need help? Contact us at <a href='mailto:support@orion.com' style='color: #f44336;'>support@orion.com</a>
                </p>
            </div>
        </body>
        </html>";
    }
}