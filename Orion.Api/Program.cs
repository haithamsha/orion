using Hangfire;
using Hangfire.PostgreSql;
using Orion.Api.Services;
using Microsoft.EntityFrameworkCore;
using Orion.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configure Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("OrionDb");
builder.Services.AddDbContext<OrionDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Add our new Order Processing Service
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();

// 4. Add and Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

// Add the processing server as a hosted service
builder.Services.AddHangfireServer();

var app = builder.Build();

// 5. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 6. Add the Hangfire Dashboard
app.UseHangfireDashboard(); // You can access this at /hangfire

app.MapControllers();

app.Run();