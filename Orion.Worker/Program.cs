using Microsoft.EntityFrameworkCore;
using Orion.Worker;
using Orion.Api.Data;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("OrionDb");
        
        // Register the DbContext
        services.AddDbContext<OrionDbContext>(options =>
            options.UseNpgsql(connectionString));
            
        // Register HttpClient for making API calls
        services.AddHttpClient();
            
        // Register our main worker service
        services.AddHostedService<OrderProcessorWorker>();
    })
    .Build();

host.Run();