using MIF.SharedKernel.Application;
using MIF.Modules.Todos.Domain;

namespace MIF.Modules.Todos.Application;

public interface ITodoRepository : IRepository<TodoItem>
{
    Task<Result<PaginatedList<TodoItem>>> GetTodosWithPaginationAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> ToggleCompletionAsync(int id, CancellationToken cancellationToken = default);
}
