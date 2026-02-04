using MIF.WebUI.Components;
using MIF.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;
using MIF.Modules.Todos;
using Wolverine;
using Wolverine.FluentValidation;
using MudBlazor.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Okta.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OktaDefaults.MvcAuthenticationScheme;
})
.AddCookie()
.AddOktaMvc(new OktaMvcOptions
{
    OktaDomain = builder.Configuration["Okta:OktaDomain"],
    ClientId = builder.Configuration["Okta:ClientId"],
    ClientSecret = builder.Configuration["Okta:ClientSecret"],
    AuthorizationServerId = builder.Configuration["Okta:AuthorizationServerId"],
    Scope = new List<string> { "openid", "profile", "email" }
});

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/auth/login", async (HttpContext httpContext, string returnUrl = "/") =>
{
    var authenticationProperties = new AuthenticationProperties
    {
        RedirectUri = returnUrl
    };

    await httpContext.ChallengeAsync(OktaDefaults.MvcAuthenticationScheme, authenticationProperties);
});

app.MapGet("/logout", async (HttpContext httpContext) =>
{
    var authenticationProperties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };

    await httpContext.SignOutAsync(OktaDefaults.MvcAuthenticationScheme, authenticationProperties);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
