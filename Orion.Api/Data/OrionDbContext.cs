using Microsoft.EntityFrameworkCore;
using Orion.Api.Models;

namespace Orion.Api.Data;

public class OrionDbContext : DbContext
{
    public OrionDbContext(DbContextOptions<OrionDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
}