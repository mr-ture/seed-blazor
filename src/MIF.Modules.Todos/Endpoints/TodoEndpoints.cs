using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
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
        // Create an endpoint group for all todo-related routes
        // All endpoints in this group will be prefixed with "/api/todos"
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos") // Tag for OpenAPI/Swagger documentation
            .RequireAuthorization(); // 🔒 SECURITY: Require valid JWT token for ALL endpoints in this group
                                     // Users must be authenticated via Okta to access any todo operations

        // POST /api/todos - Create a new todo item
        // Requires: CreateTodoCommand in request body with todo details
        // Returns: 200 OK with the new todo ID, or 400 Bad Request if validation fails
        group.MapPost("/", async (CreateTodoCommand command, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<int>>(command);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        // GET /api/todos?pageNumber=1&pageSize=10 - Retrieve todos with pagination
        // Query Parameters:
        //   - pageNumber (optional): Page number to retrieve (default: 1)
        //   - pageSize (optional): Number of items per page (default: 10)
        // Returns: 200 OK with paginated list of todos, or 400 Bad Request on error
        group.MapGet("/", async (int? pageNumber, int? pageSize, IMessageBus bus) =>
        {
            var query = new GetTodosQuery(pageNumber ?? 1, pageSize ?? 10);
            var result = await bus.InvokeAsync<Result<PaginatedList<TodoItemDto>>>(query);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        // PUT /api/todos/{id}/toggle - Toggle the completion status of a todo
        // Route Parameter:
        //   - id: The ID of the todo to toggle
        // Returns: 204 No Content on success, or 400 Bad Request if todo not found/error
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
