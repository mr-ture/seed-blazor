using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MIF.Modules.Todos.Application;
using MIF.Modules.Todos.Infrastructure;

namespace MIF.Modules.Todos;

public static class DependencyInjection
{
    public static IServiceCollection AddTodosModule(this IServiceCollection services)
    {
        // Register repositories - DbContext will be resolved at runtime
        services.AddScoped<ITodoRepository, TodoRepository>();

        return services;
    }
}
