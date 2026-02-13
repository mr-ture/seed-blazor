using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.FluentValidation;
using MIF.SharedKernel.Data;
using MIF.SharedKernel.Extensions;
using MIF.Modules.Todos;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AppDbContext as DbContext for module repositories
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    // Auto-discover handlers in the Todos module
    opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
});

// Register Module Services (Dependencies)
builder.Services.AddTodosModule();

// Add Endpoints from Modules
// Scan for IEndpoint implementations in the Todos module assembly
var moduleAssemblies = new[] { typeof(DependencyInjection).Assembly };
builder.Services.AddEndpoints(moduleAssemblies);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Map Endpoints
app.MapEndpoints();

app.Run();
