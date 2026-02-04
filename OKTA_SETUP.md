# Okta Authentication Setup Guide

This guide will help you configure Okta authentication for both the MIF Web API and Blazor WebUI.

## Overview

The MIF project now uses Okta for authentication with:
- **Blazor WebUI**: OpenID Connect (OIDC) with Authorization Code Flow + PKCE
- **Web API**: JWT Bearer token authentication

## Prerequisites

1. An Okta developer account (free at [developer.okta.com](https://developer.okta.com))
2. .NET 10.0 SDK installed

## Step 1: Create Okta Applications

### 1.1 Create Web Application (for Blazor WebUI)

1. Log in to your Okta admin console
2. Navigate to **Applications** > **Applications**
3. Click **Create App Integration**
4. Select:
   - **Sign-in method**: OIDC - OpenID Connect
   - **Application type**: Web Application
5. Configure the application:
   - **App integration name**: MIF WebUI
   - **Grant type**: Authorization Code, Refresh Token
   - **Sign-in redirect URIs**: 
     - `https://localhost:5001/account/signin`
     - `http://localhost:5000/account/signin` (for development)
   - **Sign-out redirect URIs**:
     - `https://localhost:5001/account/signout`
     - `http://localhost:5000/account/signout`
   - **Controlled access**: Choose based on your needs (e.g., "Allow everyone in your organization to access")
6. Click **Save**
7. Note down:
   - **Client ID**
   - **Client Secret** (click "Edit" if not visible)

### 1.2 Create API Service (for Web API)

1. In Okta admin console, go to **Applications** > **Applications**
2. Click **Create App Integration**
3. Select:
   - **Sign-in method**: API Services
   - **Application type**: Service (Machine-to-Machine)
4. Configure:
   - **App integration name**: MIF API
5. Click **Save**
6. Note down:
   - **Client ID**
   - **Client Secret**

### 1.3 Configure Authorization Server

1. Navigate to **Security** > **API** > **Authorization Servers**
2. Select **default** authorization server (or create a custom one)
3. Note down:
   - **Issuer URI** (should be like `https://your-domain.okta.com/oauth2/default`)
   - **Audience** (typically `api://default`)

## Step 2: Configure Web API

Update [src/MIF.API/appsettings.Development.json](src/MIF.API/appsettings.Development.json):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  },
  "Okta": {
    "Domain": "your-domain.okta.com",
    "ClientId": "your-api-client-id",
    "ClientSecret": "your-api-client-secret",
    "AuthorizationServerId": "default",
    "Audience": "api://default"
  }
}
```

Replace:
- `your-domain.okta.com` with your Okta domain (e.g., `dev-12345.okta.com`)
- `your-api-client-id` with the Client ID from Step 1.2
- `your-api-client-secret` with the Client Secret from Step 1.2

## Step 3: Configure Blazor WebUI

Update [src/MIF.WebUI/appsettings.Development.json](src/MIF.WebUI/appsettings.Development.json):

```json
{
  "Okta": {
    "Domain": "your-domain.okta.com",
    "ClientId": "your-webui-client-id",
    "ClientSecret": "your-webui-client-secret",
    "AuthorizationServerId": "default",
    "Scopes": ["openid", "profile", "email", "offline_access"],
    "Audience": "api://default"
  }
}
```

Replace:
- `your-domain.okta.com` with your Okta domain
- `your-webui-client-id` with the Client ID from Step 1.1
- `your-webui-client-secret` with the Client Secret from Step 1.1

## Step 4: Test the Integration

### 4.1 Run the Web API

```bash
cd src/MIF.API
dotnet run
```

The API will start on `https://localhost:7001` (or similar).

### 4.2 Run the Blazor WebUI

```bash
cd src/MIF.WebUI
dotnet run
```

The WebUI will start on `https://localhost:5001` (or similar).

### 4.3 Test Authentication Flow

1. Navigate to `https://localhost:5001/todos` in your browser
2. You should be redirected to the Okta login page
3. Log in with your Okta credentials
4. After successful login, you'll be redirected back to the Todos page
5. The page should display the todo list (requires authenticated user)

## Architecture Details

### Web API (JWT Bearer)

- **Authentication**: JWT Bearer tokens
- **Token Validation**: 
  - Validates issuer (Okta)
  - Validates audience (`api://default`)
  - Validates signature using Okta's public keys
  - 5-minute clock skew tolerance
- **Protected Endpoints**: All `/api/todos/*` endpoints require authentication

### Blazor WebUI (OIDC)

- **Authentication Flow**: Authorization Code Flow with PKCE (Proof Key for Code Exchange)
- **Token Storage**: Tokens stored in secure HTTP-only cookies
- **Token Refresh**: Automatic token refresh using refresh tokens
- **Session Management**: 8-hour session with sliding expiration
- **Protected Pages**: Pages with `[Authorize]` attribute (e.g., `/todos`)

### Security Features

1. **PKCE (Proof Key for Code Exchange)**: Prevents authorization code interception attacks
2. **Secure Cookies**: 
   - `SameSite=Lax` to prevent CSRF attacks
   - `SecurePolicy=Always` to ensure cookies only sent over HTTPS
3. **Token Validation**: Comprehensive validation of JWT tokens
4. **Refresh Tokens**: Automatic token refresh to maintain user sessions
5. **HTTPS Enforcement**: All production traffic requires HTTPS

## Common Issues & Troubleshooting

### Issue: "Authority is not configured"

**Solution**: Verify that your Okta domain is correctly configured in appsettings.

### Issue: "Redirect URI mismatch"

**Solution**: Ensure the redirect URIs in your Okta application match exactly what's configured:
- WebUI: `https://localhost:5001/account/signin`
- Check your `launchSettings.json` for the actual ports being used

### Issue: "Invalid audience"

**Solution**: Make sure the `Audience` in your API configuration matches the audience claim in the JWT token issued by Okta.

### Issue: API returns 401 Unauthorized

**Solution**: 
1. Verify the API is receiving a valid JWT token in the `Authorization: Bearer <token>` header
2. Check that the token's issuer and audience match your configuration
3. Enable debug logging: `"Microsoft.AspNetCore.Authentication": "Debug"`

## Production Considerations

Before deploying to production:

1. **Update Redirect URIs**: Add production URLs to your Okta application
2. **Enable HTTPS**: Set `RequireHttpsMetadata = true` in API authentication
3. **Secure Secrets**: Use Azure Key Vault, AWS Secrets Manager, or environment variables for client secrets
4. **Configure CORS**: If API and WebUI are on different domains
5. **Enable Rate Limiting**: Protect against brute force attacks
6. **Configure Token Lifetimes**: Adjust session duration based on security requirements
7. **Set Up Monitoring**: Log authentication events and failures

## Additional Resources

- [Okta Developer Documentation](https://developer.okta.com/docs/)
- [ASP.NET Core Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [OpenID Connect Specification](https://openid.net/connect/)
- [JWT.io - JWT Debugger](https://jwt.io/)

## Support

For issues specific to this implementation, please refer to:
- [MIF.SharedKernel/Authentication/OktaSettings.cs](src/MIF.SharedKernel/Authentication/OktaSettings.cs) - Configuration model
- [MIF.SharedKernel/Authentication/OktaTokenService.cs](src/MIF.SharedKernel/Authentication/OktaTokenService.cs) - Token management
- [MIF.API/Program.cs](src/MIF.API/Program.cs) - API authentication setup
- [MIF.WebUI/Program.cs](src/MIF.WebUI/Program.cs) - WebUI authentication setup
