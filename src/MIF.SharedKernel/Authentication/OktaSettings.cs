namespace MIF.SharedKernel.Authentication;

/// <summary>
/// Okta authentication configuration settings
/// </summary>
public class OktaSettings
{
    public const string SectionName = "Okta";

    /// <summary>
    /// Okta domain (e.g., your-domain.okta.com or your-domain.oktapreview.com)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// OAuth 2.0 Client ID from Okta application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth 2.0 Client Secret from Okta application
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Authorization Server ID (typically "default" for the org authorization server)
    /// </summary>
    public string AuthorizationServerId { get; set; } = "default";

    /// <summary>
    /// OAuth 2.0 scopes to request (e.g., openid, profile, email)
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// API audience for JWT token validation (typically api://default)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Full authority URL for OIDC (computed from Domain and AuthorizationServerId)
    /// </summary>
    public string Authority => $"https://{Domain}/oauth2/{AuthorizationServerId}";

    /// <summary>
    /// Issuer URL for JWT validation
    /// </summary>
    public string Issuer => $"https://{Domain}/oauth2/{AuthorizationServerId}";
}
