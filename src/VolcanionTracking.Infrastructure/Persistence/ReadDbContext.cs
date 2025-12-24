using Microsoft.EntityFrameworkCore;

namespace VolcanionTracking.Infrastructure.Persistence;

/// <summary>
/// Read database context - optimized for read operations (CQRS Read side)
/// Can be a separate database or a read replica
/// </summary>
public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReadDbContext).Assembly);
        
        // Apply read-side specific configurations
        modelBuilder.HasDefaultSchema("read");
        
        base.OnModelCreating(modelBuilder);
    }
}
