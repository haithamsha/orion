using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Orion.Infrastructure.Data;

namespace Orion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework with PostgreSQL
        services.AddDbContext<OrionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("OrionDb")));
        
        return services;
    }
}