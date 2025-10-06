using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Orion.Api.Configuration;
using Orion.Api.Services;
using Orion.Api.Data;

namespace Orion.Api.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddOrionConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind and validate configuration
        var orionConfig = new OrionConfiguration();
        configuration.GetSection("Orion").Bind(orionConfig);
        
        // Register as singleton for DI
        services.Configure<OrionConfiguration>(configuration.GetSection("Orion"));
        services.AddSingleton(orionConfig);
        
        // Add configuration validation service
        services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
        
        return services;
    }
    
    public static IServiceCollection AddOrionDatabase(
        this IServiceCollection services, 
        OrionConfiguration config)
    {
        services.AddDbContext<OrionDbContext>(options =>
        {
            options.UseNpgsql(config.Database.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: config.Database.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(config.Database.MaxRetryDelay),
                    errorCodesToAdd: null
                );
            });
            
            // Configure connection pooling
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        });
        
        return services;
    }
    
    public static IServiceCollection AddOrionAuthentication(
        this IServiceCollection services, 
        OrionConfiguration config)
    {
        var key = Encoding.ASCII.GetBytes(config.Jwt.SecretKey);
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = config.Jwt.ValidateIssuer,
                    ValidateAudience = config.Jwt.ValidateAudience,
                    ValidateLifetime = config.Jwt.ValidateLifetime,
                    ValidateIssuerSigningKey = config.Jwt.ValidateIssuerSigningKey,
                    ValidIssuer = config.Jwt.Issuer,
                    ValidAudience = config.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // No tolerance for clock differences
                };
            });
            
        return services;
    }
    
    public static IServiceCollection AddOrionSignalR(
        this IServiceCollection services, 
        OrionConfiguration config)
    {
        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(config.SignalR.KeepAliveInterval);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(config.SignalR.ClientTimeoutInterval);
            options.HandshakeTimeout = TimeSpan.FromSeconds(config.SignalR.HandshakeTimeout);
            options.MaximumReceiveMessageSize = config.SignalR.MaximumReceiveMessageSize;
        });
        
        return services;
    }
    
    public static IServiceCollection AddOrionCors(
        this IServiceCollection services, 
        OrionConfiguration config)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("OrionPolicy", builder =>
            {
                builder.WithOrigins(config.Api.AllowedOrigins.ToArray())
                       .WithMethods(config.Api.AllowedMethods.ToArray())
                       .WithHeaders(config.Api.AllowedHeaders.ToArray())
                       .AllowCredentials();
            });
            
            // SignalR-specific CORS
            if (config.SignalR.CorsOrigins.Any())
            {
                options.AddPolicy("SignalRPolicy", builder =>
                {
                    builder.WithOrigins(config.SignalR.CorsOrigins.ToArray())
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            }
        });
        
        return services;
    }
}