# API Authentication Documentation / API 认证文档

This directory contains the API authentication and integration guide for Workflow system.

本目录包含 Workflow 系统的 API 认证与集成指南。

## Documents / 文档

| File                                                         | Language | Description                                    |
| ------------------------------------------------------------ | -------- | ---------------------------------------------- |
| [api-integration-guide-zh.md](./api-integration-guide-zh.md) | 中文     | API 集成认证指南（中文版）                     |
| [api-integration-guide-en.md](./api-integration-guide-en.md) | English  | API Integration Authentication Guide (English) |

## Supported Authentication Methods / 支持的认证方式

| Method                 | Use Case                                    | Description                                      |
| ---------------------- | ------------------------------------------- | ------------------------------------------------ |
| User Login (JWT)       | Web UI, user-facing apps                    | User logs in with email/password, gets JWT token |
| Third-Party Login      | SSO integrations                            | Login via IdentityHub/IDM                        |
| **Client Credentials** | **AI Agents, server-to-server, automation** | **App-level token, bypasses permission checks**  |
| Portal Token           | External portal users                       | Limited access for invited portal users          |

## Quick Start / 快速开始

### For User-facing Applications:

1. Obtain your `X-Tenant-Id` from the system administrator
2. Call the login API to get an access token
3. Include the token and required headers in all subsequent API requests

### For AI Agents / Server-to-Server Integration:

1. Obtain `client_id` and `client_secret` from the system administrator
2. Call ItemIAM `/oauth2/token` with `grant_type=client_credentials` to get a token
3. Include the token + `X-Tenant-Id` header in all API requests
4. No user permissions needed — Client Credentials tokens bypass permission checks

---

### 面向用户的应用：

1. 从系统管理员处获取 `X-Tenant-Id`
2. 调用登录接口获取访问令牌
3. 在后续所有 API 请求中携带令牌和必要的请求头

### AI Agent / 服务端对接：

1. 从系统管理员处获取 `client_id` 和 `client_secret`
2. 调用 ItemIAM `/oauth2/token`（`grant_type=client_credentials`）获取 Token
3. 在所有 API 请求中携带 Token + `X-Tenant-Id` 请求头
4. 无需用户权限 — Client Credentials Token 自动绕过权限检查
