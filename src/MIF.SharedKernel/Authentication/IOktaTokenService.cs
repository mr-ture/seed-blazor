namespace MIF.SharedKernel.Authentication;

/// <summary>
/// Service for managing Okta tokens and token refresh
/// </summary>
public interface IOktaTokenService
{
    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>A new access token</returns>
    Task<string?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Validates if an access token is still valid (not expired)
    /// </summary>
    /// <param name="accessToken">The access token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool IsTokenValid(string accessToken);
}
