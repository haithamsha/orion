using System.Text.Json;

namespace Orion.Api.Services;

public interface ISecretsManagerService
{
    Task<string?> GetSecretAsync(string secretName);
    Task<T?> GetSecretAsync<T>(string secretName) where T : class;
    Task<bool> SetSecretAsync(string secretName, string secretValue);
    Task<Dictionary<string, string>> GetSecretsAsync(params string[] secretNames);
}

public class EnvironmentSecretsManagerService : ISecretsManagerService
{
    private readonly ILogger<EnvironmentSecretsManagerService> _logger;

    public EnvironmentSecretsManagerService(ILogger<EnvironmentSecretsManagerService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            var secretValue = Environment.GetEnvironmentVariable(secretName);

            if (string.IsNullOrEmpty(secretValue))
            {
                _logger.LogWarning("Secret '{SecretName}' not found in environment variables", secretName);
                return Task.FromResult<string?>(null);
            }

            _logger.LogDebug("Successfully retrieved secret '{SecretName}' from environment", secretName);
            return Task.FromResult<string?>(secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}' from environment", secretName);
            return Task.FromResult<string?>(null);
        }
    }

    public async Task<T?> GetSecretAsync<T>(string secretName) where T : class
    {
        try
        {
            var secretValue = await GetSecretAsync(secretName);
            if (string.IsNullOrEmpty(secretValue))
                return null;

            return JsonSerializer.Deserialize<T>(secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing secret '{SecretName}' to type {Type}", secretName, typeof(T).Name);
            return null;
        }
    }

    public Task<bool> SetSecretAsync(string secretName, string secretValue)
    {
        try
        {
            Environment.SetEnvironmentVariable(secretName, secretValue);
            _logger.LogDebug("Successfully set secret '{SecretName}' in environment", secretName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secret '{SecretName}' in environment", secretName);
            return Task.FromResult(false);
        }
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(params string[] secretNames)
    {
        var secrets = new Dictionary<string, string>();

        foreach (var secretName in secretNames)
        {
            var secretValue = await GetSecretAsync(secretName);
            if (!string.IsNullOrEmpty(secretValue))
            {
                secrets[secretName] = secretValue;
            }
        }

        return secrets;
    }
}