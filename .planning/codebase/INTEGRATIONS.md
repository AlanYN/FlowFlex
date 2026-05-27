# External Integrations

**Analysis Date:** 2026-05-25

## APIs & External Services

**Identity & Auth (IDM):**
- Item IDM (Identity Management) - User authentication, team/user queries, role/permission checks
  - SDK/Client: `Item.Internal.Auth` 8.0.0, custom `IdmUserDataClient` (`packages/flowFlex-backend/Application/Services/OW/IdmUserDataClient.cs`)
  - Config section: `IdmApis` in `appsettings.Development.json`
  - Endpoints used: `/connect/token`, `/api/v1/users`, `/api/v1/public/teams`, `/api/v1/public/teamusers`, `/api/v1/role/check`
  - Auth vars: `IdmApis:ClientId`, `IdmApis:ClientSecret`, `IdmApis:AppId`
  - Dev base URL: `https://idm-dev.item.pub`

**Microsoft Outlook / Graph API:**
- Microsoft Graph API - Email binding, OAuth2 callback, email sync
  - SDK/Client: Custom `OutlookService` (`packages/flowFlex-backend/Application/Services/MessageCenter/OutlookService.cs`)
  - Config section: `OutlookApis` in `appsettings.Development.json`
  - Auth: OAuth2 client credentials flow via `https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token`
  - Auth vars: `OutlookApis:ClientId`, `OutlookApis:ClientSecret`, `OutlookApis:TenantId`
  - Callback endpoint: `/api/ow/email-binding/v1/callback`
  - Base URL: `https://graph.microsoft.com/v1.0`

**AI Providers (configurable per user):**
- Multiple AI providers supported (OpenAI, Azure OpenAI, and others) - Workflow generation, questionnaire generation, checklist generation
  - SDK/Client: Custom HTTP client, user-configurable via `AIModelConfig` (`packages/flowFlex-common/src/app/apis/ai/config.ts`)
  - API endpoints: `/ai/config/v1/models`, `/ai/config/v1/providers`, `/ai/config/v1/models/test`
  - Auth vars: Per-user `apiKey` stored in database, `baseUrl` configurable per provider
  - Features: Streaming responses (`enableStreaming`), workflow/questionnaire/checklist generation via system prompts in `appsettings.json` under `AI.Prompts`

**Nacos (Service Discovery/Config):**
- Alibaba Nacos - Service registration and configuration management
  - SDK/Client: `Item.Internal.Nacos` 8.0.0 (`packages/flowFlex-backend/WebApi/WebApi.csproj`)
  - Config: Managed via `Item.Internal.Framework`

**IDE APIs:**
- Internal IDE service - Purpose not fully detailed in config
  - Config section: `IdeApis` in `appsettings.json`
  - Dev base URL: `https://ide-dev.item.pub`

## Data Storage

**Databases:**
- PostgreSQL (primary database)
  - Connection: `Database:ConnectionString` in `appsettings.json`
  - Dev server: `pgsql01-share-rds-aliyun.item.pub:5432`, database `ow_open`
  - Client: SqlSugar ORM (`SqlSugarCore` 5.1.4.159-preview23) via `packages/flowFlex-backend/SqlSugarDB/`
  - Also used for Hangfire job storage (`Hangfire.PostgreSql` 1.20.9)
  - Also used for Serilog log sink (`Serilog.Sinks.PostgreSQL` 2.3.0)
  - Config: `Database:ConfigId`, `Database:DbType = PostgreSQL`, `Database:CommandTimeout`, `Database:EnableSqlLogging`
  - Migrations: SQL files embedded in `packages/flowFlex-backend/SqlSugarDB/Migrations/*.sql`

**File Storage (configurable):**
- Local filesystem - Default dev storage
  - Path: `wwwroot/uploads` (configured via `FileStorage:LocalStoragePath`)
  - URL prefix: `/uploads`
- Aliyun OSS - Cloud blob storage (production default)
  - SDK/Client: `Aliyun.OSS.SDK.NetCore` 2.14.1 (`packages/flowFlex-backend/Infrastructure/Infrastructure.csproj`)
  - Auth vars: `BlobStore:AccessKeyId`, `BlobStore:SecretAccessKey`, `BlobStore:EndPoint`, `BlobStore:Bucket`
  - Dev endpoint: `oss-cn-shanghai.aliyuncs.com`, bucket `oss-share-test`
- AWS S3 - Alternative cloud blob storage
  - SDK/Client: `AWSSDK.S3` 3.7.406 + `AWSSDK.Core` 3.7.401 (`packages/flowFlex-backend/Infrastructure/Infrastructure.csproj`)
  - Auth vars: `BlobStore:AccessKeyId`, `BlobStore:SecretAccessKey`, `BlobStore:Region`, `BlobStore:Bucket`
- Storage type selected via: `Global:BlobStoreType` (Local / OSS / AWS)
- Abstraction: `Item.BlobProvider` 8.0.0

**Caching:**
- Redis - Distributed cache and session
  - SDK/Client: `Item.Redis` 8.0.1, `Microsoft.Extensions.Caching.StackExchangeRedis` 8.0.0 (`packages/flowFlex-backend/Infrastructure/Infrastructure.csproj`)
  - Connection: `Redis:ConnectionString` in `appsettings.json`
  - Dev server: `redis-cluster-dev.item.pub:6379`
  - Key prefix: `unis:ff` (dev), `ff` (local)
  - Config: `Cache:Enabled`, `Cache:DefaultExpiryMinutes`, `Cache:EnableGracefulDegradation`
- In-memory cache - Fallback / development mode
  - Used via `IMemoryCache` for `IdentityHubClient` and distributed memory cache in dev

## Authentication & Identity

**Auth Providers (multi-scheme JWT):**
- Local JWT Bearer - Primary auth for direct FlowFlex users
  - Implementation: `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.2
  - Config: `Security:JwtSecretKey`, `Security:JwtIssuer`, `Security:JwtAudience`, `Security:JwtExpiryMinutes` (1440 min = 24h)
  - Token handler: `WebApi/Authentication/TokenValidatedHandler.cs`

- Item IdentityHub (IDM) - SSO via external identity provider
  - Scheme: `AuthSchemes.Identification`
  - Config section: `IdentityHubConfig` — `Authority`, `Issuer`, `EnableIdentityHub`
  - Enabled when `IdentityHubConfig:EnableIdentityHub = true`
  - Token handler: `TokenValidatedHandler.OnIdmTokenValidated`

- Item IAM - Internal IAM system
  - Scheme: `AuthSchemes.ItemIamIdentification`
  - Config section: `ItemIamConfig`
  - Enabled when `Global:EnableItemIam = true`
  - Token handler: `TokenValidatedHandler.OnIamItemTokenValidated`

- Portal Token - Limited-scope tokens for customer portal users
  - Stored in `localStorage` as `portal_access_token`
  - Applied automatically for paths starting with `/customer-portal`, `/onboard/sub-portal/portal`, `/portal-access`

**Authorization:**
- Custom `WfeAuthorizationHandler` (`packages/flowFlex-backend/WebApi/Authorization/WfeAuthorizationHandler.cs`)
- Policy `BearerOrIdm` supports both local JWT and IDM tokens
- Multi-tenancy via `AppCode` header (`X-App-Code`) enforced by `AppIsolationMiddleware`

## Messaging

**RabbitMQ:**
- SDK/Client: `RabbitMQ.Client` 6.8.1 + `Item.Message.RabbitMq` 8.0.0 (`packages/flowFlex-backend/Infrastructure/Infrastructure.csproj`, `WebApi/WebApi.csproj`)
- Config: Managed via `Item.Internal.Framework` / Nacos

**Kafka:**
- SDK/Client: `Item.Message.Kafka` 8.0.4 (`packages/flowFlex-backend/Application.Contracts/Application.Contracts.csproj`)
- Config: Managed via `Item.Internal.Framework` / Nacos

## Background Jobs

**Hangfire:**
- SDK/Client: `Hangfire.AspNetCore` 1.8.12, `Hangfire.PostgreSql` 1.20.9, `Hangfire.Dashboard.BasicAuthorization` 1.0.2
- Storage: PostgreSQL (same database as application)
- Dashboard: Protected via `Hangfire.Dashboard.BasicAuthorization`
- Custom background services:
  - `BackgroundTaskQueue` / `BackgroundTaskService` (`packages/flowFlex-backend/Infrastructure/Services/`)
  - `EmailSyncBackgroundService` (`packages/flowFlex-backend/Application/Services/MessageCenter/EmailSyncBackgroundService.cs`)

## Monitoring & Observability

**Logging:**
- Serilog with multiple sinks:
  - File sink: `Serilog.Sinks.File` 5.0.0 — logs to `/app/logs/` in Docker
  - PostgreSQL sink: `Serilog.Sinks.PostgreSQL` 2.3.0 — structured log storage in DB
  - Console (via `Serilog.AspNetCore` 8.0.3)
- Config: `Logging:LogLevel` in `appsettings.json`, Serilog settings via `Serilog.Settings.Configuration` 8.0.8

**Monitoring Client:**
- `Item.Infrastructure.Monitoring.Client` 0.0.1-alpha — internal monitoring (alpha, `WebApi/WebApi.csproj`)

**API Documentation:**
- Swagger/OpenAPI via `Swashbuckle.AspNetCore` 6.6.2
- Available at `/swagger` in Development environment
- JWT Bearer security definition included

## Email

**SMTP:**
- Provider: Mailgun (dev: `smtp.mailgun.org:587`)
- SDK/Client: `Item.Email.Lib` 8.0.0
- Config section: `Email` — `SmtpServer`, `SmtpPort`, `EnableSsl`, `FromEmail`, `Username`, `Password`
- Auth vars: `Email:Username`, `Email:Password`
- Templates: HTML files embedded in `packages/flowFlex-backend/Application/Templates/Email/*.html`, rendered via DotLiquid

## CI/CD & Deployment

**Containerization:**
- Docker multi-stage build: `packages/flowFlex-backend/Dockerfile`
  - Build stage: `mcr.microsoft.com/dotnet/sdk:8.0`
  - Runtime stage: `mcr.microsoft.com/dotnet/aspnet:8.0`
  - Runs as non-root user `flowflex` (uid/gid 1001)
  - Health check: `curl -f http://localhost:8080/health`

**CI Pipeline:**
- Not detected in repository root (no `.github/workflows` at project level)

**Hosting:**
- Dev: `https://workflow-dev.item.pub`
- Preview: `https://workflow-preview.item.pub` (inferred from env files)

## Webhooks & Callbacks

**Incoming:**
- Microsoft OAuth2 callback: `GET/POST /api/ow/email-binding/v1/callback` — Outlook OAuth2 redirect URI
- Integration inbound attachment: documented in `packages/flowFlex-backend/WebApi/Controllers/Integration/Inbound Attachment Integration Protocol.md`

**Outgoing:**
- Integration outbound attachment: documented in `packages/flowFlex-backend/WebApi/Controllers/Integration/Outbound Attachment Integration Protocol.md`

## Environment Configuration

**Required backend env vars / config keys:**
- `Database:ConnectionString` - PostgreSQL connection
- `Redis:ConnectionString` - Redis connection
- `Security:JwtSecretKey` - JWT signing key (min 32 chars)
- `Security:JwtIssuer` / `Security:JwtAudience` - JWT validation
- `Security:EncryptionKey` / `Security:EncryptionIV` - AES encryption
- `BlobStore:AccessKeyId` / `BlobStore:SecretAccessKey` - Blob storage credentials
- `Email:Username` / `Email:Password` - SMTP credentials
- `IdmApis:ClientId` / `IdmApis:ClientSecret` - IDM OAuth2 client
- `OutlookApis:ClientId` / `OutlookApis:ClientSecret` / `OutlookApis:TenantId` - Microsoft Graph

**Required frontend env vars:**
- `VITE_GLOB_API_URL` - Backend API base URL
- `VITE_PROXY_URL` - Dev proxy target (backend address)
- `VITE_GLOB_IDM_URL` - IDM base URL
- `VITE_GLOB_DOMAIN_URL` - App domain URL
- `VITE_GLOB_SSOURL` - SSO URL
- `VITE_GLOB_CODE` - App code for multi-tenancy

**Secrets location:**
- Backend: `appsettings.Development.json` (dev), environment variables or secrets manager (production)
- Frontend: `.env.{mode}` files (not committed for production secrets)

---

*Integration audit: 2026-05-25*
