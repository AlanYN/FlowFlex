# Third-Party Login API Documentation

## Overview

The third-party login API allows external systems to authenticate users and automatically register them in the FlowFlex system. This endpoint accepts an application code, tenant ID, and authorization token from the third-party system, then parses the token to extract user information and generates a local system token.

## Endpoint

```
POST /api/ow/users/third-party-login
```

## Request Format

### Headers
```
Content-Type: application/json
```

### Request Body
```json
{
  "appCode": "string",      // Application code from third-party system (required, max 100 chars)
  "tenantId": "string",     // Tenant ID from third-party system (required, max 100 chars)
  "authorizationToken": "string"  // JWT token from third-party system (required)
}
```

### Field Descriptions

- **appCode**: Identifies the third-party application making the request
- **tenantId**: Specifies the tenant context for the user
- **authorizationToken**: Valid JWT token containing user information (must include email claim)

## Response Format

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 7200,
    "user": {
      "id": 123,
      "email": "user@example.com",
      "username": "user@example.com",
      "emailVerified": true,
      "lastLoginDate": "2023-12-01T10:30:00Z",
      "status": "active",
      "createDate": "2023-12-01T10:30:00Z"
    },
    "appCode": "DEFAULT",
    "tenantId": "DEFAULT"
  }
}
```

### Error Response (400 Bad Request)
```json
{
  "success": false,
  "message": "Third-party login failed: Invalid authorization token",
  "errors": null
}
```

### Error Response (401 Unauthorized)
```json
{
  "success": false,
  "message": "Third-party login failed: Email not found in authorization token",
  "errors": null
}
```

## Authorization Token Requirements

The `authorizationToken` must be a valid JWT token that contains:

### Required Claims
- **email** (or ClaimTypes.Email): User's email address
- **exp**: Token expiration time
- **iss**: Token issuer
- **aud**: Token audience

### Optional Claims
- **username**: User's display name (fallback to email if not provided)
- **sub** or **NameIdentifier**: User ID from third-party system
- **tenantId**: Tenant context (will use request tenantId if not in token)

## Behavior

### New User Registration
When a user doesn't exist in the system:
1. Creates a new user account automatically
2. Sets `emailVerified` to `true` (trusting third-party verification)
3. Sets `status` to `"active"`
4. Uses the provided `tenantId` from the request
5. Sends a welcome email (non-blocking)

### Existing User Login
When a user already exists:
1. Updates the user's last login time
2. Ensures user status is `"active"`
3. Updates tenant ID if different from request
4. Maintains existing user preferences

### Token Management
- Generates a new system JWT token with local claims
- Revokes any existing tokens for the user (single session)
- Records token details for audit and session management

## Example Usage

### Frontend Implementation
```javascript
// Frontend request example (assuming base URL /api/)
const response = await fetch('/api/ow/users/third-party-login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    appCode: 'EXTERNAL_APP_001',
    tenantId: 'COMPANY_123',
    authorizationToken: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
  })
});

const result = await response.json();
if (result.success) {
  // Store the access token for subsequent requests
  localStorage.setItem('accessToken', result.data.accessToken);
  // Redirect to main application
  window.location.href = '/dashboard';
}
```

### cURL Example
```bash
curl -X POST "http://localhost:5173/api/ow/users/third-party-login" \
  -H "Content-Type: application/json" \
  -d '{
    "appCode": "EXTERNAL_APP_001",
    "tenantId": "COMPANY_123",
    "authorizationToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }'
```

## Security Considerations

1. **Token Validation**: The authorization token is fully validated including signature, expiration, issuer, and audience
2. **Tenant Isolation**: Users are properly isolated by tenant ID
3. **Session Management**: Only one active session per user (previous tokens are revoked)
4. **Email Verification**: Trusts third-party email verification
5. **Audit Trail**: All login attempts are logged with details

## Error Handling

Common error scenarios:
- Invalid or expired authorization token
- Missing email claim in token
- Token signature validation failure
- Database connection issues
- Email service failures (non-blocking)

All errors are logged with appropriate detail levels and return descriptive error messages to help with debugging. 