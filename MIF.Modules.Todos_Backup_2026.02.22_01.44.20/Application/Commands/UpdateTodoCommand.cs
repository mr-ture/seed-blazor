using FluentValidation;
using MIF.Modules.Todos.Application;
using MIF.Modules.Todos.Domain;
using MIF.SharedKernel.Application;
using Microsoft.Extensions.Logging;

namespace MIF.Modules.Todos.Application.Commands;

public record UpdateTodoCommand(int Id, string Title, string AssignedTo, Importance Importance);

public class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("A valid todo ID is required.");

        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(10).WithMessage("Title must not exceed 10 characters.");

        RuleFor(v => v.AssignedTo)
            .NotEmpty().WithMessage("Assigned To is required.");

        RuleFor(v => v.Importance)
            .IsInEnum().WithMessage("Importance must be a valid value.");
    }
}

public class UpdateTodoCommandHandler
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<UpdateTodoCommandHandler> _logger;

    public UpdateTodoCommandHandler(ITodoRepository repository, ILogger<UpdateTodoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating todo ID: {TodoId}", command.Id);

        var todoResult = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (todoResult.IsFailure)
        {
            _logger.LogWarning("Todo ID {TodoId} not found", command.Id);
            return Result.Failure(todoResult.Error);
        }

        var todo = todoResult.Value!;
        todo.Title = command.Title;
        todo.AssignedTo = command.AssignedTo;
        todo.Importance = command.Importance;

        var updateResult = await _repository.UpdateAsync(todo, cancellationToken);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning("Failed to update todo ID {TodoId}: {Error}", command.Id, updateResult.Error.Message);
            return updateResult;
        }

        _logger.LogInformation("Todo ID {TodoId} updated successfully", command.Id);
        return Result.Success();
    }
}
