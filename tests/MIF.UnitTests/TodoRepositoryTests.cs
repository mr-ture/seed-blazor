using MIF.Modules.Todos.Domain;
using MIF.SharedKernel.Data;
using MIF.Modules.Todos.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MIF.UnitTests;

public class TodoRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldAddTodoItem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "AddTodosDatabase")
            .Options;

        using (var context = new AppDbContext(options))
        {
            var repository = new TodoRepository(context);
            var item = new TodoItem { Title = "Test Todo", IsCompleted = false };

            // Act
            var result = await repository.AddAsync(item, CancellationToken.None);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value > 0);
        }

        // Assert
        using (var context = new AppDbContext(options))
        {
            var item = await context.Set<TodoItem>().FirstOrDefaultAsync();
            Assert.NotNull(item);
            Assert.Equal("Test Todo", item.Title);
            Assert.False(item.IsCompleted);
        }
    }

    [Fact]
    public async Task ToggleCompletionAsync_ShouldToggleIsCompleted()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ToggleTodosDatabase")
            .Options;

        using (var context = new AppDbContext(options))
        {
            context.Set<TodoItem>().Add(new TodoItem { Id = 1, Title = "Test Todo", IsCompleted = false });
            await context.SaveChangesAsync();
        }

        using (var context = new AppDbContext(options))
        {
            var repository = new TodoRepository(context);

            // Act
            var result = await repository.ToggleCompletionAsync(1, CancellationToken.None);
            
            // Assert
            Assert.True(result.IsSuccess);
        }

        // Assert
        using (var context = new AppDbContext(options))
        {
            var item = await context.Set<TodoItem>().FindAsync(1);
            Assert.NotNull(item);
            Assert.True(item.IsCompleted);
        }
    }

    [Fact]
    public async Task GetTodosWithPaginationAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "PaginationDatabase")
            .Options;

        using (var context = new AppDbContext(options))
        {
            for (int i = 1; i <= 20; i++)
            {
                context.Set<TodoItem>().Add(new TodoItem { Id = i, Title = $"Todo {i:D2}", IsCompleted = false });
            }
            await context.SaveChangesAsync();
        }

        using (var context = new AppDbContext(options))
        {
            var repository = new TodoRepository(context);

            // Act
            var result = await repository.GetTodosWithPaginationAsync(2, 5, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(5, result.Value.Items.Count); // Page size
            Assert.Equal(2, result.Value.PageNumber);
            Assert.Equal(4, result.Value.TotalPages); // 20 / 5 = 4
            Assert.Equal(20, result.Value.TotalCount);
            
            // Should contain items 6 to 10 (sorted alphabetically Todo 01 ... Todo 20)
            // Page 1: 01, 02, 03, 04, 05
            // Page 2: 06, 07, 08, 09, 10
            Assert.Contains(result.Value.Items, t => t.Title == "Todo 06");
            Assert.Contains(result.Value.Items, t => t.Title == "Todo 10");
        }
    }
}
