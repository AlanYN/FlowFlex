---
inclusion: manual
---

# Skill: api-design — API 接口设计

## 职责

基于业务需求和数据库 Schema，设计完整的 RESTful API（或 GraphQL），包括端点定义、请求/响应格式、错误码、认证方式和 OpenAPI 文档。

---

## 输入

- `docs/{project-name}_{YYYY-MM-DD}/requirements-analysis/context.md`（用户故事 + AC）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/database-schema.md`（数据模型）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/architecture-decisions.md`（认证方案）

---

## 执行步骤

### 步骤 1：识别 API 端点

从每个用户故事的 AC 中提取需要的 API 操作：
- 每个 CRUD 操作对应一个端点
- 复杂业务操作（如"下单"）设计为独立端点
- 认证相关端点（登录、注册、刷新 Token）

### 步骤 2：设计端点规范

每个端点必须定义：
- HTTP 方法 + 路径
- 路径参数、查询参数
- 请求体（Request Body）Schema
- 成功响应（2xx）Schema
- 错误响应（4xx/5xx）Schema
- 认证要求（Public / Bearer Token / Admin）

### 步骤 3：统一响应格式

定义全局响应结构：

```json
// 成功响应
{
  "success": true,
  "data": { ... },
  "meta": { "page": 1, "total": 100 }  // 分页时包含
}

// 错误响应
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "字段验证失败",
    "details": [{ "field": "email", "message": "格式不正确" }]
  }
}
```

### 步骤 4：定义错误码

| 错误码 | HTTP 状态 | 含义 |
|--------|---------|------|
| VALIDATION_ERROR | 400 | 请求参数验证失败 |
| UNAUTHORIZED | 401 | 未登录或 Token 无效 |
| FORBIDDEN | 403 | 无权限执行此操作 |
| NOT_FOUND | 404 | 资源不存在 |
| CONFLICT | 409 | 资源冲突（如邮箱已注册） |
| INTERNAL_ERROR | 500 | 服务器内部错误 |

### 步骤 5：生成 OpenAPI 文档

用 YAML 格式生成 OpenAPI 3.0 规范。

---

## 输出格式

写入 `specs/{project-name}_{YYYY-MM-DD}/technical-design/api-spec.md`：

```markdown
# API Specification

## 基础信息
- **Base URL**：`/api/v1`
- **认证方式**：Bearer Token（JWT）
- **内容类型**：`application/json`

## 端点总览

| 方法 | 路径 | 描述 | 认证 | 关联 US |
|------|------|------|------|---------|
| POST | /auth/register | 用户注册 | Public | US-001 |
| POST | /auth/login | 用户登录 | Public | US-001 |
| GET | /users/me | 获取当前用户信息 | Bearer | US-002 |

## 端点详情

### POST /auth/register
**描述**：用户注册
**认证**：Public
**关联 AC**：AC-001-1

**请求体**：
```json
{
  "email": "string (required, email format)",
  "password": "string (required, min 8 chars)",
  "name": "string (required)"
}
```

**成功响应** (201)：
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "email": "string",
    "name": "string",
    "createdAt": "ISO8601"
  }
}
```

**错误响应**：
| 状态码 | 错误码 | 触发条件 |
|--------|--------|---------|
| 400 | VALIDATION_ERROR | 邮箱格式错误或密码太短 |
| 409 | CONFLICT | 邮箱已被注册 |

## 全局错误响应格式
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "人类可读的错误描述"
  }
}
```

## 分页规范
查询参数：`?page=1&limit=20&sort=createdAt&order=desc`
响应 meta：`{ "page": 1, "limit": 20, "total": 100, "totalPages": 5 }`

## OpenAPI 3.0 规范（YAML）
```yaml
openapi: 3.0.0
info:
  title: {project-name} API
  version: 1.0.0
paths:
  /auth/register:
    post:
      summary: 用户注册
      # ...
```
```

---

## 澄清检查点（APIAMB）

以下情况标记为 APIAMB，需用户确认：
- REST vs GraphQL 选择
- API 版本策略（URL 版本 vs Header 版本）
- 分页方式（offset 分页 vs cursor 分页）
- 文件上传方式（multipart vs base64 vs 预签名 URL）
- WebSocket / SSE 实时推送需求
- API 限流策略
