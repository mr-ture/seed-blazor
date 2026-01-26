using MIF.Modules.Todos.Application;
using MIF.SharedKernel.Application;
using Microsoft.Extensions.Logging;

namespace MIF.Modules.Todos.Application.Commands;

public record ToggleTodoCommand(int Id);

public class ToggleTodoCommandHandler
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<ToggleTodoCommandHandler> _logger;

    public ToggleTodoCommandHandler(ITodoRepository repository, ILogger<ToggleTodoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(ToggleTodoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling completion status for todo ID: {TodoId}", command.Id);
        
        var result = await _repository.ToggleCompletionAsync(command.Id, cancellationToken);
        
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to toggle todo ID {TodoId}: {Error}", command.Id, result.Error.Message);
            return result;
        }
        
        _logger.LogInformation("Todo ID: {TodoId} status toggled successfully", command.Id);
        return Result.Success();
    }
}
