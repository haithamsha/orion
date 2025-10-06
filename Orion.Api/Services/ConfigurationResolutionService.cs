using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Orion.Api.Configuration;

namespace Orion.Api.Services;

public interface IConfigurationResolutionService
{
    Task<OrionConfiguration> ResolveConfigurationAsync();
    Task<string> ResolveValueAsync(string value);
    Task<bool> ValidateEnvironmentVariablesAsync();
}

public class ConfigurationResolutionService : IConfigurationResolutionService
{
    private readonly OrionConfiguration _rawConfig;
    private readonly ISecretsManagerService _secretsManager;
    private readonly ILogger<ConfigurationResolutionService> _logger;
    private readonly Regex _environmentVariablePattern = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);

    public ConfigurationResolutionService(
        IOptions<OrionConfiguration> rawConfig,
        ISecretsManagerService secretsManager,
        ILogger<ConfigurationResolutionService> logger)
    {
        _rawConfig = rawConfig.Value;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<OrionConfiguration> ResolveConfigurationAsync()
    {
        _logger.LogInformation("🔧 Starting configuration resolution...");

        try
        {
            var resolvedConfig = new OrionConfiguration
            {
                Database = new DatabaseConfig
                {
                    ConnectionString = await ResolveValueAsync(_rawConfig.Database.ConnectionString),
                    MaxRetryCount = _rawConfig.Database.MaxRetryCount,
                    MaxRetryDelay = _rawConfig.Database.MaxRetryDelay,
                    MinPoolSize = _rawConfig.Database.MinPoolSize,
                    MaxPoolSize = _rawConfig.Database.MaxPoolSize,
                    ConnectionLifetime = _rawConfig.Database.ConnectionLifetime
                },
                
                RabbitMq = new RabbitMqConfig
                {
                    HostName = await ResolveValueAsync(_rawConfig.RabbitMq.HostName),
                    Port = _rawConfig.RabbitMq.Port,
                    UserName = await ResolveValueAsync(_rawConfig.RabbitMq.UserName),
                    Password = await ResolveValueAsync(_rawConfig.RabbitMq.Password),
                    VirtualHost = _rawConfig.RabbitMq.VirtualHost,
                    RequestedHeartbeat = _rawConfig.RabbitMq.RequestedHeartbeat,
                    NetworkRecoveryInterval = _rawConfig.RabbitMq.NetworkRecoveryInterval,
                    AutomaticRecoveryEnabled = _rawConfig.RabbitMq.AutomaticRecoveryEnabled
                },
                
                Email = new EmailConfig
                {
                    Provider = _rawConfig.Email.Provider,
                    FromEmail = await ResolveValueAsync(_rawConfig.Email.FromEmail),
                    FromName = _rawConfig.Email.FromName,
                    ReplyToEmail = await ResolveValueAsync(_rawConfig.Email.ReplyToEmail),
                    SendGrid = new SendGridConfig
                    {
                        ApiKey = await ResolveValueAsync(_rawConfig.Email.SendGrid.ApiKey),
                        EnableClickTracking = _rawConfig.Email.SendGrid.EnableClickTracking,
                        EnableOpenTracking = _rawConfig.Email.SendGrid.EnableOpenTracking
                    },
                    Smtp = new SmtpConfig
                    {
                        Host = await ResolveValueAsync(_rawConfig.Email.Smtp.Host),
                        Port = _rawConfig.Email.Smtp.Port,
                        EnableSsl = _rawConfig.Email.Smtp.EnableSsl,
                        UserName = await ResolveValueAsync(_rawConfig.Email.Smtp.UserName),
                        Password = await ResolveValueAsync(_rawConfig.Email.Smtp.Password),
                        Timeout = _rawConfig.Email.Smtp.Timeout
                    }
                },
                
                Jwt = new JwtConfig
                {
                    SecretKey = await ResolveValueAsync(_rawConfig.Jwt.SecretKey),
                    Issuer = _rawConfig.Jwt.Issuer,
                    Audience = _rawConfig.Jwt.Audience,
                    ExpirationMinutes = _rawConfig.Jwt.ExpirationMinutes,
                    RefreshTokenExpirationMinutes = _rawConfig.Jwt.RefreshTokenExpirationMinutes,
                    ValidateIssuer = _rawConfig.Jwt.ValidateIssuer,
                    ValidateAudience = _rawConfig.Jwt.ValidateAudience,
                    ValidateLifetime = _rawConfig.Jwt.ValidateLifetime,
                    ValidateIssuerSigningKey = _rawConfig.Jwt.ValidateIssuerSigningKey
                },
                
                SignalR = _rawConfig.SignalR,
                Monitoring = _rawConfig.Monitoring,
                Api = new ApiConfig
                {
                    BaseUrl = await ResolveValueAsync(_rawConfig.Api.BaseUrl),
                    ApiKey = await ResolveValueAsync(_rawConfig.Api.ApiKey),
                    RequestTimeoutSeconds = _rawConfig.Api.RequestTimeoutSeconds,
                    MaxConcurrentRequests = _rawConfig.Api.MaxConcurrentRequests,
                    RateLimit = _rawConfig.Api.RateLimit,
                    AllowedOrigins = _rawConfig.Api.AllowedOrigins,
                    AllowedMethods = _rawConfig.Api.AllowedMethods,
                    AllowedHeaders = _rawConfig.Api.AllowedHeaders
                }
            };

            _logger.LogInformation("✅ Configuration resolution completed successfully");
            return resolvedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Configuration resolution failed");
            throw;
        }
    }

    public async Task<string> ResolveValueAsync(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var matches = _environmentVariablePattern.Matches(value);
        if (!matches.Any())
            return value;

        var resolvedValue = value;
        foreach (Match match in matches)
        {
            var envVarName = match.Groups[1].Value;
            var envVarValue = await _secretsManager.GetSecretAsync(envVarName);
            
            if (!string.IsNullOrEmpty(envVarValue))
            {
                resolvedValue = resolvedValue.Replace(match.Value, envVarValue);
                _logger.LogDebug("✅ Resolved environment variable: {EnvVar}", envVarName);
            }
            else
            {
                _logger.LogWarning("⚠️ Environment variable not found: {EnvVar}", envVarName);
                // Keep the placeholder for now, validation will catch this
            }
        }

        return resolvedValue;
    }

    public async Task<bool> ValidateEnvironmentVariablesAsync()
    {
        _logger.LogInformation("🔍 Validating environment variables...");

        var requiredEnvVars = ExtractEnvironmentVariables(_rawConfig);
        var missingVars = new List<string>();

        foreach (var envVar in requiredEnvVars)
        {
            var value = await _secretsManager.GetSecretAsync(envVar);
            if (string.IsNullOrEmpty(value))
            {
                missingVars.Add(envVar);
            }
        }

        if (missingVars.Any())
        {
            _logger.LogError("❌ Missing required environment variables: {MissingVars}", 
                string.Join(", ", missingVars));
            return false;
        }

        _logger.LogInformation("✅ All required environment variables are present");
        return true;
    }

    private List<string> ExtractEnvironmentVariables(OrionConfiguration config)
    {
        var envVars = new HashSet<string>();
        
        // Extract from all string properties that contain ${...} patterns
        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var matches = _environmentVariablePattern.Matches(configJson);
        
        foreach (Match match in matches)
        {
            envVars.Add(match.Groups[1].Value);
        }

        return envVars.ToList();
    }
}