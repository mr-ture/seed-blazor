using MIF.WebUI.Components;
using MIF.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;
using MIF.Modules.Todos;
using Wolverine;
using Wolverine.FluentValidation;
using MudBlazor.Services;
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

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    // Auto-discover handlers in the Todos module
    opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Register HttpClient for API calls
builder.Services.AddHttpClient("MIF.API", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Also register a default HttpClient for convenience
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("MIF.API"));

// Add modules
builder.Services.AddTodosModule();

// Register shared database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AppDbContext as DbContext for module repositories
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

var app = builder.Build();

// Create database if not exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
