using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Wolverine;
using Wolverine.FluentValidation;
using MIF.SharedKernel.Data;
using MIF.SharedKernel.Extensions;
using MIF.SharedKernel.Authentication;
using MIF.Modules.Todos;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ===== OKTA AUTHENTICATION CONFIGURATION =====
// Load Okta configuration from appsettings.json and register it for dependency injection
// This allows the app to use Okta's OAuth2/OIDC for secure API authentication
var oktaSettings = builder.Configuration.GetSection(OktaSettings.SectionName).Get<OktaSettings>();
builder.Services.Configure<OktaSettings>(builder.Configuration.GetSection(OktaSettings.SectionName));

// Configure JWT Bearer Authentication with Okta
// This tells the API to expect JWT tokens in the Authorization header (Bearer token)
// and validates them against Okta's authentication servers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority: The Okta URL that issues and validates tokens
        // Example: https://dev-12345.okta.com/oauth2/default
        options.Authority = oktaSettings?.Authority;
        
        // Audience: The intended recipient of the token (usually your API identifier)
        // This ensures tokens are meant for THIS API, not another service
        options.Audience = oktaSettings?.Audience;
        
        // RequireHttpsMetadata: Ensures token validation metadata is fetched over HTTPS
        // Set to true in production for security. Can be false for local dev without SSL.
        options.RequireHttpsMetadata = true;

        // Token validation rules - these determine what makes a token valid
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ValidateIssuer: Verify the token was issued by our trusted Okta server
            ValidateIssuer = true,
            
            // ValidateAudience: Verify the token is intended for this API
            ValidateAudience = true,
            
            // ValidateLifetime: Check that the token hasn't expired
            ValidateLifetime = true,
            
            // ValidateIssuerSigningKey: Verify the token's cryptographic signature
            // This ensures the token hasn't been tampered with
            ValidateIssuerSigningKey = true,
            
            // ClockSkew: Tolerance for token expiration time differences
            // Allows 5 minutes of leeway to account for clock differences between servers
            ClockSkew = TimeSpan.FromMinutes(5),
            
            // NameClaimType: Which JWT claim represents the user's name/identifier
            // 'sub' (subject) is the standard OAuth2 unique user identifier
            NameClaimType = "sub",
            
            // RoleClaimType: Which JWT claim contains user roles/groups
            // If you assign users to groups in Okta, they'll appear here
            RoleClaimType = "groups"
        };

        // Event handlers for authentication lifecycle - useful for logging and debugging
        options.Events = new JwtBearerEvents
        {
            // Triggered when authentication fails (invalid token, expired, etc.)
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            },
            
            // Triggered when a token is successfully validated
            // Good place to log successful authentications or add custom claims
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated for user: {User}", 
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

// Add authorization services - required to use [Authorize] attributes on endpoints
builder.Services.AddAuthorization();

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// ===== AUTHENTICATION & AUTHORIZATION MIDDLEWARE =====
// IMPORTANT: Order matters! Authentication must come before Authorization

// UseAuthentication: Reads the JWT token from the request header and validates it
// Creates the User principal with claims if the token is valid
app.UseAuthentication();

// UseAuthorization: Checks if the authenticated user has permission to access the endpoint
// Works with [Authorize] attributes to enforce access control
app.UseAuthorization();

// Map Endpoints
app.MapEndpoints();

app.Run();
