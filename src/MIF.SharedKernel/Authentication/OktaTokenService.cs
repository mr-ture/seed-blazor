using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;

namespace MIF.SharedKernel.Authentication;

/// <summary>
/// Implementation of Okta token service for managing JWT tokens and their lifecycle.
/// 
/// This service handles:
/// 1. Refreshing expired access tokens using refresh tokens (to keep users logged in)
/// 2. Validating if an access token is still valid before use
/// 
/// Tokens expire after a certain time for security. Instead of forcing users to log in again,
/// we can use a refresh token to get a new access token silently in the background.
/// </summary>
public class OktaTokenService : IOktaTokenService
{
    private readonly HttpClient _httpClient;
    private readonly OktaSettings _oktaSettings;
    private readonly ILogger<OktaTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public OktaTokenService(
        HttpClient httpClient,
        IOptions<OktaSettings> oktaSettings,
        ILogger<OktaTokenService> logger)
    {
        _httpClient = httpClient;
        _oktaSettings = oktaSettings.Value;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Refreshes an expired or about-to-expire access token using a refresh token.
    /// 
    /// Flow:
    /// 1. User's access token expires (typically after 1 hour)
    /// 2. Instead of requiring re-login, we use the refresh token to get a new access token
    /// 3. Okta validates the refresh token and issues a fresh access token
    /// 4. User stays logged in seamlessly without interruption
    /// 
    /// Returns: A new access token string, or null if refresh fails
    /// </summary>
    public async Task<string?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            // Construct the Okta token endpoint URL
            // Format: https://your-domain.okta.com/oauth2/default/v1/token
            var tokenEndpoint = $"https://{_oktaSettings.Domain}/oauth2/{_oktaSettings.AuthorizationServerId}/v1/token";

            // Prepare the token refresh request with required OAuth2 parameters
            var requestContent = new FormUrlEncodedContent(new[]
            {
                // Grant type tells Okta we're using a refresh token to get a new access token
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                // The refresh token obtained during initial login
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                // Application credentials to identify which app is requesting the token
                new KeyValuePair<string, string>("client_id", _oktaSettings.ClientId),
                new KeyValuePair<string, string>("client_secret", _oktaSettings.ClientSecret)
            });

            // Send the refresh request to Okta
            var response = await _httpClient.PostAsync(tokenEndpoint, requestContent);

            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                // Token refresh failed - possibly refresh token expired or was revoked
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to refresh token. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            // Parse the response to extract the new access token
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse?.AccessToken == null)
            {
                _logger.LogError("Token refresh succeeded but no access token in response");
                return null;
            }

            _logger.LogInformation("Successfully refreshed access token");
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while refreshing access token");
            return null;
        }
    }

    /// <summary>
    /// Validates if an access token is still valid and hasn't expired.
    /// 
    /// This method:
    /// 1. Decodes the JWT token without verifying the signature (just reading the expiration)
    /// 2. Checks if the token's expiration time is in the future
    /// 3. Adds a 5-minute buffer to handle clock skew between servers
    /// 
    /// Returns: True if token is valid and not expired, false otherwise
    /// </summary>
    public bool IsTokenValid(string accessToken)
    {
        try
        {
            // Basic validation - token must not be null or empty
            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            // Decode the JWT token to read its claims (including expiration time)
            // Note: This doesn't validate the signature, just reads the token contents
            var token = _tokenHandler.ReadJwtToken(accessToken);

            // Get the token's expiration time (ValidTo is when the token expires)
            var expirationTime = token.ValidTo;
            
            // Add a 5-minute buffer to account for clock differences between servers
            // If the token expires in less than 5 minutes, consider it invalid
            var now = DateTime.UtcNow.AddMinutes(5);

            // Token is valid if its expiration time is still in the future (after our buffered time)
            return expirationTime > now;
        }
        catch (Exception ex)
        {
            // If we can't read/parse the token, consider it invalid
            _logger.LogWarning(ex, "Failed to validate token");
            return false;
        }
    }

    /// <summary>
    /// Internal class to deserialize Okta's token endpoint response.
    /// 
    /// Okta returns JSON with token information when we request or refresh tokens.
    /// This class maps the JSON properties to C# properties.
    /// 
    /// Example response from Okta:
    /// {
    ///   "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
    ///   "token_type": "Bearer",
    ///   "expires_in": 3600,
    ///   "refresh_token": "v2.local.abc123..."
    /// }
    /// </summary>
    private class TokenResponse
    {
        /// <summary>
        /// The JWT access token used to authenticate API requests
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// The type of token (always "Bearer" for OAuth2)
        /// </summary>
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        /// <summary>
        /// How long the access token is valid (in seconds)
        /// Typically 3600 seconds (1 hour) for Okta
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// The refresh token used to obtain new access tokens when they expire
        /// Refresh tokens typically last much longer (days/weeks)
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
