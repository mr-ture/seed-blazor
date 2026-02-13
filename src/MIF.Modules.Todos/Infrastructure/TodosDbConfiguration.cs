using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIF.Modules.Todos.Domain;

namespace MIF.Modules.Todos.Infrastructure;

/// <summary>
/// Scaffolded EF mapping for <see cref="TodoItem"/>.
/// Update database schema first, then re-scaffold instead of editing mapping manually.
/// </summary>
public class TodosDbConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems", tb => { });
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.IsCompleted)
            .IsRequired();
    }
}
