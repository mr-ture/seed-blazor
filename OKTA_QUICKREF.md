# Okta Authentication - Quick Reference

## What's Secured?

### Blazor WebUI
- **Protected Pages**: Pages with `[Authorize]` attribute
  - `/todos` - Todo list page (requires authentication)
- **Login Flow**: Users are automatically redirected to Okta login when accessing protected pages
- **Logout**: Available via the logout link in the navigation

### Web API
- **Protected Endpoints**: All endpoints under `/api/todos/*`
  - `POST /api/todos` - Create todo
  - `GET /api/todos` - Get todos (paginated)
  - `PUT /api/todos/{id}/toggle` - Toggle todo completion
- **Authentication**: Requires JWT Bearer token in `Authorization` header

## How It Works

### 🔄 Authentication Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                     OKTA AUTHENTICATION FLOW DIAGRAM                         │
└──────────────────────────────────────────────────────────────────────────────┘

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 PHASE 1: Initial Login (Authorization Code Flow + PKCE)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    User                WebUI (Blazor)           Okta            WebUI Server
     │                       │                    │                    │
     │  1. Navigate          │                    │                    │
     │  /todos               │                    │                    │
     ├──────────────────────>│                    │                    │
     │                       │                    │                    │
     │                       │ 2. Check Auth      │                    │
     │                       │    [Not Auth]      │                    │
     │                       │                    │                    │
     │  3. Redirect to Okta  │                    │                    │
     │  Login Page           │                    │                    │
     │<──────────────────────┤                    │                    │
     │                       │                    │                    │
     │  4. Enter Credentials │                    │                    │
     ├───────────────────────┼───────────────────>│                    │
     │                       │                    │                    │
     │                       │  5. Validate       │                    │
     │                       │     Credentials    │                    │
     │                       │                    │                    │
     │  6. Redirect to WebUI │                    │                    │
     │     with Auth Code    │                    │                    │
     │<──────────────────────┼────────────────────┤                    │
     │                       │                    │                    │
     │  7. Callback          │                    │                    │
     │  /account/signin      │                    │                    │
     ├──────────────────────>│                    │                    │
     │                       │                    │                    │
     │                       │  8. Exchange Code  │                    │
     │                       │     for Tokens     │                    │
     │                       ├───────────────────>│                    │
     │                       │                    │                    │
     │                       │  9. Tokens         │                    │
     │                       │  - Access Token    │                    │
     │                       │  - ID Token        │                    │
     │                       │  - Refresh Token   │                    │
     │                       │<───────────────────┤                    │
     │                       │                    │                    │
     │                       │ 10. Store Tokens   │                    │
     │                       │     in Secure      │                    │
     │                       │     HTTP-Only      │                    │
     │                       │     Cookie         │                    │
     │                       ├───────────────────────────────────────>│
     │                       │                    │                    │
     │  11. Redirect to      │                    │                    │
     │      /todos           │                    │                    │
     │<──────────────────────┤                    │                    │
     │                       │                    │                    │

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 PHASE 2: Authenticated API Call (JWT Bearer Token)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    User                WebUI (Blazor)                    MIF.API
     │                       │                              │
     │  1. View /todos       │                              │
     │     Page Loads        │                              │
     ├──────────────────────>│                              │
     │                       │                              │
     │                       │  2. Read Tokens              │
     │                       │     from Cookie              │
     │                       │                              │
     │                       │  3. GET /api/todos           │
     │                       │     Authorization:           │
     │                       │     Bearer <access_token>    │
     │                       ├─────────────────────────────>│
     │                       │                              │
     │                       │                              │ 4. Validate JWT
     │                       │                              │    - Check Signature
     │                       │                              │    - Verify Issuer (Okta)
     │                       │                              │    - Verify Audience
     │                       │                              │    - Check Expiration
     │                       │                              │
     │                       │  5. 200 OK                   │
     │                       │     Todo List Data           │
     │                       │<─────────────────────────────┤
     │                       │                              │
     │  6. Display Todos     │                              │
     │<──────────────────────┤                              │
     │                       │                              │

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 PHASE 3: Token Refresh (When Access Token Expires)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    User                WebUI                 Okta              MIF.API
     │                   │                     │                   │
     │  1. API Call      │                     │                   │
     ├──────────────────>│                     │                   │
     │                   │                     │                   │
     │                   │  2. GET /api/todos  │                   │
     │                   │     with EXPIRED    │                   │
     │                   │     access token    │                   │
     │                   ├────────────────────────────────────────>│
     │                   │                     │                   │
     │                   │  3. 401 Unauthorized│                   │
     │                   │<────────────────────────────────────────┤
     │                   │                     │                   │
     │                   │  4. Use Refresh     │                   │
     │                   │     Token to Get    │                   │
     │                   │     New Access      │                   │
     │                   ├────────────────────>│                   │
     │                   │                     │                   │
     │                   │  5. New Access Token│                   │
     │                   │<────────────────────┤                   │
     │                   │                     │                   │
     │                   │  6. Update Cookie   │                   │
     │                   │     with New Token  │                   │
     │                   │                     │                   │
     │                   │  7. Retry API Call  │                   │
     │                   │     with NEW token  │                   │
     │                   ├────────────────────────────────────────>│
     │                   │                     │                   │
     │                   │  8. 200 OK          │                   │
     │                   │<────────────────────────────────────────┤
     │                   │                     │                   │
     │  9. Success!      │                     │                   │
     │<──────────────────┤                     │                   │
     │                   │                     │                   │

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 KEY COMPONENTS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

 🔑 Access Token:  JWT token used to authenticate API requests (expires ~1 hour)
 🆔 ID Token:      Contains user identity information (name, email, etc.)
 🔄 Refresh Token: Long-lived token to get new access tokens (expires ~days/weeks)
 🍪 Cookie:        Secure, HTTP-only cookie storing all tokens
 🔐 PKCE:          Proof Key for Code Exchange (prevents code interception)
 🎫 Auth Code:     Temporary code exchanged for tokens (single-use)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### User Authentication Flow (WebUI)

1. User navigates to `/todos`
2. WebUI detects user is not authenticated
3. User is redirected to Okta login page
4. User enters credentials
5. Okta validates and redirects back with authorization code
6. WebUI exchanges code for tokens (access token, ID token, refresh token)
7. Tokens are stored in secure HTTP-only cookie
8. User can now access protected pages

### API Authentication Flow

1. WebUI makes request to API endpoint (e.g., `GET /api/todos`)
2. `OktaAuthorizationMessageHandler` automatically adds access token to request:
   ```
   Authorization: Bearer eyJhbGci...
   ```
3. API validates JWT token with Okta
4. If valid, request is processed
5. If invalid/expired, API returns 401 Unauthorized

## Using HttpClient in Blazor Components

The default `HttpClient` is pre-configured to automatically include the access token:

```csharp
@page "/example"
@inject HttpClient Http

<h3>Example Component</h3>

@code {
    private async Task CallProtectedApi()
    {
        // HttpClient automatically includes the Bearer token
        var response = await Http.GetAsync("/api/todos");
        
        if (response.IsSuccessStatusCode)
        {
            var todos = await response.Content.ReadFromJsonAsync<List<TodoDto>>();
            // Process todos...
        }
    }
}
```

## Testing Authentication Locally

### 1. Configure Okta (see OKTA_SETUP.md for details)

### 2. Update Configuration Files

**API**: [src/MIF.API/appsettings.Development.json](src/MIF.API/appsettings.Development.json)
```json
{
  "Okta": {
    "Domain": "dev-12345.okta.com",
    "ClientId": "0oa...",
    "ClientSecret": "...",
    "AuthorizationServerId": "default",
    "Audience": "api://default"
  }
}
```

**WebUI**: [src/MIF.WebUI/appsettings.Development.json](src/MIF.WebUI/appsettings.Development.json)
```json
{
  "ApiBaseUrl": "https://localhost:7001",
  "Okta": {
    "Domain": "dev-12345.okta.com",
    "ClientId": "0oa...",
    "ClientSecret": "...",
    "AuthorizationServerId": "default",
    "Audience": "api://default",
    "Scopes": ["openid", "profile", "email", "offline_access"]
  }
}
```

### 3. Run Both Projects

Terminal 1 (API):
```bash
cd src/MIF.API
dotnet run
```

Terminal 2 (WebUI):
```bash
cd src/MIF.WebUI
dotnet run
```

### 4. Test

1. Open browser to `https://localhost:5001/todos`
2. Login with Okta credentials
3. View todos (calls protected API)

## Key Classes

### Configuration
- **[OktaSettings.cs](src/MIF.SharedKernel/Authentication/OktaSettings.cs)**: Configuration model for Okta settings

### Token Management
- **[IOktaTokenService.cs](src/MIF.SharedKernel/Authentication/IOktaTokenService.cs)**: Interface for token operations
- **[OktaTokenService.cs](src/MIF.SharedKernel/Authentication/OktaTokenService.cs)**: Token refresh and validation

### HTTP Client
- **[OktaAuthorizationMessageHandler.cs](src/MIF.SharedKernel/Authentication/OktaAuthorizationMessageHandler.cs)**: Automatically adds Bearer tokens to API calls

### Setup
- **[MIF.API/Program.cs](src/MIF.API/Program.cs)**: JWT Bearer authentication configuration
- **[MIF.WebUI/Program.cs](src/MIF.WebUI/Program.cs)**: OIDC authentication configuration

### Endpoints
- **[TodoEndpoints.cs](src/MIF.Modules.Todos/Endpoints/TodoEndpoints.cs)**: Protected API endpoints

## Common Scenarios

### Check if User is Authenticated (Blazor)

```csharp
@inject AuthenticationStateProvider AuthenticationStateProvider

@code {
    private async Task CheckAuth()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var userName = user.Identity.Name;
            var email = user.FindFirst("email")?.Value;
            // User is authenticated
        }
    }
}
```

### Get User Claims

```csharp
@code {
    private async Task GetUserInfo()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        var name = user.FindFirst("name")?.Value;
        var email = user.FindFirst("email")?.Value;
        var sub = user.FindFirst("sub")?.Value; // Unique user ID
    }
}
```

### Manual API Call with Token

If you need to manually add the token (not using the default HttpClient):

```csharp
var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
var accessToken = authState.User.FindFirst("access_token")?.Value;

var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

var response = await httpClient.SendAsync(request);
```

## Debugging Tips

### Enable Authentication Logging

Add to appsettings.Development.json:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

### View JWT Token Contents

Use [jwt.io](https://jwt.io) to decode and inspect JWT tokens:
1. Get token from browser dev tools (Application > Cookies)
2. Paste into jwt.io
3. View claims and expiration

### Test API Directly with Postman/curl

```bash
# Get token from Okta (simplified example)
curl -X POST https://your-domain.okta.com/oauth2/default/v1/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=YOUR_CLIENT_ID" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=api://default"

# Use token to call API
curl -X GET https://localhost:7001/api/todos \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Security Best Practices Implemented

✅ **PKCE**: Protects against authorization code interception  
✅ **Secure Cookies**: HTTP-only, SameSite=Lax, Secure flag  
✅ **Token Validation**: Comprehensive JWT validation (issuer, audience, signature, expiration)  
✅ **Refresh Tokens**: Automatic token refresh to maintain sessions  
✅ **HTTPS**: Required for production  
✅ **Authorization**: Endpoints protected with `RequireAuthorization()`  
✅ **Clock Skew**: 5-minute tolerance for token expiration  
✅ **Logging**: Authentication events logged for debugging and auditing  

## Next Steps

- [ ] Configure production Okta application
- [ ] Set up user groups/roles in Okta for role-based authorization
- [ ] Implement logout functionality in UI
- [ ] Add user profile page
- [ ] Configure token refresh behavior
- [ ] Set up monitoring and alerting for auth failures
- [ ] Implement rate limiting
- [ ] Configure CORS for cross-origin requests (if needed)

## Additional Resources

- Full setup guide: [OKTA_SETUP.md](OKTA_SETUP.md)
- Okta docs: https://developer.okta.com/docs/
- ASP.NET Core auth: https://learn.microsoft.com/en-us/aspnet/core/security/
