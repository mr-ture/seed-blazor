using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace MIF.SharedKernel.Authentication;

/// <summary>
/// HTTP message handler that automatically adds the Okta access token to outgoing HTTP requests.
/// 
/// This is a DelegatingHandler that intercepts HTTP requests made from the Blazor WebUI to the API
/// and automatically attaches the user's JWT access token in the Authorization header.
/// 
/// Usage: Register this handler with HttpClient in Program.cs to enable automatic token injection
/// for API calls, eliminating the need to manually add tokens to every request.
/// </summary>
public class OktaAuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<OktaAuthorizationMessageHandler> _logger;

    public OktaAuthorizationMessageHandler(
        AuthenticationStateProvider authenticationStateProvider,
        ILogger<OktaAuthorizationMessageHandler> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Intercepts outgoing HTTP requests and adds the Bearer token to the Authorization header.
    /// This method is called automatically by HttpClient before sending each request.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve the current user's authentication state from the authentication provider
            // This contains information about whether the user is logged in and their claims
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // Only proceed if the user is authenticated (logged in via Okta)
            if (user.Identity?.IsAuthenticated == true)
            {
                // Extract the access token from the user's claims
                // The token is stored in claims after successful Okta authentication
                var accessToken = user.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Add the JWT token to the Authorization header using the Bearer scheme
                    // Format: "Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6..."
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    _logger.LogDebug("Added access token to request: {Url}", request.RequestUri);
                }
                else
                {
                    // User is logged in but somehow doesn't have a token - this shouldn't normally happen
                    _logger.LogWarning("User is authenticated but no access token found in claims");
                }
            }
            else
            {
                // User is not logged in - request will proceed without a token
                // The API will return 401 Unauthorized if the endpoint requires authentication
                _logger.LogDebug("User is not authenticated, skipping token attachment");
            }
        }
        catch (Exception ex)
        {
            // Log any errors but don't fail the request
            // The request will continue without a token, and the API will handle authorization
            _logger.LogError(ex, "Error attaching access token to HTTP request");
        }

        // Continue with the request (with or without the token attached)
        return await base.SendAsync(request, cancellationToken);
    }
}
