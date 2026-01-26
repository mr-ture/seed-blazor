# OpenTelemetry Configuration Guide

## Overview
The MIF application is configured with **Azure Monitor OpenTelemetry** (distro) for comprehensive observability through logging, tracing, and metrics:
- **Local Development**: Console logging (no Application Insights connection)
- **Test/Staging/Production**: Full telemetry to Azure Application Insights

## Azure Monitor OpenTelemetry Distro

The application uses the **Azure.Monitor.OpenTelemetry.AspNetCore** package, which is a distribution (distro) that automatically configures:
- **Logging**: ILogger integration with Application Insights
- **Tracing**: Distributed tracing with automatic instrumentation
- **Metrics**: Performance counters and custom metrics
- **Correlation**: Automatic correlation between logs, traces, and metrics

This single package replaces multiple OpenTelemetry packages and provides optimized integration with Azure Monitor.

## Components

The Azure Monitor distro automatically configures all observability components:

### 1. Tracing
Captures distributed traces across the application:
- **ASP.NET Core Instrumentation**: Automatically traces HTTP requests and responses
- **HTTP Client Instrumentation**: Traces outgoing HTTP calls
- **SQL Client Instrumentation**: Traces database queries
- **Custom Activity Sources**: Can add custom spans for business logic

### 2. Metrics
Collects performance metrics:
- **ASP.NET Core Metrics**: Request counts, duration, errors, active requests
- **HTTP Client Metrics**: Outbound request metrics
- **Runtime Metrics**: CPU, memory, GC stats
- **Custom Metrics**: Support for custom meters and instruments

### 3. Logging
Structured logging with Microsoft.Extensions.Logging:
- **Development**: Console output with formatted timestamps
- **Test/Staging/Production**: Azure Application Insights with full context
- **Correlation**: All logs automatically correlated with traces and metrics
- **Structured**: Template-based logging with property extraction

## Environment-Specific Configuration

### Local Development (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "SingleLine": false,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss ",
        "UseUtcTimestamp": false
      }
    }
  }
}
```

**Features:**
- Console logging with formatted timestamps
- No Application Insights connection required
- Structured logging with property extraction
- Scope information included for context
- Real-time output in terminal/IDE

### Test/Staging (appsettings.Staging.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://your-region.applicationinsights.azure.com/"
  }
}
```

**Features:**
- All telemetry sent to Azure Application Insights
- Distributed tracing with correlation
- Real-time metrics and logs
- Advanced querying with KQL (Kusto Query Language)

## Configuration

### Program.cs Implementation
```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Monitor OpenTelemetry for all environments
var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(connectionString))
{
    // Use Azure Monitor distro for comprehensive observability (logs, traces, metrics)
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        });
}
else if (builder.Environment.IsDevelopment())
{
    // Development without Application Insights: use console logging
    builder.Logging.AddConsole();
}
```

### appsettings.json (Base Configuration)
```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

### Environment Variables
Override configuration using environment variables:
- `ApplicationInsights__ConnectionString`: Azure Application Insights connection string
- `ASPNETCORE_ENVIRONMENT`: Set to "Development", "Staging", or "Production"

### Azure Monitor Options

The `UseAzureMonitor` method accepts additional options:

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = connectionString;
        
        // Optional: Customize sampling
        options.SamplingRatio = 0.1F; // Sample 10% of requests
        
        // Optional: Add custom resource attributes
        options.EnableLiveMetrics = true; // Enable Live Metrics
    });
```

**Available Options:**
- `ConnectionString`: Application Insights connection string (required)
- `Credential`: Azure credential for authentication (optional, uses default if not set)
- `SamplingRatio`: Sampling rate for telemetry (0.0 to 1.0, default 1.0)
- `EnableLiveMetrics`: Enable real-time Live Metrics stream (default true)
- `StorageDirectory`: Local storage for offline telemetry buffering

## Local Development Setup

### Console Logging
Logs are written to the console/terminal with formatted output:

Example log entry:
```
2026-01-22 15:30:45 info: MIF.Application.Features.Todos.Commands.CreateTodo.CreateTodoCommandHandler[0]
      Creating new todo with title: Buy groceries
2026-01-22 15:30:45 info: MIF.Application.Features.Todos.Commands.CreateTodo.CreateTodoCommandHandler[0]
      Todo created successfully with ID: 42
```

### Viewing Logs
```bash
# Run application and view logs in real-time
dotnet run --project src/MIF.WebUI

# Filter logs using grep
dotnet run --project src/MIF.WebUI | grep "TodoId"

# Save logs to file for later analysis
dotnet run --project src/MIF.WebUI > app.log 2>&1
```

### Structured Logging Format
Logs include:
- **Timestamp**: Local time with millisecond precision
- **Level**: info, warn, error, critical
- **Category**: Full type name of the logger
- **EventId**: Numeric identifier for the log event
- **Message**: Formatted message with structured properties
- **Scopes**: Request/operation context (when enabled)

## Azure Application Insights Setup

### 1. Create Application Insights Resource

Using Azure CLI:
```bash
# Create resource group
az group create --name mif-rg --location eastus

# Create Application Insights
az monitor app-insights component create \
  --app mif-app-insights \
  --location eastus \
  --resource-group mif-rg \
  --application-type web

# Get connection string
az monitor app-insights component show \
  --app mif-app-insights \
  --resource-group mif-rg \
  --query connectionString -o tsv
```

### 2. Configure Application

Update `appsettings.Staging.json` or `appsettings.Production.json`:
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-instrumentation-key;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
  }
}
```

Or use environment variable:
```bash
export ApplicationInsights__ConnectionString="InstrumentationKey=..."
```

### 3. Verify Telemetry

1. Navigate to Azure Portal → Application Insights → your resource
2. Check:
   - **Live Metrics**: Real-time monitoring
   - **Transaction Search**: Individual requests and traces
   - **Application Map**: Service dependencies
   - **Performance**: Request duration and failures
   - **Failures**: Exception tracking
   - **Logs**: Query logs with KQL

## Querying Logs

### Azure Application Insights (KQL)

```kusto
// Find all todo creation events
traces
| where message contains "Creating new todo"
| project timestamp, message, customDimensions

// Find errors in last hour
traces
| where severityLevel >= 3
| where timestamp > ago(1h)
| project timestamp, message, severityLevel

// Request duration analysis
requests
| summarize avg(duration), count() by name
| order by avg_duration desc

// Track specific todo operations
traces
| where customDimensions contains "TodoId"
| project timestamp, message, tostring(customDimensions.TodoId)
```

### Local Console Logs (grep/awk)

```bash
# Find all errors from saved log file
grep "error:" app.log

# Find warnings and errors
grep -E "warn:|error:" app.log

# Extract TodoIds
grep -oP 'TodoId: \K\d+' app.log | sort -u

# Count log entries by level
grep -oE "(info|warn|error|critical):" app.log | sort | uniq -c

# Filter by logger category
grep "CreateTodoCommandHandler" app.log
```

## Running with OpenTelemetry Collector (Optional)

**Note**: The Azure Monitor distro sends telemetry directly to Application Insights. An OpenTelemetry Collector is not required but can be used for advanced scenarios like:
- Local telemetry visualization with Jaeger
- Multi-cloud telemetry routing
- Custom processing pipelines
- Offline development/testing

### Using Docker Compose

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  otel-collector:
    image: otel/opentelemetry-collector:latest
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC receiver
      - "4318:4318"   # OTLP HTTP receiver
      - "13133:13133" # Health check

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686" # Jaeger UI
      - "14250:14250" # Jaeger gRPC

  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
```

### OpenTelemetry Collector Configuration

Create `otel-collector-config.yaml`:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:

exporters:
  logging:
    loglevel: debug
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, prometheus]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging]
```

### Start the Stack

```bash
docker-compose up -d
```

Access:
- **Jaeger UI**: http://localhost:16686
- **Prometheus**: http://localhost:9090

## Development Without Application Insights

The application works perfectly without an Application Insights connection string. When no connection string is configured:
1. **Logs**: Output to console with formatted timestamps
2. **Traces**: Not collected (no overhead)
3. **Metrics**: Not collected (no overhead)

This provides a lightweight development experience with zero Azure dependencies.

## Logging Examples

All handlers now include structured logging:

```csharp
_logger.LogInformation("Creating new todo with title: {Title}", command.Title);
_logger.LogInformation("Todo created successfully with ID: {TodoId}", entity.Id);
```

Benefits:
- Structured data for querying
- Automatic correlation with traces
- Context propagation across async operations

## Best Practices

1. **Use structured logging**: Always use template parameters instead of string interpolation
   ```csharp
   // Good
   _logger.LogInformation("User {UserId} created todo {TodoId}", userId, todoId);
   
   // Bad
   _logger.LogInformation($"User {userId} created todo {todoId}");
   ```

2. **Include correlation data**: Use consistent property names across logs
   - `TodoId`, `UserId`, `PageNumber`, etc.

3. **Set appropriate log levels**:
   - `Trace`: Very detailed debugging
   - `Debug`: Debugging information
   - `Information`: General flow
   - `Warning`: Unexpected but handled
   - `Error`: Errors and exceptions
   - `Critical`: Critical failures

4. **Environment-specific configuration**:
   - Use console logging for local development (no connection string)
   - Use Azure Application Insights for Test/Staging/Production
   - Never commit connection strings to source control

5. **Azure Monitor distro benefits**:
   - Automatic instrumentation for ASP.NET Core, HTTP, SQL
   - No manual trace/metric configuration needed
   - Optimized for Application Insights ingestion
   - Single package reduces dependency conflicts

## Deployment Checklist

### Local Development
- ✅ Azure Monitor distro configured in Program.cs
- ✅ Console logging enabled for development
- ✅ No Application Insights connection string (optional)
- ✅ appsettings.Development.json configured with console formatter

### Test/Staging/Production
- ✅ Application Insights resource created in Azure
- ✅ Connection string configured in appsettings.{Environment}.json or environment variable
- ✅ `UseAzureMonitor()` configured in Program.cs
- ✅ Environment variable `ASPNETCORE_ENVIRONMENT` set correctly
- ✅ Verify telemetry appears in Azure Portal (Live Metrics, Logs, Transaction Search)
- ✅ Test correlation between logs, traces, and metrics

## Troubleshooting

### Local Development

#### Logs not appearing in console
1. Verify console logging is configured in appsettings.Development.json
2. Check that no Application Insights connection string is set
3. Ensure `ASPNETCORE_ENVIRONMENT=Development`
4. Try running with `--verbosity detailed` flag
5. Check IDE console/output window settings

#### Too much log output
1. Increase log level to Warning for noisy components:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Information",
       "Microsoft": "Warning",
       "Microsoft.AspNetCore": "Warning",
       "Microsoft.EntityFrameworkCore": "Warning"
     }
   }
   ```
2. Use log filtering in appsettings
3. Redirect output to file: `dotnet run > app.log 2>&1`

### Test/Staging/Production

#### No telemetry in Application Insights
1. Verify connection string is correct
2. Check network connectivity to Azure
3. Ensure `ASPNETCORE_ENVIRONMENT` is not "Development"
4. Review application startup logs for exporter errors
5. Check Application Insights Live Metrics for real-time data

#### Missing traces
1. Ensure handlers are being invoked through Wolverine
2. Verify `AddSource("Wolverine")` is configured
3. Check trace context propagation in async code

#### High costs
1. Adjust sampling rate in Application Insights
2. Filter noisy logs with log level configuration
3. Use daily cap in Application Insights settings
4. Review and optimize query patterns

## Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable)
- [Application Insights Overview](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Serilog Documentation](https://serilog.net/)
- [KQL Query Language](https://learn.microsoft.com/azure/data-explorer/kusto/query/)

## Summary

The MIF application uses **Azure Monitor OpenTelemetry distro** for unified observability:
- **Single Package**: `Azure.Monitor.OpenTelemetry.AspNetCore` provides logs, traces, and metrics
- **Local Development**: Simple console logging with no Azure dependencies
- **Production**: Full telemetry to Application Insights with automatic correlation
- **Zero Configuration**: Automatic instrumentation for ASP.NET Core, HTTP, and SQL
- **Optimized**: Purpose-built for Azure Monitor ingestion and querying

This provides a streamlined, production-ready observability solution with minimal configuration and maintenance.
