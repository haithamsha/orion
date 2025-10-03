using System.ComponentModel.DataAnnotations;

namespace Orion.Api.Configuration;

public class OrionConfiguration
{
    [Required]
    public DatabaseConfig Database { get; set; } = new();
    
    [Required]
    public RabbitMqConfig RabbitMq { get; set; } = new();
    
    [Required]
    public EmailConfig Email { get; set; } = new();
    
    [Required]
    public JwtConfig Jwt { get; set; } = new();
    
    [Required]
    public SignalRConfig SignalR { get; set; } = new();
    
    public MonitoringConfig Monitoring { get; set; } = new();
    
    [Required]
    public ApiConfig Api { get; set; } = new();
}

public class DatabaseConfig
{
    [Required]
    public string ConnectionString { get; set; } = "";
    
    [Range(1, 1000)]
    public int MaxRetryCount { get; set; } = 3;
    
    [Range(1, 300)]
    public int MaxRetryDelay { get; set; } = 30;
    
    [Range(5, 200)]
    public int MinPoolSize { get; set; } = 5;
    
    [Range(10, 500)]
    public int MaxPoolSize { get; set; } = 100;
    
    [Range(60, 3600)]
    public int ConnectionLifetime { get; set; } = 300;
}

public class RabbitMqConfig
{
    [Required]
    public string HostName { get; set; } = "";
    
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;
    
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string VirtualHost { get; set; } = "/";
    
    [Range(1, 300)]
    public int RequestedHeartbeat { get; set; } = 60;
    
    [Range(1, 300)]
    public int NetworkRecoveryInterval { get; set; } = 10;
    
    public bool AutomaticRecoveryEnabled { get; set; } = true;
}

public class EmailConfig
{
    [Required]
    public string Provider { get; set; } = "SendGrid"; // SendGrid, SMTP, AWS-SES
    
    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = "";
    
    [Required]
    public string FromName { get; set; } = "";
    
    [EmailAddress]
    public string ReplyToEmail { get; set; } = "";
    
    // SendGrid Configuration
    public SendGridConfig SendGrid { get; set; } = new();
    
    // SMTP Configuration
    public SmtpConfig Smtp { get; set; } = new();
    
    // AWS SES Configuration
    public AwsSesConfig AwsSes { get; set; } = new();
}

public class SendGridConfig
{
    public string ApiKey { get; set; } = "";
    public bool EnableClickTracking { get; set; } = true;
    public bool EnableOpenTracking { get; set; } = true;
}

public class SmtpConfig
{
    public string Host { get; set; } = "";
    
    [Range(1, 65535)]
    public int Port { get; set; } = 587;
    
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    
    [Range(5000, 120000)]
    public int Timeout { get; set; } = 30000;
}

public class AwsSesConfig
{
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
}

public class JwtConfig
{
    [Required]
    [MinLength(32)]
    public string SecretKey { get; set; } = "";
    
    [Required]
    public string Issuer { get; set; } = "";
    
    [Required]
    public string Audience { get; set; } = "";
    
    [Range(1, 43200)] // 1 minute to 12 hours
    public int ExpirationMinutes { get; set; } = 60;
    
    [Range(1, 10080)] // 1 minute to 1 week
    public int RefreshTokenExpirationMinutes { get; set; } = 1440;
    
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}

public class SignalRConfig
{
    public List<string> CorsOrigins { get; set; } = new();
    
    [Range(1, 86400)]
    public int KeepAliveInterval { get; set; } = 15;
    
    [Range(1, 300)]
    public int ClientTimeoutInterval { get; set; } = 30;
    
    [Range(1, 120)]
    public int HandshakeTimeout { get; set; } = 15;
    
    [Range(1024, 1048576)]
    public int MaximumReceiveMessageSize { get; set; } = 32768;
}

public class MonitoringConfig
{
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableTracing { get; set; } = false;
    
    public string HealthCheckPath { get; set; } = "/health";
    public string MetricsPath { get; set; } = "/metrics";
    
    // Application Insights / Other monitoring
    public string ApplicationInsightsKey { get; set; } = "";
    public string ElasticSearchUrl { get; set; } = "";
    public string PrometheusUrl { get; set; } = "";
}

public class ApiConfig
{
    [Required]
    public string BaseUrl { get; set; } = "";
    
    [Required]
    public string ApiKey { get; set; } = "";
    
    [Range(1, 86400)]
    public int RequestTimeoutSeconds { get; set; } = 30;
    
    [Range(1, 10000)]
    public int MaxConcurrentRequests { get; set; } = 100;
    
    // Rate Limiting
    public RateLimitConfig RateLimit { get; set; } = new();
    
    // CORS
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE" };
    public List<string> AllowedHeaders { get; set; } = new() { "*" };
}

public class RateLimitConfig
{
    public bool Enabled { get; set; } = false;
    
    [Range(1, 10000)]
    public int RequestsPerMinute { get; set; } = 100;
    
    [Range(1, 100000)]
    public int RequestsPerHour { get; set; } = 1000;
    
    [Range(1, 1000000)]
    public int RequestsPerDay { get; set; } = 10000;
}