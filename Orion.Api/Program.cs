using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using Orion.Api.Configuration;
using Orion.Api.Data;
using Orion.Api.Extensions;
using Orion.Api.Hubs;
using Orion.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// PHASE 1: CONFIGURATION SETUP AND VALIDATION
// =========================================================================

Console.WriteLine("🚀 Starting Orion E-Commerce Backend...");
Console.WriteLine($"🌍 Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"⏰ Startup Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

try
{
    // 1. Register configuration services first
    builder.Services.AddOrionConfiguration(builder.Configuration);
    
    // 2. Register secrets management (Environment Variables for now)
    builder.Services.AddScoped<ISecretsManagerService, EnvironmentSecretsManagerService>();
    
    // 3. Register configuration resolution and validation services
    builder.Services.AddScoped<IConfigurationResolutionService, ConfigurationResolutionService>();
    builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();

    // 4. Build temporary service provider to resolve configuration
    using var tempServiceProvider = builder.Services.BuildServiceProvider();
    var resolutionService = tempServiceProvider.GetRequiredService<IConfigurationResolutionService>();

    // 5. Validate environment variables first
    Console.WriteLine("🔍 Validating environment variables...");
    var envValid = await resolutionService.ValidateEnvironmentVariablesAsync();
    if (!envValid)
    {
        Console.WriteLine("❌ STARTUP FAILED: Missing required environment variables");
        Console.WriteLine("💡 For development, ensure all variables in appsettings.Development.json are available");
        Environment.Exit(1);
    }

    // 6. Resolve final configuration with secrets
    Console.WriteLine("🔧 Resolving configuration with secrets...");
    var resolvedConfig = await resolutionService.ResolveConfigurationAsync();
    
    // 7. Replace the configuration in DI container
    builder.Services.AddSingleton(resolvedConfig);

    Console.WriteLine("✅ Configuration resolution completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FATAL: Configuration setup failed: {ex.Message}");
    Environment.Exit(1);
}

// =========================================================================
// PHASE 2: SERVICE REGISTRATION WITH RESOLVED CONFIGURATION
// =========================================================================

// Get the resolved configuration for service setup
var serviceProvider = builder.Services.BuildServiceProvider();
var config = serviceProvider.GetRequiredService<OrionConfiguration>();

Console.WriteLine("📦 Registering application services...");

// Database with enhanced configuration
builder.Services.AddOrionDatabase(config);

// Authentication with resolved JWT settings
builder.Services.AddOrionAuthentication(config);

// SignalR with performance settings
builder.Services.AddOrionSignalR(config);

// CORS with environment-specific origins
builder.Services.AddOrionCors(config);

// Business Services
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IEmailService, SimpleEmailService>();

// Event Sourcing Services
builder.Services.AddScoped<Orion.Api.Services.EventSourcing.IEventStore, Orion.Api.Services.EventSourcing.EventStore>();

Console.WriteLine("✅ Event Sourcing services registered");

// CQRS with MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Projection Services for Read Models
builder.Services.AddScoped<Orion.Api.Projections.IProjectionService, Orion.Api.Projections.ProjectionService>();
builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderCreatedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderStatusChangedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderCompletedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderFailedEvent>, Orion.Api.Projections.OrderProjectionHandler>();

Console.WriteLine("✅ CQRS and Projection services registered");

// API Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Orion E-Commerce API", 
        Version = "v1",
        Description = "Production-ready e-commerce backend with advanced configuration management"
    });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});

Console.WriteLine("✅ Service registration completed");

// =========================================================================
// PHASE 3: APPLICATION BUILD AND FINAL VALIDATION
// =========================================================================

var app = builder.Build();

Console.WriteLine("🔎 Performing final configuration validation...");

// Final validation of all configurations at startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configValidator = scope.ServiceProvider.GetRequiredService<IConfigurationValidationService>();
        var validationResult = await configValidator.ValidateAllConfigurationsAsync();
        
        if (!validationResult.IsValid)
        {
            Console.WriteLine("❌ STARTUP FAILED - Configuration Validation Errors:");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(validationResult.GetValidationSummary());
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();
            Console.WriteLine("💡 Check your environment variables and configuration files");
            Console.WriteLine("💡 Use /api/configuration/health endpoint for detailed diagnostics");
            Environment.Exit(1);
        }
        
        Console.WriteLine("✅ All configurations validated successfully");
        Console.WriteLine();
        Console.WriteLine("📊 Configuration Summary:");
        Console.WriteLine($"   🗄️  Database: {config.Database.ConnectionString.Split(';')[0]}");
        Console.WriteLine($"   🐰 RabbitMQ: {config.RabbitMq.HostName}:{config.RabbitMq.Port}");
        Console.WriteLine($"   📧 Email Provider: {config.Email.Provider}");
        Console.WriteLine($"   🔐 JWT Issuer: {config.Jwt.Issuer}");
        Console.WriteLine($"   🌐 API Base URL: {config.Api.BaseUrl}");
        Console.WriteLine($"   📈 Monitoring: Health={config.Monitoring.EnableHealthChecks}, Metrics={config.Monitoring.EnableMetrics}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL: Final validation failed: {ex.Message}");
        Environment.Exit(1);
    }
}

// =========================================================================
// PHASE 4: MIDDLEWARE PIPELINE CONFIGURATION
// =========================================================================

Console.WriteLine("🔧 Configuring middleware pipeline...");

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orion API v1");
        c.RoutePrefix = "api-docs";
    });
    Console.WriteLine("📚 Swagger UI available at: /api-docs");
}

// Security and performance middleware
app.UseHttpsRedirection();

// CORS with environment-specific settings
app.UseCors("OrionPolicy");

// Authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// API routes
app.MapControllers();

// SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

// =========================================================================
// PHASE 5: MONITORING AND HEALTH CHECK ENDPOINTS
// =========================================================================

Console.WriteLine("🏥 Setting up health monitoring endpoints...");

// Configuration health endpoint
app.MapGet("/api/config/health", async (IConfigurationValidationService validator) =>
{
    var result = await validator.ValidateAllConfigurationsAsync();
    return result.IsValid ? Results.Ok(result) : Results.Problem(
        detail: result.GetValidationSummary(),
        statusCode: 503,
        title: "Configuration Health Check Failed"
    );
}).WithTags("Monitoring");

// System health endpoint
app.MapGet("/health", async (OrionDbContext dbContext) =>
{
    try
    {
        // Quick health checks
        var dbHealthy = await dbContext.Database.CanConnectAsync();
        var timestamp = DateTime.UtcNow;
        
        var health = new
        {
            status = dbHealthy ? "Healthy" : "Unhealthy",
            timestamp = timestamp,
            environment = app.Environment.EnvironmentName,
            version = "1.0.0",
            uptime = timestamp.Subtract(Process.GetCurrentProcess().StartTime.ToUniversalTime()),
            checks = new
            {
                database = dbHealthy ? "✅ Healthy" : "❌ Unhealthy",
                configuration = "✅ Validated",
                services = "✅ Running"
            }
        };
        
        return dbHealthy ? Results.Ok(health) : Results.Problem(
            detail: "Database connection failed",
            statusCode: 503
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Health Check Failed"
        );
    }
}).WithTags("Monitoring");

// Application info endpoint
app.MapGet("/api/info", (OrionConfiguration config) =>
{
    return Results.Ok(new
    {
        application = "Orion E-Commerce Backend",
        version = "1.0.0",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
        timestamp = DateTime.UtcNow,
        features = new
        {
            database = "✅ PostgreSQL with connection pooling",
            messaging = "✅ RabbitMQ event-driven architecture",
            realtime = "✅ SignalR notifications",
            email = $"✅ {config.Email.Provider} email service",
            authentication = "✅ JWT with validation",
            monitoring = "✅ Health checks and configuration validation",
            cors = "✅ Environment-specific CORS policies",
            rateLimit = config.Api.RateLimit.Enabled ? "✅ Enabled" : "⚠️ Disabled"
        },
        endpoints = new
        {
            api = "/api",
            health = "/health",
            configuration = "/api/config/health",
            docs = app.Environment.IsDevelopment() ? "/api-docs" : "Disabled in production",
            signalr = "/notificationHub"
        }
    });
}).WithTags("Information");

// =========================================================================
// PHASE 6: APPLICATION STARTUP
// =========================================================================

Console.WriteLine("🎯 Starting application...");
Console.WriteLine();
Console.WriteLine("🌟 " + new string('=', 50));
Console.WriteLine("   ORION E-COMMERCE BACKEND");
Console.WriteLine("   Production-Ready Configuration System");
Console.WriteLine("   " + new string('=', 50));
Console.WriteLine();
Console.WriteLine("📍 Available Endpoints:");
Console.WriteLine($"   🏠 Application Info: {config.Api.BaseUrl}/api/info");
Console.WriteLine($"   🏥 Health Check: {config.Api.BaseUrl}/health");
Console.WriteLine($"   ⚙️  Configuration Health: {config.Api.BaseUrl}/api/config/health");
Console.WriteLine($"   🛒 Orders API: {config.Api.BaseUrl}/api/orders");
Console.WriteLine($"   📧 Email Test: {config.Api.BaseUrl}/api/emailtest");
Console.WriteLine($"   ⚡ SignalR Hub: {config.Api.BaseUrl}/notificationHub");

if (app.Environment.IsDevelopment())
{
    Console.WriteLine($"   📚 API Documentation: {config.Api.BaseUrl}/api-docs");
}

Console.WriteLine();
Console.WriteLine("✅ Orion Backend is ready to handle requests!");
Console.WriteLine($"🚀 Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

app.Run();

// Make Program class accessible for testing
public partial class Program { }