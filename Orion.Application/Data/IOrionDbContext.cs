using Microsoft.EntityFrameworkCore;
using Orion.Domain.Data;

namespace Orion.Application.Data;

public interface IOrionDbContext
{
    DbSet<EventStoreEntry> EventStore { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
