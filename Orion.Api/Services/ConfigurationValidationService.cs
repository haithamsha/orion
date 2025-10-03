using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;
using Orion.Api.Configuration;
using Orion.Api.Data;

namespace Orion.Api.Services;

public interface IConfigurationValidationService
{
    Task<ConfigurationValidationResult> ValidateAllConfigurationsAsync();
    Task<bool> ValidateDatabaseConnectionAsync();
    Task<bool> ValidateRabbitMqConnectionAsync();
    Task<bool> ValidateEmailConfigurationAsync();
}

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly OrionConfiguration _config;
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConfigurationValidationService(
        IOptions<OrionConfiguration> config,
        ILogger<ConfigurationValidationService> logger,
        IServiceProvider serviceProvider)
    {
        _config = config.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<ConfigurationValidationResult> ValidateAllConfigurationsAsync()
    {
        var result = new ConfigurationValidationResult();
        var errors = new List<string>();

        try
        {
            // 1. Validate Configuration Model Structure
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(_config);
            
            if (!Validator.TryValidateObject(_config, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error"));
            }

            // 2. Validate Database Connection
            result.DatabaseValid = await ValidateDatabaseConnectionAsync();
            if (!result.DatabaseValid)
                errors.Add("Database connection validation failed");

            // 3. Validate RabbitMQ Connection
            result.RabbitMqValid = await ValidateRabbitMqConnectionAsync();
            if (!result.RabbitMqValid)
                errors.Add("RabbitMQ connection validation failed");

            // 4. Validate Email Configuration
            result.EmailValid = await ValidateEmailConfigurationAsync();
            if (!result.EmailValid)
                errors.Add("Email configuration validation failed");

            // 5. Validate JWT Configuration
            result.JwtValid = ValidateJwtConfiguration();
            if (!result.JwtValid)
                errors.Add("JWT configuration validation failed");

            // 6. Validate API Configuration
            result.ApiValid = ValidateApiConfiguration();
            if (!result.ApiValid)
                errors.Add("API configuration validation failed");

            result.IsValid = result.DatabaseValid && result.RabbitMqValid && 
                           result.EmailValid && result.JwtValid && result.ApiValid;
            result.Errors = errors;
            result.ValidationCompletedAt = DateTime.UtcNow;

            if (result.IsValid)
            {
                _logger.LogInformation("✅ All configurations validated successfully");
            }
            else
            {
                _logger.LogError("❌ Configuration validation failed: {Errors}", string.Join(", ", errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception during configuration validation");
            result.IsValid = false;
            result.Errors = new List<string> { $"Validation exception: {ex.Message}" };
            return result;
        }
    }

    public async Task<bool> ValidateDatabaseConnectionAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrionDbContext>();
            
            await dbContext.Database.CanConnectAsync();
            _logger.LogInformation("✅ Database connection validated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database connection validation failed");
            return false;
        }
    }

    public async Task<bool> ValidateRabbitMqConnectionAsync()
    {
        try
        {
            // Test RabbitMQ connection
            using var factory = new RabbitMQ.Client.ConnectionFactory()
            {
                HostName = _config.RabbitMq.HostName,
                Port = _config.RabbitMq.Port,
                UserName = string.IsNullOrEmpty(_config.RabbitMq.UserName) ? "guest" : _config.RabbitMq.UserName,
                Password = string.IsNullOrEmpty(_config.RabbitMq.Password) ? "guest" : _config.RabbitMq.Password,
                VirtualHost = _config.RabbitMq.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(_config.RabbitMq.RequestedHeartbeat),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_config.RabbitMq.NetworkRecoveryInterval),
                AutomaticRecoveryEnabled = _config.RabbitMq.AutomaticRecoveryEnabled
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            
            _logger.LogInformation("✅ RabbitMQ connection validated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RabbitMQ connection validation failed");
            return false;
        }
    }

    public async Task<bool> ValidateEmailConfigurationAsync()
    {
        try
        {
            var emailService = _serviceProvider.GetRequiredService<IEmailService>();
            
            // For now, just check if service can be created and basic config exists
            var hasValidConfig = !string.IsNullOrEmpty(_config.Email.FromEmail) && 
                               !string.IsNullOrEmpty(_config.Email.FromName);

            if (hasValidConfig)
            {
                _logger.LogInformation("✅ Email configuration validated");
                return true;
            }
            else
            {
                _logger.LogWarning("⚠️ Email configuration incomplete");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Email configuration validation failed");
            return false;
        }
    }

    private bool ValidateJwtConfiguration()
    {
        try
        {
            var isValid = !string.IsNullOrEmpty(_config.Jwt.SecretKey) &&
                         _config.Jwt.SecretKey.Length >= 32 &&
                         !string.IsNullOrEmpty(_config.Jwt.Issuer) &&
                         !string.IsNullOrEmpty(_config.Jwt.Audience) &&
                         _config.Jwt.ExpirationMinutes > 0;

            if (isValid)
            {
                _logger.LogInformation("✅ JWT configuration validated");
            }
            else
            {
                _logger.LogWarning("⚠️ JWT configuration validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ JWT configuration validation failed");
            return false;
        }
    }

    private bool ValidateApiConfiguration()
    {
        try
        {
            var isValid = !string.IsNullOrEmpty(_config.Api.BaseUrl) &&
                         !string.IsNullOrEmpty(_config.Api.ApiKey) &&
                         _config.Api.RequestTimeoutSeconds > 0;

            if (isValid)
            {
                _logger.LogInformation("✅ API configuration validated");
            }
            else
            {
                _logger.LogWarning("⚠️ API configuration validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ API configuration validation failed");
            return false;
        }
    }
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public bool DatabaseValid { get; set; }
    public bool RabbitMqValid { get; set; }
    public bool EmailValid { get; set; }
    public bool JwtValid { get; set; }
    public bool ApiValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ValidationCompletedAt { get; set; }

    public string GetValidationSummary()
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Configuration Validation Summary ({ValidationCompletedAt:yyyy-MM-dd HH:mm:ss} UTC)");
        summary.AppendLine($"Overall Status: {(IsValid ? "✅ VALID" : "❌ INVALID")}");
        summary.AppendLine();
        summary.AppendLine("Component Status:");
        summary.AppendLine($"  Database: {(DatabaseValid ? "✅" : "❌")}");
        summary.AppendLine($"  RabbitMQ: {(RabbitMqValid ? "✅" : "❌")}");
        summary.AppendLine($"  Email: {(EmailValid ? "✅" : "❌")}");
        summary.AppendLine($"  JWT: {(JwtValid ? "✅" : "❌")}");
        summary.AppendLine($"  API: {(ApiValid ? "✅" : "❌")}");
        
        if (Errors.Any())
        {
            summary.AppendLine();
            summary.AppendLine("Errors:");
            foreach (var error in Errors)
            {
                summary.AppendLine($"  • {error}");
            }
        }
        
        return summary.ToString();
    }
}