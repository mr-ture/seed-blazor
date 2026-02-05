# Authentication Setup Guide

## Overview
Your application now uses **OpenID Connect (OIDC)** with **Okta** for authentication. The authentication layer has been completely refactored for better security, error handling, and maintainability.

## Architecture

### Authentication Flow
1. **Cookie Authentication** - Primary authentication scheme for maintaining user sessions
2. **OpenID Connect** - Challenge/sign-out scheme for Okta integration
3. **Minimal API Endpoints** - `/auth/login` and `/auth/logout` for authentication actions

### Key Components

#### Program.cs - Authentication Configuration
- **Cookie settings**: 8-hour expiration with sliding expiration
- **OIDC settings**: PKCE enabled, proper callback paths
- **Event handlers**: Error handling for authentication failures
- **Token validation**: Custom claims mapping (name, groups)

#### Authentication Endpoints
- `GET /auth/login?returnUrl={url}` - Initiates OIDC login flow
- `POST /auth/logout` - Signs out user from both cookie and OIDC
- `GET /signout-callback-oidc` - Handles post-logout redirect

#### Blazor Pages
- `/login` - Login landing page
- `/logout` - Logout confirmation page
- `RedirectToLogin.razor` - Component for unauthorized access redirects

## Okta Configuration Required

### Application Settings in Okta Dashboard

1. **Application Type**: Web Application
2. **Grant Types**: 
   - ✅ Authorization Code
   - ✅ Refresh Token (optional)

3. **Sign-in redirect URIs**:
   ```
   https://localhost:5001/signin-oidc
   http://localhost:5000/signin-oidc
   ```

4. **Sign-out redirect URIs**:
   ```
   https://localhost:5001/signout-callback-oidc
   https://localhost:5001/
   http://localhost:5000/signout-callback-oidc
   http://localhost:5000/
   ```

5. **Initiate login URI**: (Leave blank or use `https://localhost:5001/login`)

### appsettings.Development.json Structure
```json
{
  "Okta": {
    "OktaDomain": "https://your-domain.okta.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "AuthorizationServerId": "default"
  }
}
```

## Current Configuration Issue

**Problem**: Your client secret appears to be invalid or expired.

**Solution**:
1. Go to: https://integrator-5560650-admin.okta.com
2. Navigate to: **Applications** → **Applications**
3. Find your app: Client ID `0oaztfzjyvugLHkj4697`
4. Under **General** tab → **Client Credentials**
5. Click **Edit** → **Generate new client secret**
6. **Copy the secret immediately** (it won't be shown again)
7. Update `appsettings.Development.json` with the new secret

## Security Features

### Implemented
- ✅ PKCE (Proof Key for Code Exchange) enabled
- ✅ Secure cookies (HTTPS only, SameSite=Lax)
- ✅ Token validation with issuer check
- ✅ Claims mapping for user identity
- ✅ Sliding session expiration
- ✅ Proper error handling and redirects

### Best Practices
- Tokens are saved securely
- User info endpoint is called for additional claims
- Authentication state is validated before actions
- Proper sign-out from both local cookies and Okta

## Testing

### Test Login Flow
1. Navigate to `/login`
2. Click "Sign In with OKTA"
3. Authenticate with Okta credentials
4. Should redirect back to the return URL

### Test Logout Flow
1. When authenticated, POST to `/auth/logout`
2. Should clear local session
3. Should sign out from Okta
4. Should redirect to home page

### Test Protected Routes
1. Try accessing a protected page without authentication
2. Should redirect to `/login` with return URL
3. After login, should return to original page

## Troubleshooting

### "Invalid client secret" Error
- Generate a new client secret in Okta
- Update `appsettings.Development.json`
- Restart the application

### Redirect Loop
- Check that redirect URIs in Okta match exactly
- Ensure `/signin-oidc` is registered
- Verify `CallbackPath` setting matches

### Claims Not Available
- Ensure `GetClaimsFromUserInfoEndpoint = true`
- Check that required scopes are requested
- Verify user has necessary attributes in Okta profile

### Session Expires Too Quickly
- Adjust `ExpireTimeSpan` in Cookie options
- Enable/disable `SlidingExpiration` as needed
- Check Okta session policy settings

## Next Steps

1. **Update Client Secret**: Get new secret from Okta and update config
2. **Test Authentication**: Try the login flow
3. **Verify Redirect URIs**: Ensure Okta app configuration matches
4. **Add Authorization**: Implement role-based or policy-based authorization as needed
5. **Production Config**: Move secrets to Azure Key Vault or user secrets

## Additional Resources

- [Okta Documentation](https://developer.okta.com/docs/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [OpenID Connect Spec](https://openid.net/specs/openid-connect-core-1_0.html)
