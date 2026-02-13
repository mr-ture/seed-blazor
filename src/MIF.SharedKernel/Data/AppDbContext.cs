using Microsoft.EntityFrameworkCore;

namespace MIF.SharedKernel.Data;

/// <summary>
/// Shared EF Core context used by all modules.
/// It applies module mappings so database-owned schema stays outside host projects.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Creates the context with externally provided options (connection string, provider, etc.).
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Applies EF configurations discovered from loaded module assemblies.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-discover and apply all IEntityTypeConfiguration from module assemblies
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith("MIF.Modules."));
        
        foreach (var assembly in moduleAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
        
        base.OnModelCreating(modelBuilder);
    }
}
