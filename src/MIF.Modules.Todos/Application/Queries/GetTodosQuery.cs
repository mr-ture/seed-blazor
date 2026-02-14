using MIF.SharedKernel.Application;
using MIF.Modules.Todos.Application.DTOs;
using MIF.Modules.Todos.Application;
using Microsoft.Extensions.Logging;
using Mapster;

namespace MIF.Modules.Todos.Application.Queries;

public record GetTodosQuery(int PageNumber = 1, int PageSize = 10);

public class GetTodosQueryHandler
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<GetTodosQueryHandler> _logger;

    public GetTodosQueryHandler(ITodoRepository repository, ILogger<GetTodosQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<TodoItemDto>>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving todos - Page: {PageNumber}, PageSize: {PageSize}", query.PageNumber, query.PageSize);
        
        var result = await _repository.GetTodosWithPaginationAsync(query.PageNumber, query.PageSize, cancellationToken);
        
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to retrieve todos: {Error}", result.Error.Message);
            return Result.Failure<PaginatedList<TodoItemDto>>(result.Error);
        }
        
        PaginatedList<Domain.TodoItem>? paginatedTodos = result.Value!;
        List<TodoItemDto>? dtos = paginatedTodos.Items.Adapt<List<TodoItemDto>>();

        _logger.LogInformation("Retrieved {Count} todos out of {TotalCount} total", dtos.Count, paginatedTodos.TotalCount);

        return Result.Success(new PaginatedList<TodoItemDto>(dtos, paginatedTodos.TotalCount, paginatedTodos.PageNumber, query.PageSize));
    }
}
