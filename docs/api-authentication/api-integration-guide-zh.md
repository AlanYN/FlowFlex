# Workflow API 集成指南

## 目录

- [环境配置](#环境配置)
- [关键前置条件](#关键前置条件)
  - [1. 租户标识 (X-Tenant-Id)](#1-租户标识-x-tenant-id)
  - [2. 应用标识 (X-App-Code)](#2-应用标识-x-app-code)
  - [3. 应用 ID (X-App-Id)](#3-应用-id-x-app-id)
  - [4. 认证令牌 (Authorization)](#4-认证令牌-authorization)
- [所有 API 请求必需的请求头](#所有-api-请求必需的请求头)
- [认证流程](#认证流程)
  - [获取访问令牌](#获取访问令牌)
  - [刷新访问令牌](#刷新访问令牌)
  - [第三方登录](#第三方登录)
  - [登出](#登出)
- [Client Credentials 认证（应用级 / AI Agent 调用）](#client-credentials-认证应用级--ai-agent-调用)
- [API 调用示例](#api-调用示例)
- [API 版本控制](#api-版本控制)
- [错误处理](#错误处理)
- [常见问题排查](#常见问题排查)
- [相关文档](#相关文档)

---

## 环境配置

| 环境     | Base URL                            | 说明         |
| -------- | ----------------------------------- | ------------ |
| 开发环境 | `https://workflow-dev.item.pub`     | 开发测试使用 |
| 测试环境 | `https://workflow-staging.item.com` | UAT 验收使用 |
| 生产环境 | `https://workflow.item.com`         | 正式生产环境 |

> **注意**: 请向系统管理员确认实际的 API 地址。所有接口均以 `/api/` 为统一前缀。

---

## 关键前置条件

### 1. 租户标识 (X-Tenant-Id)

Workflow 是一个多租户系统，每个请求必须携带租户标识以确保数据隔离。

> ⚠️ **注意**：系统中存在「租户 ID」和「租户 Code」两个概念，请求头 `X-Tenant-Id` 中传递的是**租户 ID（数字）**，而非租户 Code。

**字段格式：**

| 属性       | 说明                                                      |
| ---------- | --------------------------------------------------------- |
| 类型       | `string`（内容为纯数字的租户 ID）                         |
| 格式       | **纯数字字符串**，代表系统分配的租户 ID                   |
| 最大长度   | 32 个字符                                                 |
| 默认值     | `default`（仅开发/测试环境，生产环境必须提供真实租户 ID） |
| 是否可为空 | ❌ 不可为空字符串或空白字符，否则抛出异常                 |

**示例值：** `1000`、`1005`、`1012`

> ⚠️ **重要**：`TenantId` 不能为空字符串或空白字符（`null`、`""`、`" "`），否则系统将抛出 `ArgumentException` 错误，提示 "TenantId is required for tenant isolation"。

**获取方式：**

- 由系统管理员分配
- 登录成功后在响应中返回 `tenantId` 字段
- 也可以从 JWT Token 的 Claims 中解析

**传递方式（优先级从高到低）：**

1. 请求头 `X-Tenant-Id`（推荐）
2. 请求头 `TenantId`
3. 查询参数 `tenantId` 或 `tenant_id`
4. JWT Token 中的 `tenantId` Claim

### 2. 应用标识 (X-App-Code)

用于标识调用来源的应用程序，实现应用级别的数据隔离。

**字段格式：**

| 属性       | 说明                                 |
| ---------- | ------------------------------------ |
| 类型       | `string`                             |
| 最大长度   | 32 个字符                            |
| 格式       | 英文字符串，代表应用标识             |
| 默认值     | `default`（未提供时系统自动使用）    |
| 是否可为空 | 可以不传，系统将使用默认值 `default` |

**示例值：** `default`、`WEB`、`MOBILE`、`ADMIN`

> 💡 **说明**：大多数集成场景下传 `default` 即可。系统也会根据请求路径自动推断 AppCode（如 `/api/mobile/` → `MOBILE`、`/api/admin/` → `ADMIN`），但建议显式传递以确保准确性。

**获取方式：**

- 由系统管理员分配
- 通过 IDM（身份管理系统）登录后获取
- 常见值：`default`、`WEB`、`MOBILE`、`ADMIN`

**传递方式（优先级从高到低）：**

1. 请求头 `X-App-Code`（推荐）
2. 请求头 `AppCode`
3. 查询参数 `appCode` 或 `app_code`
4. JWT Token 中的 `appCode` Claim
5. 未提供时默认为 `default`

### 3. 应用 ID (X-App-Id)

用于标识当前接入的应用系统 ID，由 IDM 系统分配。

**字段格式：**

| 属性       | 说明                                     |
| ---------- | ---------------------------------------- |
| 类型       | `string`（内容为纯数字的应用 ID）        |
| 格式       | **纯数字字符串**，代表 IDM 分配的应用 ID |
| 是否可为空 | ⚠️ 推荐传递                              |

**示例值：** `5`

> 💡 **说明**：`X-App-Id` 是 IDM 身份管理系统分配给当前应用的唯一标识，用于 IDM 公共 API 的调用鉴权。

### 4. 认证令牌 (Authorization)

系统使用 JWT Bearer Token 进行身份认证。

**格式：**

```
Authorization: Bearer <your_access_token>
```

---

## 所有 API 请求必需的请求头

| 请求头          | 是否必需               | 说明                        | 示例值                           |
| --------------- | ---------------------- | --------------------------- | -------------------------------- |
| `Authorization` | ✅ 是（除登录接口外）  | JWT Bearer Token            | `Bearer eyJhbGciOiJSUzI1NiIs...` |
| `X-Tenant-Id`   | ✅ 是                  | 租户 ID（纯数字）           | `1000`                           |
| `X-App-Code`    | ⚠️ 推荐                | 应用标识                    | `default`                        |
| `X-App-Id`      | ⚠️ 推荐                | IDM 分配的应用 ID（纯数字） | `5`                              |
| `Content-Type`  | ✅ 是（POST/PUT 请求） | 请求体格式                  | `application/json`               |
| `Time-Zone`     | ⚠️ 推荐                | 客户端时区（IANA 时区标识） | `America/Anchorage`              |
| `x-api-version` | ⚠️ 可选                | API 版本号                  | `1.0`                            |
| `X-Request-Id`  | ⚠️ 可选                | 请求追踪 ID                 | `a1b2c3d4`                       |

---

## 认证流程

### 获取访问令牌

**接口：** `POST /api/user/v1/login`

**请求头：**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**请求体：**

```json
{
  "email": "user@example.com",
  "password": "your_password"
}
```

**成功响应：**

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

**响应字段说明：**

| 字段          | 类型   | 说明                            |
| ------------- | ------ | ------------------------------- |
| `accessToken` | string | JWT 访问令牌，用于后续 API 调用 |
| `tokenType`   | string | 令牌类型，固定为 `Bearer`       |
| `expiresIn`   | int    | 令牌有效期（秒）                |
| `appCode`     | string | 应用标识                        |
| `tenantId`    | string | 租户 ID                         |
| `user`        | object | 当前用户信息                    |

> **重要提示：**
>
> - 登录接口有频率限制：每 60 秒最多 10 次请求
> - 请妥善保存 `accessToken`，在有效期内重复使用
> - 令牌过期后需要调用刷新接口获取新令牌

---

### 刷新访问令牌

当令牌即将过期时，可以使用当前令牌刷新获取新令牌。

**接口：** `POST /api/user/v1/refresh-access-token`

**请求头：**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**请求体：**

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs..."
}
```

**成功响应：**

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

> **注意：** 刷新令牌接口频率限制为每 60 秒最多 20 次请求。

---

### 第三方登录

支持通过第三方身份提供商（如 IdentityHub / IDM）进行登录。

**接口：** `POST /api/user/v1/third-party-login`

**请求头：**

```http
Content-Type: application/json
X-Tenant-Id: 1000
```

**请求体：**

```json
{
  "provider": "identity_hub",
  "externalToken": "third_party_access_token",
  "email": "user@example.com"
}
```

> **注意：** 第三方登录接口频率限制为每 60 秒最多 10 次请求。

---

### 登出

**接口：** `POST /api/user/v1/logout`

**请求头：**

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
X-Tenant-Id: 1000
```

登出后当前令牌将被撤销，无法再使用。

---

## Client Credentials 认证（应用级 / AI Agent 调用）

对于服务端到服务端的集成、自动化系统或 AI Agent 等无需用户交互即可调用 Workflow API 的场景，系统支持 **Client Credentials**（OAuth2 `client_credentials` 授权类型）认证方式。

### 工作原理

Client Credentials Token 由 ItemIAM 身份管理系统签发。当请求携带 Client Credentials Token 时，系统会：

1. 识别 Token 类型为 `client_credentials`（通过 `grant_type` claim 或 `token_category: "Client"`）
2. 设置认证方案为 `ItemIamClientIdentification`
3. **自动绕过所有 `[WFEAuthorize]` 权限检查** — 无需具体权限（如 WORKFLOW:CREATE、CASE:READ）
4. 从 `X-Tenant-Id` 请求头提取租户上下文

### 获取 Client Credentials Token

联系系统管理员获取应用的 `client_id` 和 `client_secret`。

**接口：** ItemIAM Token 端点

```http
POST /oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id={your_client_id}&client_secret={your_client_secret}
```

**响应：**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

### Client Credentials 请求必需的请求头

| Header          | 必填             | 说明                            | 示例                             |
| --------------- | ---------------- | ------------------------------- | -------------------------------- |
| `Authorization` | ✅ 是            | Client Credentials Bearer Token | `Bearer eyJhbGciOiJSUzI1NiIs...` |
| `X-Tenant-Id`   | ✅ 是            | 目标租户 ID（数字）             | `1000`                           |
| `X-App-Code`    | ⚠️ 推荐          | 应用标识                        | `default`                        |
| `Content-Type`  | ✅ 是 (POST/PUT) | 请求体格式                      | `application/json`               |

> ⚠️ **重要**：Client Credentials 请求**必须**携带 `X-Tenant-Id`。缺少此头部将导致租户隔离失败，请求会被拒绝。

### 权限行为

| 方面                       | 行为                               |
| -------------------------- | ---------------------------------- |
| `[WFEAuthorize]` 权限检查  | ✅ 自动绕过                        |
| `[Authorize]` 认证检查     | ✅ 通过（Token 有效）              |
| 租户数据隔离               | ✅ 通过 `X-Tenant-Id` 强制执行     |
| `UserContext.UserId`       | 设为 `"0"`（无用户身份）           |
| `UserContext.UserName`     | 设为 Token claims 中的 client name |
| `UserContext.SystemSource` | 设为 `Client`                      |

### 示例：AI Agent 调用 Workflow API

```bash
# 1. 从 ItemIAM 获取 Client Credentials Token
CLIENT_TOKEN=$(curl -s -X POST https://iam.item.pub/oauth2/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=ai-agent-001&client_secret=your_secret" \
  | jq -r '.access_token')

# 2. 使用 Client Token 调用 Workflow API
curl -X GET https://workflow-dev.item.pub/api/ow/workflows/v1 \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default"

# 3. 创建工作流
curl -X POST https://workflow-dev.item.pub/api/ow/workflows/v1 \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default" \
  -H "Content-Type: application/json" \
  -d '{"name": "Auto-configured Workflow", "description": "Created by AI Agent"}'
```

### 示例：Python AI Agent

```python
import requests

IAM_URL = "https://iam.item.pub"
WFE_URL = "https://workflow-dev.item.pub"
CLIENT_ID = "ai-agent-001"
CLIENT_SECRET = "your_secret"
TENANT_ID = "1000"

# 第1步：获取 Client Credentials Token
token_response = requests.post(
    f"{IAM_URL}/oauth2/token",
    data={
        "grant_type": "client_credentials",
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET
    }
)
access_token = token_response.json()["access_token"]

# 第2步：调用 Workflow API（无需用户权限）
headers = {
    "Authorization": f"Bearer {access_token}",
    "X-Tenant-Id": TENANT_ID,
    "X-App-Code": "default",
    "Content-Type": "application/json"
}

# 查询工作流列表
workflows = requests.get(f"{WFE_URL}/api/ow/workflows/v1", headers=headers)
print(workflows.json())

# 创建 Checklist
checklist = requests.post(
    f"{WFE_URL}/api/ow/checklists/v1",
    headers=headers,
    json={"name": "Device Intake Checklist", "description": "AI Agent 自动创建"}
)
print(f"Created checklist ID: {checklist.json()['data']}")
```

### 与用户 Token 认证的区别

| 特性         | 用户 Token (password/login) | Client Token (client_credentials) |
| ------------ | --------------------------- | --------------------------------- |
| 需要用户登录 | ✅ 是                       | ❌ 否                             |
| 有用户身份   | ✅ 完整用户上下文           | ❌ UserId = "0"                   |
| 权限检查     | ✅ 按接口强制执行           | ❌ 自动绕过                       |
| 适用场景     | Web UI、面向用户的应用      | AI Agent、服务端对接、自动化      |
| Token 有效期 | 较短（用户会话）            | 可配置（通常较长）                |
| 刷新机制     | Refresh Token               | 重新用凭证请求                    |

### 安全注意事项

1. **安全存储 Client Secret** — 不要暴露在客户端代码或版本控制中
2. **使用短期 Token** — 配置合适的 Token 过期时间
3. **限制 IP/网络** — 在 ItemIAM 中配置 Client Credentials 仅限已知 IP 使用
4. **审计日志** — 所有 Client Token API 调用都会记录 client name 以便追溯
5. **最小权限原则** — 仅为确实需要自动化访问的系统授予 Client Credentials

---

## API 调用示例

### 示例：查询集成列表

```http
GET /api/integration/v1?name=CRM&type=CRM&status=Active HTTP/1.1
Host: workflow-dev.item.pub
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
X-Tenant-Id: 1000
X-App-Code: default
X-App-Id: 5
Time-Zone: America/Anchorage
```

### 示例：使用 cURL

```bash
# 1. 登录获取令牌
curl -X POST https://workflow-dev.item.pub/api/user/v1/login \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 1000" \
  -d '{
    "email": "user@example.com",
    "password": "your_password"
  }'

# 2. 使用令牌调用 API
curl -X GET https://workflow-dev.item.pub/api/integration/v1 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "X-Tenant-Id: 1000" \
  -H "X-App-Code: default" \
  -H "X-App-Id: 5" \
  -H "Time-Zone: Asia/Shanghai"
```

### 示例：使用 JavaScript (Fetch)

```javascript
// 登录
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

// 调用业务 API
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

### 示例：使用 C# (HttpClient)

```csharp
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://workflow-dev.item.pub");

// 登录
var loginRequest = new { email = "user@example.com", password = "your_password" };
var loginContent = new StringContent(
    JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1000");
var loginResponse = await httpClient.PostAsync("/api/user/v1/login", loginContent);
var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

// 设置认证头
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);
httpClient.DefaultRequestHeaders.Add("X-App-Code", "default");
httpClient.DefaultRequestHeaders.Add("X-App-Id", "5");
httpClient.DefaultRequestHeaders.Add("Time-Zone", "Asia/Shanghai");

// 调用业务 API
var response = await httpClient.GetAsync("/api/integration/v1");
```

---

## API 版本控制

Workflow API 支持两种版本控制方式：

1. **URL 路径版本**（推荐）：版本号包含在 URL 路径中

   ```
   /api/integration/v1/...
   /api/user/v1/...
   /api/ow/onboardings/v1/...
   ```

2. **请求头版本**：通过 `x-api-version` 请求头指定
   ```
   x-api-version: 1.0
   ```

当前默认版本为 `v1.0`，未指定版本时自动使用默认版本。

---

## 错误处理

### 标准错误响应格式

```json
{
  "success": false,
  "errorCode": "ERROR_CODE",
  "message": "错误描述信息"
}
```

### 常见 HTTP 状态码

| 状态码 | 说明              | 处理建议                 |
| ------ | ----------------- | ------------------------ |
| 400    | 请求参数错误      | 检查请求体格式和必填字段 |
| 401    | 未认证 / 令牌无效 | 重新登录获取新令牌       |
| 403    | 权限不足          | 确认用户角色和权限       |
| 404    | 资源不存在        | 检查请求路径和资源 ID    |
| 429    | 请求频率超限      | 降低请求频率，稍后重试   |
| 500    | 服务器内部错误    | 联系系统管理员           |

### 认证相关错误码

| 错误码                | 说明         | 处理方式             |
| --------------------- | ------------ | -------------------- |
| `UNAUTHORIZED`        | 用户未认证   | 调用登录接口获取令牌 |
| `TOKEN_EXPIRED`       | 令牌已过期   | 调用刷新令牌接口     |
| `TOKEN_INVALID`       | 令牌无效     | 重新登录             |
| `TOKEN_REVOKED`       | 令牌已被撤销 | 重新登录             |
| `RATE_LIMIT_EXCEEDED` | 频率限制     | 等待后重试           |

---

## 常见问题排查

### 1. 收到 401 Unauthorized 错误

**可能原因：**

- 未携带 `Authorization` 请求头
- Token 格式错误（缺少 `Bearer ` 前缀）
- Token 已过期
- Token 已被撤销（用户已登出）

**解决方案：**

1. 确认请求头格式：`Authorization: Bearer <token>`
2. 检查 Token 是否过期，过期则调用刷新接口
3. 如果刷新失败，重新登录获取新 Token

### 2. 收到 403 Forbidden 错误

**可能原因：**

- 当前用户没有访问该资源的权限
- 租户 ID 不匹配
- Portal Token 尝试访问非 Portal 接口

**解决方案：**

1. 确认 `X-Tenant-Id` 与用户所属租户一致
2. 联系管理员确认用户权限配置

### 3. 数据返回为空或不正确

**可能原因：**

- `X-Tenant-Id` 设置错误，导致查询了错误租户的数据
- `X-App-Code` 设置错误，导致应用隔离过滤

**解决方案：**

1. 确认 `X-Tenant-Id` 值正确（应为纯数字的租户 ID，如 `1000`）
2. 确认 `X-App-Code` 值与数据所属应用一致

### 4. 请求被限流 (429)

**可能原因：**

- 短时间内发送了过多请求

**解决方案：**

1. 实现指数退避重试策略
2. 缓存 Token，避免频繁调用登录接口
3. 合理控制请求频率

### 5. 文件上传失败

**注意事项：**

- 单文件最大 50MB
- 表单值最大 10MB
- 使用 `multipart/form-data` 格式上传

---

## 相关文档

- [Workflow API Swagger 文档](/swagger/v1/swagger.json) - 完整的 API 接口定义
- [集成管理 API](../docs/) - 外部系统集成相关接口
- [工作流引擎 API](../docs/) - 工作流管理相关接口

---

## 安全建议

1. **不要在客户端代码中硬编码密码或 Token**
2. **使用 HTTPS** 确保传输安全
3. **Token 存储安全** - 使用安全存储机制（如 HttpOnly Cookie、加密本地存储）
4. **及时刷新 Token** - 在 Token 过期前主动刷新
5. **登出时清理** - 调用登出接口撤销 Token，并清理本地存储
6. **最小权限原则** - 仅请求必要的权限和数据范围
