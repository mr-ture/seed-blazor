# MIF Architecture

## Overview
This project follows **Vertical Slice Architecture** to organize code by features rather than technical layers. Each feature (module) is self-contained with its own domain logic, application logic, and infrastructure, promoting high cohesion and low coupling.

## Key Architectural Patterns
- **Vertical Slice Architecture**: Code organized by features/modules, not technical layers.
- **CQRS (Command Query Responsibility Segregation)**: Operations split into **Commands** (writes) and **Queries** (reads) using **Wolverine**.
- **Repository Pattern**: Abstraction layer for data access within each module.
- **Shared Kernel**: Common abstractions and base classes shared across modules.

## Project Structure

### 1. SharedKernel (`MIF.SharedKernel`)
*Common Abstractions.*
- Contains shared interfaces, base classes, and utilities.
- **Dependencies**: None.
- **Key Components**: 
  - `IEntity`, `EntityBase`: Base entity abstractions
  - `IRepository<T>`: Generic repository interface
  - `Result<T>`: Result pattern for error handling
  - `PaginatedList<T>`: Pagination utility
  - `DomainEvent`: Base class for domain events

### 2. Modules (`MIF.Modules.*`)
*Feature Modules (Vertical Slices).*
- Each module is self-contained with Domain, Application, and Infrastructure.
- **Dependencies**: `MIF.SharedKernel` only.
- **Example: Todos Module (`MIF.Modules.Todos`)**:
  - `Domain/`: Entities specific to Todos (e.g., `TodoItem`)
  - `Application/`: Commands, Queries, DTOs, Validators, and Handlers
  - `Infrastructure/`: Repository implementations
  - `DependencyInjection.cs`: Module registration

### 3. Infrastructure (`MIF.Infrastructure`)
*Shared Infrastructure.*
- Contains shared infrastructure concerns.
- **Dependencies**: Feature modules (for entity configuration).
- **Key Components**:
  - **EF Core**: `AppDbContext` using **SQLite**
  - Shared database configuration

### 4. WebUI (`MIF.WebUI`)
*The Presentation.*
- The entry point for the user.
- **Dependencies**: `MIF.Infrastructure`, Feature Modules (e.g., `MIF.Modules.Todos`).
- **Key Components**:
  - **Blazor Server**: Interactive UI components
  - **MudBlazor**: Component library for styling
  - **Module Registration**: Registers all modules via `AddTodosModule()`

## Technologies Stack
- **Framework**: .NET 10.0
- **UI**: Blazor Server, MudBlazor
- **Database**: SQLite (via Entity Framework Core)
- **Mediator**: Wolverine
- **Validation**: FluentValidation
- **Observability**: Azure Monitor OpenTelemetry (distro) for logs, traces, and metrics
  - **Local Development**: Console logging (no Azure dependencies)
  - **Test/Staging/Production**: Full telemetry to Azure Application Insights
  - **Package**: Azure.Monitor.OpenTelemetry.AspNetCore 1.3.0
