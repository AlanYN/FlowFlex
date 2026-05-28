# Workflow API Integration Guide

## Table of Contents

- [Environment Configuration](#environment-configuration)
- [Key Prerequisites](#key-prerequisites)
  - [1. Tenant Identifier (X-Tenant-Id)](#1-tenant-identifier-x-tenant-id)
  - [2. Application Identifier (X-App-Code)](#2-application-identifier-x-app-code)
  - [3. Application ID (X-App-Id)](#3-application-id-x-app-id)
  - [4. Authentication Token (Authorization)](#4-authentication-token-authorization)
- [Required Headers for All API Requests](#required-headers-for-all-api-requests)
- [Authentication Flow](#authentication-flow)
  - [Obtain Access Token](#obtain-access-token)
  - [Refresh Access Token](#refresh-access-token)
  - [Third-Party Login](#third-party-login)
  - [Logout](#logout)
- [Client Credentials Authentication (App-Level / AI Agent)](#client-credentials-authentication-app-level--ai-agent)
- [API Call Examples](#api-call-examples)
- [API Versioning](#api-versioning)
- [Error Handling](#error-handling)
- [Troubleshooting](#troubleshooting)
- [Related Documentation](#related-documentation)

---

## Environment Configuration

| Environment | Base URL                            | Description                 |
| ----------- | ----------------------------------- | --------------------------- |
| Development | `https://workflow-dev.item.pub`     | For development and testing |
| Staging     | `https://workflow-staging.item.com` | For UAT acceptance testing  |
| Production  | `https://workflow.item.com`         | Production environment      |

> **Note**: Please confirm the actual API address with your system administrator. All endpoints use `/api/` as the unified prefix.

---

## Key Prerequisites

### 1. Tenant Identifier (X-Tenant-Id)

Workflow is a multi-tenant system. Each request must carry a tenant identifier to ensure data isolation.

> ⚠️ **Important**: The system has both "Tenant ID" and "Tenant Code" concepts. The `X-Tenant-Id` header requires the **Tenant ID (numeric)**, not the Tenant Code.

**Field Format:**

| Property   | Description                                                     |
| ---------- | --------------------------------------------------------------- |
| Type       | `string` (numeric tenant ID as string)                          |
| Format     | **Numeric string** representing the system-assigned tenant ID   |
| Max Length | 32 characters                                                   |
| Default    | `default` (dev/test only; production requires a real tenant ID) |
| Nullable   | ❌ Cannot be empty string or whitespace; throws exception       |

**Example Values:** `1000`, `1005`, `1012`

> ⚠️ **Warning**: `TenantId` cannot be null, empty string, or whitespace. The system will throw an `ArgumentException` with the message "TenantId is required for tenant isolation".

**How to obtain:**

- Assigned by the system administrator
- Returned in the `tenantId` field of the login response
- Can also be parsed from JWT Token Claims

**How to pass (in priority order):**

1. Request header `X-Tenant-Id` (recommended)
2. Request header `TenantId`
3. Query parameter `tenantId` or `tenant_id`
4. `tenantId` Claim in JWT Token

### 2. Application Identifier (X-App-Code)

Used to identify the calling application for application-level data isolation.

**Field Format:**

| Property   | Description                                      |
| ---------- | ------------------------------------------------ |
| Type       | `string`                                         |
| Max Length | 32 characters                                    |
| Format     | String representing the application identity     |
| Default    | `default` (used automatically when not provided) |
| Nullable   | Optional; system uses `default` if not provided  |

**Example Values:** `default`, `WEB`, `MOBILE`, `ADMIN`

> 💡 **Note**: For most integration scenarios, passing `default` is sufficient. The system can also infer AppCode from the request path (e.g., `/api/mobile/` -> `MOBILE`, `/api/admin/` -> `ADMIN`), but explicit passing is recommended for accuracy.

**How to obtain:**

- Assigned by the system administrator
- Obtained after login via IDM (Identity Management System)
- Common values: `default`, `WEB`, `MOBILE`, `ADMIN`

**How to pass (in priority order):**

1. Request header `X-App-Code` (recommended)
2. Request header `AppCode`
3. Query parameter `appCode` or `app_code`
4. `appCode` Claim in JWT Token
5. Defaults to `default` if not provided

### 3. Application ID (X-App-Id)

Used to identify the application system ID assigned by the IDM system.

**Field Format:**

| Property | Description                                                     |
| -------- | --------------------------------------------------------------- |
| Type     | `string` (numeric application ID as string)                     |
| Format   | **Numeric string** representing the IDM-assigned application ID |
| Nullable | ⚠️ Recommended                                                  |

**Example Values:** `5`

> 💡 **Note**: `X-App-Id` is the unique identifier assigned to the current application by the IDM identity management system, used for IDM public API authentication.

### 4. Authentication Token (Authorization)

The system uses JWT Bearer Token for authentication.

**Format:**

```
Authorization: Bearer <your_access_token>
```

---

## Required Headers for All API Requests

| Header          | Required              | Description                           | Example                          |
| --------------- | --------------------- | ------------------------------------- | -------------------------------- |
| `Authorization` | ✅ Yes (except login) | JWT Bearer Token                      | `Bearer eyJhbGciOiJSUzI1NiIs...` |
| `X-Tenant-Id`   | ✅ Yes                | Tenant ID (numeric)                   | `1000`                           |
| `X-App-Code`    | ⚠️ Recommended        | Application identifier                | `default`                        |
| `X-App-Id`      | ⚠️ Recommended        | IDM-assigned application ID (numeric) | `5`                              |
| `Content-Type`  | ✅ Yes (POST/PUT)     | Request body format                   | `application/json`               |
| `Time-Zone`     | ⚠️ Recommended        | Client timezone (IANA identifier)     | `America/Anchorage`              |
| `x-api-version` | ⚠️ Optional           | API version number                    | `1.0`                            |
| `X-Request-Id`  | ⚠️ Optional           | Request tracing ID                    | `a1b2c3d4`                       |

---

## Authentication Flow

### Obtain Access Token

**Endpoint:** `POST /api/user/v1/login`

**Request Headers:**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "your_password"
}
```

**Success Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "appCode": "default",
    "tenantId": "1000",
    "user": {
      "id": "123456789",
      "email": "user@example.com",
      "username": "John Doe"
    }
  }
}
```

**Response Fields:**

| Field         | Type   | Description                               |
| ------------- | ------ | ----------------------------------------- |
| `accessToken` | string | JWT access token for subsequent API calls |
| `tokenType`   | string | Token type, always `Bearer`               |
| `expiresIn`   | int    | Token validity period (seconds)           |
| `appCode`     | string | Application identifier                    |
| `tenantId`    | string | Tenant ID                                 |
| `user`        | object | Current user information                  |

> **Important Notes:**
>
> - Login endpoint is rate-limited: max 10 requests per 60 seconds
> - Store the `accessToken` securely and reuse it within its validity period
> - When the token expires, call the refresh endpoint to obtain a new token

---

### Refresh Access Token

When the token is about to expire, use the current token to obtain a new one.

**Endpoint:** `POST /api/user/v1/refresh-access-token`

**Request Headers:**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**Request Body:**

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs..."
}
```

**Success Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...(new_token)",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "appCode": "default",
    "tenantId": "1000",
    "user": { ... }
  }
}
```

> **Note:** The refresh token endpoint is rate-limited to max 20 requests per 60 seconds.

---

### Third-Party Login

Supports login through third-party identity providers (e.g., IdentityHub / IDM).

**Endpoint:** `POST /api/user/v1/third-party-login`

**Request Headers:**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**Request Body:**

```json
{
  "provider": "identity_hub",
  "externalToken": "third_party_access_token",
  "email": "user@example.com"
}
```

> **Note:** Third-party login endpoint is rate-limited to max 10 requests per 60 seconds.

---

### Logout

**Endpoint:** `POST /api/user/v1/logout`

**Request Headers:**

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
X-Tenant-Id: 1000
```

After logout, the current token will be revoked and can no longer be used.

---

## Client Credentials Authentication (App-Level / AI Agent)

For server-to-server integrations, automated systems, or AI Agents that need to call Workflow APIs without user interaction, the system supports **Client Credentials** (OAuth2 `client_credentials` grant type) authentication.

### How It Works

Client Credentials tokens are issued by the ItemIAM identity management system. When a request carries a Client Credentials token, the system:

1. Identifies the token as `client_credentials` type (via `grant_type` claim or `token_category: "Client"`)
2. Sets the authentication scheme to `ItemIamClientIdentification`
3. **Automatically bypasses all `[WFEAuthorize]` permission checks** — no specific permissions (e.g., WORKFLOW:CREATE, CASE:READ) are required
4. Extracts tenant context from the `X-Tenant-Id` header

### Obtain Client Credentials Token

Contact your system administrator to obtain a `client_id` and `client_secret` for your application.

**Endpoint:** ItemIAM Token Endpoint

```http
POST /oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id={your_client_id}&client_secret={your_client_secret}
```

**Response:**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

### Required Headers for Client Credentials Requests

| Header          | Required          | Description                     | Example                          |
| --------------- | ----------------- | ------------------------------- | -------------------------------- |
| `Authorization` | ✅ Yes            | Client Credentials Bearer Token | `Bearer eyJhbGciOiJSUzI1NiIs...` |
| `X-Tenant-Id`   | ✅ Yes            | Target tenant ID (numeric)      | `1000`                           |
| `X-App-Code`    | ⚠️ Recommended    | Application identifier          | `default`                        |
| `Content-Type`  | ✅ Yes (POST/PUT) | Request body format             | `application/json`               |

> ⚠️ **Important**: `X-Tenant-Id` is **mandatory** for Client Credentials requests. Without it, tenant isolation will fail and the request will be rejected.

### Permission Behavior

| Aspect                             | Behavior                             |
| ---------------------------------- | ------------------------------------ |
| `[WFEAuthorize]` permission checks | ✅ Automatically bypassed            |
| `[Authorize]` authentication check | ✅ Passes (token is valid)           |
| Tenant data isolation              | ✅ Enforced via `X-Tenant-Id`        |
| `UserContext.UserId`               | Set to `"0"` (no user identity)      |
| `UserContext.UserName`             | Set to client name from token claims |
| `UserContext.SystemSource`         | Set to `Client`                      |

### Example: AI Agent Calling Workflow API

```bash
# 1. Obtain Client Credentials Token from ItemIAM
CLIENT_TOKEN=$(curl -s -X POST https://iam.item.pub/oauth2/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=ai-agent-001&client_secret=your_secret" \
  | jq -r '.access_token')

# 2. Call Workflow API with Client Token
curl -X GET https://workflow-dev.item.pub/api/ow/workflows/v1 \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default"

# 3. Create a workflow
curl -X POST https://workflow-dev.item.pub/api/ow/workflows/v1 \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default" \
  -H "Content-Type: application/json" \
  -d '{"name": "Auto-configured Workflow", "description": "Created by AI Agent"}'
```

### Example: Python AI Agent

```python
import requests

IAM_URL = "https://iam.item.pub"
WFE_URL = "https://workflow-dev.item.pub"
CLIENT_ID = "ai-agent-001"
CLIENT_SECRET = "your_secret"
TENANT_ID = "1000"

# Step 1: Get Client Credentials Token
token_response = requests.post(
    f"{IAM_URL}/oauth2/token",
    data={
        "grant_type": "client_credentials",
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET
    }
)
access_token = token_response.json()["access_token"]

# Step 2: Call Workflow APIs (no user permissions needed)
headers = {
    "Authorization": f"Bearer {access_token}",
    "X-Tenant-Id": TENANT_ID,
    "X-App-Code": "default",
    "Content-Type": "application/json"
}

# List workflows
workflows = requests.get(f"{WFE_URL}/api/ow/workflows/v1", headers=headers)
print(workflows.json())

# Create checklist
checklist = requests.post(
    f"{WFE_URL}/api/ow/checklists/v1",
    headers=headers,
    json={"name": "Device Intake Checklist", "description": "Auto-created by AI Agent"}
)
print(f"Created checklist ID: {checklist.json()['data']}")
```

### Differences from User Token Authentication

| Feature             | User Token (password/login) | Client Token (client_credentials)       |
| ------------------- | --------------------------- | --------------------------------------- |
| Requires user login | ✅ Yes                      | ❌ No                                   |
| Has user identity   | ✅ Full user context        | ❌ UserId = "0"                         |
| Permission checks   | ✅ Enforced per endpoint    | ❌ Bypassed                             |
| Suitable for        | Web UI, user-facing apps    | AI Agents, server-to-server, automation |
| Token lifetime      | Shorter (user session)      | Configurable (typically longer)         |
| Refresh mechanism   | Refresh token               | Re-request with credentials             |

### Security Considerations

1. **Store client secrets securely** — Never expose in client-side code or version control
2. **Use short-lived tokens** — Configure appropriate token expiration
3. **Restrict by IP/network** — Configure ItemIAM to limit client credential usage to known IPs
4. **Audit logging** — All Client Token API calls are logged with the client name for traceability
5. **Principle of least privilege** — Only grant client credentials to systems that genuinely need automated access

---

## API Call Examples

### Example: Query Integration List

```http
GET /api/integration/v1?name=CRM&type=CRM&status=Active HTTP/1.1
Host: workflow-dev.item.pub
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
X-Tenant-Id: 1000
X-App-Code: default
X-App-Id: 5
Time-Zone: America/Anchorage
```

### Example: Using cURL

```bash
# 1. Login to obtain token
curl -X POST https://workflow-dev.item.pub/api/user/v1/login \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 1000" \
  -d '{
    "email": "user@example.com",
    "password": "your_password"
  }'

# 2. Use token to call API
curl -X GET https://workflow-dev.item.pub/api/integration/v1 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default" \
  -H "X-App-Id: 5" \
  -H "Time-Zone: Asia/Shanghai"
```

### Example: Using JavaScript (Fetch)

```javascript
// Login
const loginResponse = await fetch(
  "https://workflow-dev.item.pub/api/user/v1/login",
  {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Tenant-Id": "1000",
    },
    body: JSON.stringify({
      email: "user@example.com",
      password: "your_password",
    }),
  },
);

const { data } = await loginResponse.json();
const accessToken = data.accessToken;

// Call business API
const response = await fetch(
  "https://workflow-dev.item.pub/api/integration/v1",
  {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "X-Tenant-Id": "1000",
      "X-App-Code": "default",
      "X-App-Id": "5",
      "Time-Zone": Intl.DateTimeFormat().resolvedOptions().timeZone,
    },
  },
);
```

### Example: Using C# (HttpClient)

```csharp
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://workflow-dev.item.pub");

// Login
var loginRequest = new { email = "user@example.com", password = "your_password" };
var loginContent = new StringContent(
    JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1000");
var loginResponse = await httpClient.PostAsync("/api/user/v1/login", loginContent);
var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

// Set authentication header
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);
httpClient.DefaultRequestHeaders.Add("X-App-Code", "default");
httpClient.DefaultRequestHeaders.Add("X-App-Id", "5");
httpClient.DefaultRequestHeaders.Add("Time-Zone", "Asia/Shanghai");

// Call business API
var response = await httpClient.GetAsync("/api/integration/v1");
```

### Example: Using Python (requests)

```python
import requests

BASE_URL = "https://workflow-dev.item.pub"
TENANT_ID = "1000"

# Login
login_response = requests.post(
    f"{BASE_URL}/api/user/v1/login",
    headers={
        "Content-Type": "application/json",
        "X-Tenant-Id": TENANT_ID
    },
    json={
        "email": "user@example.com",
        "password": "your_password"
    }
)

token = login_response.json()["data"]["accessToken"]

# Call business API
response = requests.get(
    f"{BASE_URL}/api/integration/v1",
    headers={
        "Authorization": f"Bearer {token}",
        "X-Tenant-Id": TENANT_ID,
        "X-App-Code": "default",
        "X-App-Id": "5",
        "Time-Zone": "Asia/Shanghai"
    }
)
```

---

## API Versioning

Workflow API supports two versioning methods:

1. **URL Path Versioning** (recommended): Version number is included in the URL path

   ```
   /api/integration/v1/...
   /api/user/v1/...
   /api/ow/onboardings/v1/...
   ```

2. **Header Versioning**: Specify version via `x-api-version` header
   ```
   x-api-version: 1.0
   ```

The current default version is `v1.0`. If no version is specified, the default version is used automatically.

---

## Error Handling

### Standard Error Response Format

```json
{
  "success": false,
  "errorCode": "ERROR_CODE",
  "message": "Error description"
}
```

### Common HTTP Status Codes

| Status Code | Description                  | Recommended Action                            |
| ----------- | ---------------------------- | --------------------------------------------- |
| 400         | Bad Request                  | Check request body format and required fields |
| 401         | Unauthorized / Invalid Token | Re-login to obtain a new token                |
| 403         | Forbidden                    | Verify user roles and permissions             |
| 404         | Not Found                    | Check request path and resource ID            |
| 429         | Too Many Requests            | Reduce request frequency, retry later         |
| 500         | Internal Server Error        | Contact system administrator                  |

### Authentication Error Codes

| Error Code            | Description            | Resolution                          |
| --------------------- | ---------------------- | ----------------------------------- |
| `UNAUTHORIZED`        | User not authenticated | Call login endpoint to obtain token |
| `TOKEN_EXPIRED`       | Token has expired      | Call refresh token endpoint         |
| `TOKEN_INVALID`       | Token is invalid       | Re-login                            |
| `TOKEN_REVOKED`       | Token has been revoked | Re-login                            |
| `RATE_LIMIT_EXCEEDED` | Rate limit exceeded    | Wait and retry                      |

---

## Troubleshooting

### 1. Receiving 401 Unauthorized Error

**Possible causes:**

- Missing `Authorization` header
- Incorrect token format (missing `Bearer ` prefix)
- Token has expired
- Token has been revoked (user logged out)

**Solutions:**

1. Verify header format: `Authorization: Bearer <token>`
2. Check if token is expired; if so, call the refresh endpoint
3. If refresh fails, re-login to obtain a new token

### 2. Receiving 403 Forbidden Error

**Possible causes:**

- Current user lacks permission to access the resource
- Tenant ID mismatch
- Portal Token attempting to access non-Portal endpoints

**Solutions:**

1. Verify `X-Tenant-Id` matches the user's assigned tenant
2. Contact administrator to confirm user permission configuration

### 3. Empty or Incorrect Data Returned

**Possible causes:**

- Incorrect `X-Tenant-Id`, querying wrong tenant's data
- Incorrect `X-App-Code`, causing application isolation filtering

**Solutions:**

1. Verify the `X-Tenant-Id` value is correct (should be a numeric tenant ID like `1000`)
2. Verify the `X-App-Code` value matches the application the data belongs to

### 4. Request Rate Limited (429)

**Possible causes:**

- Too many requests sent in a short period

**Solutions:**

1. Implement exponential backoff retry strategy
2. Cache the token to avoid frequent login calls
3. Control request frequency appropriately

### 5. File Upload Failures

**Important notes:**

- Maximum single file size: 50MB
- Maximum form value size: 10MB
- Use `multipart/form-data` format for uploads

---

## Related Documentation

- [Workflow API Swagger Documentation](/swagger/v1/swagger.json) - Complete API endpoint definitions
- [Integration Management API](../docs/) - External system integration endpoints
- [Workflow Engine API](../docs/) - Workflow management endpoints

---

## Security Best Practices

1. **Never hardcode passwords or tokens in client-side code**
2. **Use HTTPS** to ensure transport security
3. **Secure token storage** - Use secure storage mechanisms (e.g., HttpOnly Cookies, encrypted local storage)
4. **Proactively refresh tokens** - Refresh before expiration
5. **Clean up on logout** - Call the logout endpoint to revoke the token and clear local storage
6. **Principle of least privilege** - Only request necessary permissions and data scopes
