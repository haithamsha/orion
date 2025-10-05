using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Orion.Api.Data;
using Orion.Api.Models;
using Orion.Api.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Authentication;

namespace Orion.Api.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _dbName = $"OrionTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<OrionDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add a database context (OrionDbContext) using an in-memory database for testing.
            services.AddDbContext<OrionDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddHttpContextAccessor();

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => {});

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database contexts.
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<OrionDbContext>();

                // Ensure the database is created.
                db.Database.EnsureCreated();

                // Seeding is now done in the test constructor
                // SeedData(db);
            }
        });
    }

    public void SeedData(OrionDbContext context)
    {
        // Add test users
        context.Users.AddRange(
            new User 
            { 
                Id = 1, 
                UserId = "test-user-123", 
                FirstName = "Test", 
                LastName = "User",
                Email = "test@example.com" 
            }
        );

        // Add test inventory
        context.Inventories.AddRange(
            new Inventory 
            { 
                Id = 1, 
                ProductSku = "INTEGRATION-001", 
                ProductName = "Integration Test Product 1", 
                Price = 25.00m, 
                AvailableQuantity = 100 
            },
            new Inventory 
            { 
                Id = 2, 
                ProductSku = "INTEGRATION-002", 
                ProductName = "Integration Test Product 2", 
                Price = 15.00m, 
                AvailableQuantity = 50 
            }
        );

        context.SaveChanges();
    }
}
