using MIF.Modules.Todos.Application;
using MIF.SharedKernel.Application;
using Microsoft.Extensions.Logging;

namespace MIF.Modules.Todos.Application.Commands;

public record DeleteTodoCommand(int Id);

public class DeleteTodoCommandHandler
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<DeleteTodoCommandHandler> _logger;

    public DeleteTodoCommandHandler(ITodoRepository repository, ILogger<DeleteTodoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteTodoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting todo ID: {TodoId}", command.Id);

        var result = await _repository.DeleteAsync(command.Id, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to delete todo ID {TodoId}: {Error}", command.Id, result.Error.Message);
            return result;
        }

        _logger.LogInformation("Todo ID: {TodoId} deleted successfully", command.Id);
        return Result.Success();
    }
}
