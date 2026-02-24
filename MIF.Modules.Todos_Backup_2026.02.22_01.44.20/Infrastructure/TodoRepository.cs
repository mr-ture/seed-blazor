using MIF.SharedKernel.Application;
using MIF.Modules.Todos.Domain;
using MIF.Modules.Todos.Application;
using Microsoft.EntityFrameworkCore;

namespace MIF.Modules.Todos.Infrastructure;

public class TodoRepository : ITodoRepository
{
    private readonly DbContext _context;
    private readonly DbSet<TodoItem> _todoItems;

    public TodoRepository(DbContext context)
    {
        _context = context;
        _todoItems = context.Set<TodoItem>();
    }

    public async Task<Result<TodoItem>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _todoItems.FindAsync(new object[] { id }, cancellationToken);
        return item != null
            ? Result.Success(item)
            : Result.Failure<TodoItem>(Error.NotFound(nameof(TodoItem), id));
    }

    public async Task<Result<int>> AddAsync(TodoItem entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _todoItems.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(entity.Id);
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure<int>(new Error("Database.SaveFailed", $"Failed to save todo: {ex.Message}"));
        }
    }

    public async Task<Result> UpdateAsync(TodoItem entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new Error("Database.UpdateFailed", $"Failed to update todo: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        try
        {
            _todoItems.Remove(result.Value!);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new Error("Database.DeleteFailed", $"Failed to delete todo: {ex.Message}"));
        }
    }

    public async Task<Result<PaginatedList<TodoItem>>> GetTodosWithPaginationAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await PaginatedList<TodoItem>.CreateAsync(
                _todoItems.AsNoTracking().OrderBy(t => t.Title), 
                pageNumber, 
                pageSize, 
                cancellationToken);
            return Result.Success(list);
        }
        catch (Exception ex)
        {
            return Result.Failure<PaginatedList<TodoItem>>(new Error("Database.QueryFailed", $"Failed to retrieve todos: {ex.Message}"));
        }
    }

    public async Task<Result> ToggleCompletionAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        try
        {
            var item = result.Value!;
            item.IsCompleted = !item.IsCompleted;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new Error("Database.UpdateFailed", $"Failed to toggle todo completion: {ex.Message}"));
        }
    }
}
