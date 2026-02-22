using FluentValidation;
using MIF.Modules.Todos.Domain;
using MIF.Modules.Todos.Application;
using MIF.SharedKernel.Application;
using Microsoft.Extensions.Logging;

namespace MIF.Modules.Todos.Application.Commands;

using MIF.Modules.Todos.Domain;

public record CreateTodoCommand(string Title, string AssignedTo, Importance Importance);

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(10).WithMessage("Title must not exceed 10 characters.");

        RuleFor(v => v.AssignedTo)
            .NotEmpty().WithMessage("Assigned To is required.");

        RuleFor(v => v.Importance)
            .IsInEnum().WithMessage("Importance must be a valid value.");
    }
}

public class CreateTodoCommandHandler
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<CreateTodoCommandHandler> _logger;

    public CreateTodoCommandHandler(ITodoRepository repository, ILogger<CreateTodoCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new todo with title: {Title}, assigned to: {AssignedTo}, importance: {Importance}", command.Title, command.AssignedTo, command.Importance);

        var entity = new TodoItem
        {
            Title = command.Title,
            IsCompleted = false,
            AssignedTo = command.AssignedTo,
            Importance = command.Importance
        };

        var result = await _repository.AddAsync(entity, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create todo: {Error}", result.Error.Message);
            return Result.Failure<int>(result.Error);
        }

        _logger.LogInformation("Todo created successfully with ID: {TodoId}", result.Value);

        return Result.Success(result.Value);
    }
}
