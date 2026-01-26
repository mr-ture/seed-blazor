using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MIF.SharedKernel.Interfaces;
using MIF.Modules.Todos.Application.Commands;
using MIF.Modules.Todos.Application.Queries;
using MIF.Modules.Todos.Application.DTOs;
using MIF.SharedKernel.Application;
using Wolverine;

namespace MIF.Modules.Todos.Endpoints;

public class TodoEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapPost("/", async (CreateTodoCommand command, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<int>>(command);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/", async (int? pageNumber, int? pageSize, IMessageBus bus) =>
        {
            var query = new GetTodosQuery(pageNumber ?? 1, pageSize ?? 10);
            var result = await bus.InvokeAsync<Result<PaginatedList<TodoItemDto>>>(query);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        group.MapPut("/{id}/toggle", async (int id, IMessageBus bus) =>
        {
            var command = new ToggleTodoCommand(id);
            var result = await bus.InvokeAsync<Result>(command);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(result.Error);
        });
    }
}
