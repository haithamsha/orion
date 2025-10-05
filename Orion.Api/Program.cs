using Serilog;
using Orion.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

// Configure Serilog for bootstrap logging to catch startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("🚀 Starting Orion E-Commerce Backend...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT Key is not configured in appsettings.json.");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddDbContext<OrionDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("OrionDb"), npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    builder.Services.AddSignalR();

    // Business Services
    builder.Services.AddSingleton<Orion.Api.Services.IMessagePublisher, Orion.Api.Services.RabbitMqPublisher>();
    builder.Services.AddScoped<Orion.Api.Services.IInventoryService, Orion.Api.Services.InventoryService>();
    
    builder.Services.AddHttpClient<Orion.Api.Services.IEmailService, Orion.Api.Services.SimpleEmailService>()
        .AddPolicyHandler(GetRetryPolicy());

    // Event Sourcing Services
    builder.Services.AddScoped<Orion.Api.Services.EventSourcing.IEventStore, Orion.Api.Services.EventSourcing.EventStore>();

    // Projection Services for Read Models
    builder.Services.AddScoped<Orion.Api.Projections.IProjectionService, Orion.Api.Projections.ProjectionService>();
    builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderCreatedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
    builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderStatusChangedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
    builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderCompletedEvent>, Orion.Api.Projections.OrderProjectionHandler>();
    builder.Services.AddScoped<Orion.Api.Projections.IProjectionHandler<Orion.Api.Models.Events.OrderFailedEvent>, Orion.Api.Projections.OrderProjectionHandler>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<Orion.Api.Hubs.OrderStatusHub>("/order-status-hub");

    Log.Information("✅ Orion Backend is ready to handle requests!");
    Log.Information("🚀 Started at: {StartupTime} UTC", DateTime.UtcNow);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Log.Warning("Delaying for {delay}ms, then making retry {retry}.", timespan.TotalMilliseconds, retryAttempt);
        });
}

// Make Program class accessible for testing
public partial class Program { }
