# Authentication Setup Guide

## Overview
The UI projects use **OpenID Connect (OIDC)** with **Okta** for authentication.
This is configured directly in `Program.cs` using cookie auth for sessions and OIDC for challenges.

## Where Authentication Is Enabled

- **MIF.Web** (Blazor Server)
- **MIF.WebUI** (Blazor Server)
- **MIF.API** does not configure authentication in the current codebase

## Current Authentication Flow
1. **Cookie Authentication** is the default scheme for signed-in sessions.
2. **OpenID Connect** is the default challenge scheme (redirects to Okta).
3. Authorization is enabled in **MIF.WebUI** (`AddAuthorization`), but there are
   no custom login/logout endpoints in the current code.

## Key Configuration (Program.cs)

- Cookie + Okta OIDC are registered in both UI projects.
- Okta settings are read from configuration under the `Okta` section.

### Files
- [src/MIF.Web/Program.cs](src/MIF.Web/Program.cs)
- [src/MIF.WebUI/Program.cs](src/MIF.WebUI/Program.cs)

## Okta Configuration Required

### Application Settings in Okta Dashboard

1. **Application Type**: Web Application
2. **Grant Types**: 
   - ✅ Authorization Code
   - ✅ Refresh Token (optional)

3. **Sign-in redirect URIs** (for each UI app you run):
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

5. **Initiate login URI**: optional (leave blank or set a UI route if you add one)

### appsettings.json / appsettings.Development.json Structure
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

## Where to Put Secrets
Store Okta secrets in:
- `src/MIF.Web/appsettings.json` or user-secrets
- `src/MIF.WebUI/appsettings.json` or user-secrets

Avoid committing real secrets to source control in production.

## What Is Not Present in Current Code
- No custom `/auth/login` or `/auth/logout` endpoints
- No custom OIDC event handlers or claims mapping
- No explicit cookie options (defaults are used)

## Testing

### Test Login Flow
1. Navigate to a route that requires authorization.
2. The app should redirect to Okta for sign-in.
3. After login, you should return to the original page.

### Test Logout Flow
Sign-out is not explicitly wired in code. If you need it, add a logout endpoint or UI action
and call `SignOutAsync` for both cookie and OIDC schemes.

## Troubleshooting

### "Invalid client secret" Error
- Generate a new client secret in Okta
- Update the appropriate appsettings file or user-secrets
- Restart the application

### Redirect Loop
- Check that redirect URIs in Okta match exactly
- Ensure `/signin-oidc` is registered

### Claims Not Available
- Add explicit claims mapping and scopes in `OktaMvcOptions` if needed
- Verify user has required attributes in Okta profile

### Session Expires Too Quickly
- Configure cookie options via `AddCookie(...)` if needed
- Check Okta session policy settings

## Next Steps

1. **Confirm Okta settings** in your appsettings or user-secrets
2. **Test authentication** by hitting a protected route
3. **Add logout support** if required by your UX
4. **Add policies/roles** via ASP.NET Core authorization if needed

## Additional Resources

- [Okta Documentation](https://developer.okta.com/docs/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [OpenID Connect Spec](https://openid.net/specs/openid-connect-core-1_0.html)
