# MIF Developer Onboarding Guide

Welcome to MIF! This guide will help you get up and running with the project quickly.

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- **.NET SDK 10.0** or later ([Download](https://dotnet.microsoft.com/download))
- **IDE**: Visual Studio 2025, Visual Studio Code, or JetBrains Rider
- **Git** for version control

### Verify Installation
```bash
dotnet --version  # Should show 10.0.x or later
```

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd MIF2.0
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build the Solution
```bash
dotnet build
```

### 4. Run the Application

**Blazor UI:**
```bash
dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj
```
The application will start on **http://localhost:5096**

**REST API:**
```bash
dotnet run --project src/MIF.API/MIF.API.csproj
```
The API will start on **http://localhost:5043**

### 5. Access the Application
Open your browser and navigate to:
- **Blazor UI**: http://localhost:5096
- **API Documentation**: http://localhost:5043/scalar/v1
- **OpenAPI JSON**: http://localhost:5043/openapi/v1.json

## 🏗️ Project Structure

```
MIF/
├── src/
│   ├── MIF.SharedKernel/    # Common abstractions, base classes, DbContext
│   ├── MIF.Modules.Todos/   # Todos feature module (vertical slice)
│   │   ├── Domain/          # Todo entities
│   │   ├── Application/     # Commands, queries, handlers
│   │   ├── Infrastructure/  # Repository implementations
│   │   └── Endpoints/       # API endpoint definitions
│   ├── MIF.API/             # REST API host (Minimal APIs)
│   └── MIF.WebUI/           # Blazor UI components
└── tests/
    └── MIF.UnitTests/       # Unit tests
```

### Architecture
- **Vertical Slice Architecture**: Features organized as self-contained modules
- **SharedKernel**: No dependencies, provides common abstractions and DbContext
- **Modules**: Depend only on SharedKernel (high cohesion, low coupling)
  - Each module defines its own API endpoints via `IEndpoint` interface (implements `MapEndpoint` method)
- **API**: Thin host that auto-discovers and maps module endpoints using `MapEndpoints()` extension
- **WebUI**: Blazor UI, references SharedKernel and feature modules

## 🔧 Key Technologies

| Technology | Purpose | Version |
|------------|---------|---------|
| **.NET** | Framework | 10.0 |
| **Blazor Server** | UI Framework | 10.0 |
| **MudBlazor** | Component Library | 8.15.0 |
| **Wolverine** | CQRS/Messaging | 5.11.0 |
| **FluentValidation** | Input Validation | 12.1.1 |
| **Entity Framework Core** | ORM | 10.0.2 |
| **SQLite** | Database | 10.0.2 |
| **Mapster** | Object Mapping | 7.4.0 |
| **Azure Monitor OpenTelemetry** | Observability | 1.3.0 |

## 📝 Development Workflows

### Adding a New Feature

#### 1. Create a Command or Query

**Command Example** (for write operations):
```csharp
// Modules.Todos/Application/Commands/CreateTodoCommand.cs
public record CreateTodoCommand(string Title);

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
        _logger.LogInformation("Creating new todo with title: {Title}", command.Title);
        
        var entity = new TodoItem
        {
            Title = command.Title,
            IsCompleted = false
        };
        
        var result = await _repository.AddAsync(entity, cancellationToken);
        
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create todo: {Error}", result.Error.Message);
            return Result.Failure<int>(result.Error);
        }
        
        _logger.LogInformation("Todo created successfully with ID: {TodoId}", result.Value);
        return result;
    }
}
```

**Query Example** (for read operations):
```csharp
// Modules.Todos/Application/Queries/GetTodosQuery.cs
using Mapster;

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
        
        var paginatedTodos = result.Value!;
        var dtos = paginatedTodos.Items.Adapt<List<TodoItemDto>>();
        
        _logger.LogInformation("Retrieved {Count} todos out of {TotalCount} total", dtos.Count, paginatedTodos.TotalCount);
        
        return Result.Success(new PaginatedList<TodoItemDto>(dtos, paginatedTodos.TotalCount, paginatedTodos.PageNumber, query.PageSize));
    }
}
```

#### 2. Add Validation (Optional)

```csharp
// Modules.Todos/Application/Commands/CreateTodoCommand.cs
public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(10).WithMessage("Title must not exceed 10 characters");
    }
}
```

Wolverine will automatically discover and apply validators!

#### 3. Use in Blazor Component

```razor
@inject IMessageBus MessageBus

<MudTextField @bind-Value="newTodo" Label="New Todo" />
<MudButton OnClick="CreateTodo">Add</MudButton>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <MudAlert Severity="Severity.Error">@errorMessage</MudAlert>
}

@code {
    private string newTodo = string.Empty;
    private string? errorMessage;

    private async Task CreateTodo()
    {
        try
        {
            var command = new CreateTodoCommand(newTodo);
            var result = await MessageBus.InvokeAsync<Result<int>>(command);
            
            if (result.IsSuccess)
            {
                newTodo = string.Empty;
                errorMessage = null;
                // Reload or update UI
            }
            else
            {
                errorMessage = result.Error.Message;
            }
        }
        catch (FluentValidation.ValidationException ex)
        {
            // Handle validation errors
            errorMessage = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
        }
    }
}
```

### Adding a New Repository Method

#### 1. Add to Interface
```csharp
// Modules.Todos/Application/ITodoRepository.cs
public interface ITodoRepository : IRepository<TodoItem>
{
    Task<Result<TodoItem>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<PaginatedList<TodoItem>>> GetTodosWithPaginationAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
```

#### 2. Implement in Repository
```csharp
// Modules.Todos/Infrastructure/TodoRepository.cs
public async Task<Result<TodoItem>> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    try
    {
        var item = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        return item != null
            ? Result.Success(item)
            : Result.Failure<TodoItem>(Error.NotFound(nameof(TodoItem), id));
    }
    catch (Exception ex)
    {
        return Result.Failure<TodoItem>(new Error("Database.QueryFailed", ex.Message));
    }
}
```

### Adding a Database Migration

When you modify entities:

```bash
# Add migration
dotnet ef migrations add MigrationName --project src/MIF.SharedKernel --startup-project src/MIF.WebUI

# Apply migration
dotnet ef database update --project src/MIF.SharedKernel --startup-project src/MIF.WebUI
```

### Adding a New Module

To create a completely new feature module:

#### 1. Create Project Structure
```bash
# Create the module project
dotnet new classlib -n MIF.Modules.YourFeature -o src/MIF.Modules.YourFeature

# Add reference to SharedKernel
dotnet add src/MIF.Modules.YourFeature/MIF.Modules.YourFeature.csproj reference src/MIF.SharedKernel/MIF.SharedKernel.csproj

# Add to solution
dotnet sln add src/MIF.Modules.YourFeature/MIF.Modules.YourFeature.csproj
```

#### 2. Create Folder Structure
```
MIF.Modules.YourFeature/
├── Domain/              # Your entities
├── Application/         # Commands, queries, handlers, DTOs, validators
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── IYourFeatureRepository.cs
├── Infrastructure/      # Repository implementations, EF configurations
├── Endpoints/           # API endpoints (for MIF.API)
└── DependencyInjection.cs
```

#### 3. Create DependencyInjection.cs
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace MIF.Modules.YourFeature;

public static class DependencyInjection
{
    public static IServiceCollection AddYourFeatureModule(this IServiceCollection services)
    {
        // Register module-specific services
        services.AddScoped<IYourFeatureRepository, YourFeatureRepository>();
        
        return services;
    }
}
```

#### 4. Register in MIF.API
```csharp
// src/MIF.API/Program.cs
using MIF.Modules.YourFeature;

// ... other code ...

// Register Module Services
builder.Services.AddYourFeatureModule();

// Configure Wolverine - add module assembly
builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Discovery.IncludeAssembly(typeof(MIF.Modules.Todos.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(MIF.Modules.YourFeature.DependencyInjection).Assembly); // Add this
});

// Add Endpoints from Modules - add module assembly
var moduleAssemblies = new[] { 
    typeof(MIF.Modules.Todos.DependencyInjection).Assembly,
    typeof(MIF.Modules.YourFeature.DependencyInjection).Assembly  // Add this
};
builder.Services.AddEndpoints(moduleAssemblies);
```

#### 5. Register in MIF.WebUI (if needed)
```csharp
// src/MIF.WebUI/Program.cs
using MIF.Modules.YourFeature;

// ... other code ...

// Add modules
builder.Services.AddYourFeatureModule();

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Discovery.IncludeAssembly(typeof(MIF.Modules.Todos.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(MIF.Modules.YourFeature.DependencyInjection).Assembly); // Add this
});
```

### Creating API Endpoints

API endpoints are defined in the `Endpoints` folder and implement the `IEndpoint` interface:

```csharp
// src/MIF.Modules.YourFeature/Endpoints/YourFeatureEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MIF.SharedKernel.Interfaces;
using MIF.SharedKernel.Application;
using Wolverine;

namespace MIF.Modules.YourFeature.Endpoints;

public class YourFeatureEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/yourfeature")
            .WithTags("YourFeature");

        // POST endpoint
        group.MapPost("/", async (CreateYourItemCommand command, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<int>>(command);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateYourItem")
        .WithOpenApi();

        // GET endpoint with pagination
        group.MapGet("/", async (int? pageNumber, int? pageSize, IMessageBus bus) =>
        {
            var query = new GetYourItemsQuery(pageNumber ?? 1, pageSize ?? 10);
            var result = await bus.InvokeAsync<Result<PaginatedList<YourItemDto>>>(query);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        })
        .WithName("GetYourItems")
        .WithOpenApi();

        // GET by ID endpoint
        group.MapGet("/{id}", async (int id, IMessageBus bus) =>
        {
            var query = new GetYourItemQuery(id);
            var result = await bus.InvokeAsync<Result<YourItemDto>>(query);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error);
        })
        .WithName("GetYourItemById")
        .WithOpenApi();

        // PUT endpoint
        group.MapPut("/{id}", async (int id, UpdateYourItemCommand command, IMessageBus bus) =>
        {
            if (id != command.Id)
                return Results.BadRequest("ID mismatch");

            var result = await bus.InvokeAsync<Result>(command);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(result.Error);
        })
        .WithName("UpdateYourItem")
        .WithOpenApi();

        // DELETE endpoint
        group.MapDelete("/{id}", async (int id, IMessageBus bus) =>
        {
            var command = new DeleteYourItemCommand(id);
            var result = await bus.InvokeAsync<Result>(command);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.NotFound(result.Error);
        })
        .WithName("DeleteYourItem")
        .WithOpenApi();
    }
}
```

**Key Points:**
- Use `MapGroup()` to group related endpoints under a common path
- Use `WithTags()` for OpenAPI documentation grouping
- Use `WithName()` to give endpoints unique names
- Use `WithOpenApi()` to include in OpenAPI documentation
- Always return appropriate HTTP status codes based on `Result.IsSuccess`

## 🧪 Testing

### Running Unit Tests
```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/MIF.UnitTests/MIF.UnitTests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests

Use the InMemory EF Core provider for testing:

```csharp
public class TodoRepositoryTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddTodoItem()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new TodoRepository(context);
        var todo = new TodoItem { Title = "Test" };

        // Act
        var result = await repository.AddAsync(todo, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }
}
```

## 🎨 UI Development with MudBlazor

### Common Components

**Table**:
```razor
<MudTable Items="@items" Hover="true">
    <HeaderContent>
        <MudTh>Name</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
    </RowTemplate>
</MudTable>
```

**Form**:
```razor
<MudTextField @bind-Value="model.Name" Label="Name" />
<MudButton OnClick="Submit" Color="Color.Primary">Submit</MudButton>
```

**Dialog**:
```razor
<MudDialog>
    <DialogContent>
        <MudText>Are you sure?</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Confirm">OK</MudButton>
    </DialogActions>
</MudDialog>
```

[MudBlazor Documentation](https://mudblazor.com/)

## 🔍 Architecture Patterns

### Vertical Slice Architecture

**Why Vertical Slices?**
- High cohesion: All related code in one module
- Low coupling: Modules only depend on SharedKernel
- Easy to scale: Add/remove features independently
- Team scalability: Different teams can own different modules

**Module Structure**:
```
MIF.Modules.FeatureName/
├── Domain/              # Feature-specific entities
├── Application/         # Commands, queries, handlers
├── Infrastructure/      # Repository implementations
└── DependencyInjection.cs  # Module registration
```

### CQRS with Wolverine

**Why CQRS?**
- Separates read and write operations
- Optimizes each operation independently
- Better scalability and maintainability

**How Wolverine Works**:
1. Define a command/query as a record
2. Create a handler class with a `Handle` method
3. Wolverine automatically discovers and wires handlers
4. Use `IMessageBus.InvokeAsync()` to execute

### Repository Pattern

**Benefits**:
- Abstraction over data access
- Easy to mock for testing
- Centralized data access logic per module

**Implementation**:
- Each module defines its own repository interface
- Interface extends `IRepository<T>` from SharedKernel
- Repository implementation uses injected `DbContext`

### Validation Strategy

- Use **FluentValidation** for business rules
- Validators are automatically discovered by Wolverine
- Validation exceptions are thrown before handlers execute
- Handle `FluentValidation.ValidationException` in UI

### Object Mapping Strategy

**Why Map Objects?**
- Separate domain entities from API contracts (DTOs)
- Prevent exposing internal entity structure
- Shape data specifically for API/UI needs

**Mapping Approaches:**

1. **EF Core Projection** (Best for read queries):
```csharp
// Map at database level - only fetches needed columns
var query = _todoItems
    .AsNoTracking()
    .Select(t => new TodoItemDto
    {
        Id = t.Id,
        Title = t.Title,
        IsCompleted = t.IsCompleted
    });
```

2. **Mapster** (Best for in-memory mapping):
```csharp
using Mapster;

// Simple mapping - zero configuration
var dto = entity.Adapt<TodoItemDto>();
var dtos = entities.Adapt<List<TodoItemDto>>();

// Complex mapping with custom logic
TypeAdapterConfig<Order, OrderDto>
    .NewConfig()
    .Map(dest => dest.Total, src => src.Items.Sum(i => i.Price * i.Quantity));
```

**When to use each:**
- ✅ Use **EF Projection** for paginated queries (like GetTodos)
- ✅ Use **Mapster** for commands, complex transformations, non-database sources
- ✅ Mapster works by convention (matching property names) - no config needed for simple cases

### Result Pattern for Error Handling

**Why Result Pattern?**
- Explicit error handling without exceptions
- Predictable control flow
- Better testability
- Standardized error responses

**How It Works**:
```csharp
// Return Result<T> for operations that return a value
public async Task<Result<int>> AddAsync(TodoItem entity, CancellationToken cancellationToken)
{
    try
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(entity.Id);
    }
    catch (DbUpdateException ex)
    {
        return Result.Failure<int>(new Error("Database.SaveFailed", ex.Message));
    }
}

// Return Result (non-generic) for operations without a return value
public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
{
    var result = await GetByIdAsync(id, cancellationToken);
    if (result.IsFailure)
        return Result.Failure(result.Error);
    
    _dbSet.Remove(result.Value);
    await _context.SaveChangesAsync(cancellationToken);
    return Result.Success();
}
```

**Error Types** (from `SharedKernel.Application.Error`):
- `Error.NotFound(entity, key)` - Entity not found
- `Error.Validation(message)` - Validation failed
- `Error.Conflict(message)` - Conflict detected
- `new Error(code, message)` - Custom error

**Handling Results**:
```csharp
var result = await repository.GetByIdAsync(id, cancellationToken);

if (result.IsSuccess)
{
    var todo = result.Value;
    // Use the value
}
else
{
    var error = result.Error;
    _logger.LogWarning("Operation failed: {Code} - {Message}", error.Code, error.Message);
}
```

### Observability & Logging

**Azure Monitor OpenTelemetry Distro**:
- **Single Package**: Comprehensive observability solution
- **Development**: Console logging with formatted timestamps (no Azure dependencies)
- **Production**: Full telemetry (logs, traces, metrics) to Azure Application Insights
- **Automatic**: Zero-configuration instrumentation for ASP.NET Core, HTTP, SQL
- **Correlation**: Automatic correlation between logs, traces, and metrics

**Environment-Specific Configuration**:
```csharp
// Program.cs
var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(connectionString))
{
    // Production: Use Azure Monitor distro
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        });
}
else if (builder.Environment.IsDevelopment())
{
    // Development: Console logging only
    builder.Logging.AddConsole();
}
```

**Structured Logging**:
All handlers include structured logging for observability:
```csharp
_logger.LogInformation("Creating new todo with title: {Title}", command.Title);
_logger.LogInformation("Todo created successfully with ID: {TodoId}", todo.Id);
```

**Benefits**:
- Correlation with distributed traces
- Easy filtering and searching
- Context propagation across operations

See [OPENTELEMETRY.md](OPENTELEMETRY.md) for detailed logging configuration.

## 🐛 Troubleshooting

### Application won't start

**Issue**: Port 5096 already in use
```bash
# Kill the process using port 5096
lsof -ti:5096 | xargs kill -9
```

### Database issues

**Issue**: Database file locked or corrupted
```bash
# Delete the database and recreate
rm src/MIF.WebUI/financeapp.db
dotnet ef database update --project src/MIF.SharedKernel --startup-project src/MIF.WebUI
```

### Build errors

**Issue**: Package restore failed
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

### Wolverine handlers not found

**Issue**: Handlers not being discovered
- Ensure handler class is public
- Verify handler has a public `Handle` method
- Check that the assembly is included in Wolverine configuration:
  ```csharp
  builder.Host.UseWolverine(opts =>
  {
      opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
  });
  ```

### Logs not appearing

**Issue**: Can't find application logs

**Local Development**:
- Logs output to console/terminal in real-time
- Verify `ASPNETCORE_ENVIRONMENT=Development`
- Check IDE console/output window
- Save to file: `dotnet run > app.log 2>&1`

**Test/Staging/Production**:
- Open Azure Portal → Application Insights → Logs
- Use KQL to query: `traces | where timestamp > ago(1h)`
- Check Live Metrics for real-time data
- Verify connection string is configured in appsettings or environment variable

## 📚 Useful Commands

```bash
# Build solution
dotnet build

# Run application
dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj

# Run in specific environment
ASPNETCORE_ENVIRONMENT=Staging dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj

# Run tests
dotnet test

# Add package
dotnet add package PackageName

# Remove package
dotnet remove package PackageName

# Format code
dotnet format

# List installed packages
dotnet list package

# Clean build outputs
dotnet clean

# View logs (Development - real-time console)
dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj

# Save logs to file for analysis
dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj > app.log 2>&1

# Filter logs
dotnet run --project src/MIF.WebUI/MIF.WebUI.csproj | grep "error:"
```

## 📖 Additional Resources

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Wolverine Documentation](https://wolverine.netlify.app/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [MudBlazor Components](https://mudblazor.com/components)
- [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [OpenTelemetry Configuration](OPENTELEMETRY.md) - Logging, tracing, and metrics setup

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make your changes following the architecture patterns
3. Write tests for new functionality
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## 💡 Best Practices

✅ **DO**:
- Follow Vertical Slice Architecture principles
- Keep features self-contained in modules
- Use CQRS for clear separation of concerns
- Write unit tests for business logic
- Use FluentValidation for input validation
- Keep handlers focused and single-purpose
- **Use structured logging with template parameters**
- **Include correlation data in logs (TodoId, UserId, etc.)**
- **Use ILogger<T> for all logging in handlers**
- **Leverage Azure Monitor distro for zero-config observability**
- **Register new modules in Program.cs with AddXxxModule()**
- **Only reference SharedKernel from modules**
- **Return Result<T> or Result from all repository methods**
- **Check result.IsSuccess before accessing result.Value**
- **Propagate errors through handler layers using Result**
- **Use Error.NotFound, Error.Validation, etc. for standard errors**

❌ **DON'T**:
- Add dependencies between modules (only SharedKernel)
- Put business logic in UI components
- Use Entity Framework directly in UI
- Skip validation for user inputs
- Create god classes or handlers
- Mix read and write operations in a single handler
- **Use string interpolation in log messages**
- **Log sensitive data (passwords, tokens, etc.)**
- **Commit Application Insights connection strings to source control**
- **Mix Serilog with Azure Monitor distro (use one observability solution)**
- **Reference Infrastructure from modules (causes circular dependencies)**
- **Throw exceptions for business logic errors (use Result instead)**
- **Access result.Value without checking result.IsSuccess first**
- **Swallow errors - always propagate Result through the call stack**
- **Use try-catch for expected business failures (use Result pattern)**

## 🆘 Getting Help

- Check this onboarding guide first
- Review the [README.md](README.md) for architecture overview
- Search existing issues in the repository
- Ask the team on your communication channel

---

Happy coding! 🚀
