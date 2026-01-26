using Microsoft.EntityFrameworkCore;

namespace MIF.SharedKernel.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
