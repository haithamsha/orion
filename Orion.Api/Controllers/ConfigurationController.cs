using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orion.Api.Configuration;
using Orion.Api.Services;

namespace Orion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationValidationService _validationService;
    private readonly IConfigurationResolutionService _resolutionService;
    private readonly OrionConfiguration _config;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IConfigurationValidationService validationService,
        IConfigurationResolutionService resolutionService,
        IOptions<OrionConfiguration> config,
        ILogger<ConfigurationController> logger)
    {
        _validationService = validationService;
        _resolutionService = resolutionService;
        _config = config.Value;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetConfigurationHealth()
    {
        try
        {
            var validationResult = await _validationService.ValidateAllConfigurationsAsync();
            
            var response = new
            {
                timestamp = DateTime.UtcNow,
                status = validationResult.IsValid ? "Healthy" : "Unhealthy",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                validationResult = new
                {
                    validationResult.IsValid,
                    validationResult.DatabaseValid,
                    validationResult.RabbitMqValid,
                    validationResult.EmailValid,
                    validationResult.JwtValid,
                    validationResult.ApiValid,
                    errorCount = validationResult.Errors.Count,
                    errors = validationResult.Errors
                },
                summary = validationResult.GetValidationSummary()
            };

            return validationResult.IsValid ? Ok(response) : StatusCode(503, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Configuration health check failed");
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                status = "Error",
                error = ex.Message
            });
        }
    }

    [HttpGet("environment-check")]
    public async Task<IActionResult> CheckEnvironmentVariables()
    {
        try
        {
            var isValid = await _resolutionService.ValidateEnvironmentVariablesAsync();
            
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                environmentVariablesValid = isValid,
                status = isValid ? "All environment variables present" : "Missing required environment variables"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Environment variables check failed");
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    [HttpGet("summary")]
    public IActionResult GetConfigurationSummary()
    {
        try
        {
            var summary = new
            {
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                configuration = new
                {
                    database = new
                    {
                        maxPoolSize = _config.Database.MaxPoolSize,
                        maxRetryCount = _config.Database.MaxRetryCount,
                        connectionLifetime = _config.Database.ConnectionLifetime
                    },
                    rabbitmq = new
                    {
                        hostname = _config.RabbitMq.HostName,
                        port = _config.RabbitMq.Port,
                        virtualHost = _config.RabbitMq.VirtualHost
                    },
                    email = new
                    {
                        provider = _config.Email.Provider,
                        fromEmail = _config.Email.FromEmail,
                        fromName = _config.Email.FromName
                    },
                    jwt = new
                    {
                        issuer = _config.Jwt.Issuer,
                        audience = _config.Jwt.Audience,
                        expirationMinutes = _config.Jwt.ExpirationMinutes
                    },
                    api = new
                    {
                        baseUrl = _config.Api.BaseUrl,
                        rateLimitEnabled = _config.Api.RateLimit.Enabled,
                        maxConcurrentRequests = _config.Api.MaxConcurrentRequests,
                        allowedOrigins = _config.Api.AllowedOrigins
                    },
                    monitoring = new
                    {
                        healthChecksEnabled = _config.Monitoring.EnableHealthChecks,
                        metricsEnabled = _config.Monitoring.EnableMetrics,
                        tracingEnabled = _config.Monitoring.EnableTracing
                    }
                }
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Configuration summary failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("reload")]
    public async Task<IActionResult> ReloadConfiguration()
    {
        try
        {
            _logger.LogInformation("🔄 Reloading configuration...");

            // Resolve configuration with latest environment variables
            var resolvedConfig = await _resolutionService.ResolveConfigurationAsync();
            
            // Validate the new configuration
            var validationResult = await _validationService.ValidateAllConfigurationsAsync();

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = "Configuration reloaded",
                validationResult = new
                {
                    validationResult.IsValid,
                    validationResult.DatabaseValid,
                    validationResult.RabbitMqValid,
                    validationResult.EmailValid,
                    validationResult.JwtValid,
                    validationResult.ApiValid,
                    errors = validationResult.Errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Configuration reload failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}