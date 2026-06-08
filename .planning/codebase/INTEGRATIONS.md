# External Integrations

**Analysis Date:** 2026-06-08

## APIs & External Services

**Identity & Authentication:**
- Item IDM (IdentityHub) - User authentication, team queries, role/permission checks
  - SDK/Client: `Item.Internal.Auth` (local NuGet package)
  - Base URL config: `IdmApis:BaseUrl` (e.g., `https://idm-dev.item.pub`)
  - Endpoints: `/connect/token`, `/api/v1/users`, `/api/v1/public/teams`, `/api/v1/public/teams/teamTree`, `/api/v1/public/teamusers`, `/api/v1/users/current/extension`, `/api/v1/role/check`
  - Auth: Client credentials (`IdmApis:ClientId`, `IdmApis:ClientSecret`)
  - HttpClient: `FlowFlexIdmUserDataClient` with Polly retry + 30s timeout
  - Implementation: `packages/flowFlex-backend/Application/Services/OW/IdmUserDataClient.cs`

- Item IAM - Alternative identity provider (newer system)
  - Config section: `ItemIamConfig`
  - Authority: `https://id-dev.item.pub`
  - JWT scheme: `ItemIamIdentification`
  - Enabled via: `Global:EnableItemIam` flag

**Microsoft Graph (Outlook):**
- Microsoft Graph API - Outlook email sync, calendar integration
  - SDK/Client: Custom `OutlookService` with HttpClient
  - Base URL: `https://graph.microsoft.com/v1.0`
  - Auth: OAuth2 with `OutlookApis:ClientId`, `OutlookApis:ClientSecret`, `OutlookApis:TenantId`
  - Token endpoint: `https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token`
  - Callback: `OutlookApis:RedirectUri` (e.g., `https://workflow-dev.item.pub/api/ow/email-binding/v1/callback`)
  - Implementation: `packages/flowFlex-backend/Application/Services/MessageCenter/OutlookService.cs`
  - Background sync: `packages/flowFlex-backend/Application/Services/MessageCenter/EmailSyncBackgroundService.cs`
  - HttpClient config: 60s timeout, connection pooling (10 max/server), Polly retry

**AI Providers:**
- Multi-provider AI integration - Workflow/questionnaire/checklist generation
  - Supported providers: ZhipuAI, OpenAI, Gemini, Claude, DeepSeek, LLMGateway (Item Gateway), generic OpenAI-compatible
  - Implementation: `packages/flowFlex-backend/Application/Services/AI/Providers/AIProviderAdapter.cs`
  - Base class: `packages/flowFlex-backend/Application/Services/AI/AIServiceBase.cs`
  - Config section: `AI` (features, prompts, connection test settings)
  - Per-user API key stored in database (not env vars)
  - JWT token cache for Item Gateway with 1-hour TTL

**Code Execution (Judge0):**
- Judge0 IDE - Online code execution service
  - Client: `packages/flowFlex-backend/Application/Client/IdeClient.cs`
  - Base URL config: `IdeApis:BaseUrl` (e.g., `https://ide-dev.item.pub`)
  - Endpoints: `POST /submissions`, `GET /submissions/{token}`
  - Uses base64 encoding for source code

**External Integration Framework:**
- Generic HTTP API integration - Configurable external API calls
  - Client: `packages/flowFlex-backend/Application/Services/Integration/IntegrationHttpClient.cs`
  - Supports authenticated requests to user-configured external APIs
  - HttpClient names: `HttpApiExecutor` (30s timeout, 100 max connections), `GeneralHttpClient`
  - Logging: `packages/flowFlex-backend/Application/Services/Integration/IntegrationApiLogService.cs`
  - Registration: `packages/flowFlex-backend/Application/Client/AddHttpClientExtensions.cs`

## Data Storage

**Databases:**
- PostgreSQL 15
  - Connection config: `Database:ConnectionString`
  - ORM: SqlSugarCore 5.1.4.159-preview23
  - Config ID: `FlowFlex`
  - Command timeout: 30s
  - Connection pooling: MinPool=1, MaxPool=20
  - Features: SQL logging, performance logging, retry (3 attempts, 1s delay)
  - Tables prefixed with `ff_`
  - Serilog sink: `Serilog.Sinks.PostgreSQL` (logs written to DB)
  - Hangfire storage: `Hangfire.PostgreSql`

**File Storage:**
- Aliyun OSS (development/default)
  - SDK: `Aliyun.OSS.SDK.NetCore 2.14.1`
  - Provider: `Item.BlobProvider 8.0.0`
  - Config: `BlobStore:EndPoint`, `BlobStore:Bucket`, `BlobStore:AccessKeyId`, `BlobStore:SecretAccessKey`
  - Endpoint: `oss-cn-shanghai.aliyuncs.com`

- AWS S3 (alternate)
  - SDK: `AWSSDK.S3 3.7.406`
  - Region: configurable (e.g., `us-west-2`)

- Local filesystem (development fallback)
  - Path: `wwwroot/uploads`
  - URL prefix: `/uploads`
  - Config: `FileStorage:StorageType` = `Local`

- Storage type switch: `Global:BlobStoreType` (values: `Local`, `OSS`, `AWS`)
- File config: `packages/flowFlex-backend/WebApi/appsettings.json` section `FileStorage`
- Max file size: ~1GB, allowed extensions configurable
- File name encryption and date-based grouping supported

**Caching:**
- Redis
  - Client: `Item.Redis 8.0.1` + `Microsoft.Extensions.Caching.StackExchangeRedis 8.0.0`
  - Config: `Redis:ConnectionString`, `Redis:KeyPrefix` (prefix: `unis:ff`)
  - Used for: distributed caching, session data
  - Cache config section: `Cache` (default expiry 10 min, graceful degradation enabled)
  - Fallback: `DistributedMemoryCache` when Redis unavailable (current non-dev setup)

## Authentication & Identity

**Auth Providers (3 simultaneous JWT schemes):**

1. Local JWT (`Bearer` - default scheme)
   - Implementation: Custom JWT generation/validation
   - Config: `Security:JwtSecretKey`, `Security:JwtIssuer`, `Security:JwtAudience`
   - Expiry: 1440 minutes (24 hours)
   - Token validation in `JwtBearerEvents.OnTokenValidated`
   - Handler: `packages/flowFlex-backend/WebApi/Authentication/TokenValidatedHandler.cs`

2. IdentityHub IDM (`Identification` scheme)
   - Authority: `IdentityHubConfig:Authority`
   - Enabled via: `IdentityHubConfig:EnableIdentityHub`
   - Does not validate audience

3. ItemIAM (`ItemIamIdentification` scheme)
   - Authority: `ItemIamConfig:Authority`
   - Enabled via: `Global:EnableItemIam`
   - Does not validate audience

**Authorization:**
- Custom handler: `WfeAuthorizationHandler` (`packages/flowFlex-backend/WebApi/Authorization/`)
- Combined policy: `BearerOrIdm` (accepts both Bearer and IDM tokens)
- Permission attribute: `RequirePermissionAttribute` (action filter)
- Client credentials bypass: `PermissionHelpers.IsClientCredentialsToken()`

## Monitoring & Observability

**Logging:**
- Serilog 3.1.1 (structured logging)
  - Sinks: File (`Serilog.Sinks.File 5.0.0`), PostgreSQL (`Serilog.Sinks.PostgreSQL 2.3.0`)
  - Config: `Serilog.Settings.Configuration 8.0.4`
  - Expressions: `Serilog.Expressions 4.0.0`
  - Custom application logger: `packages/flowFlex-backend/Infrastructure/Services/Logging/ApplicationLogger.cs`

**Monitoring:**
- Item.Infrastructure.Monitoring.Client 0.0.1-alpha (internal monitoring package)

**Error Tracking:**
- Global exception middleware: `packages/flowFlex-backend/Infrastructure/Exceptions/GlobalExceptionHandlingMiddleware.cs`
- Integration API logging: `packages/flowFlex-backend/WebApi/Filters/IntegrationApiLogFilter.cs`

## CI/CD & Deployment

**Hosting:**
- Docker containers (Linux)
- Port: 8080 (Kestrel)
- Health check endpoint: `/health`

**Docker Compose:**
- `packages/flowFlex-backend/docker-compose.yml` - Production-like (PostgreSQL 15 Alpine + API)
- `packages/flowFlex-backend/docker-compose.dev.yml` - Development environment
- Volumes: `postgres_data`, `uploads_data`, `logs_data`
- Network: `flowflex-network` (bridge driver)

**Service Discovery:**
- Nacos (via `Item.Internal.Nacos 8.0.0`, backed by `nacos-sdk-csharp 1.3.8`)
- Used for: service registration, configuration management

## Messaging

**RabbitMQ:**
- Package: `RabbitMQ.Client 6.8.1` (direct) + `Item.Message.RabbitMq 8.0.0` (abstraction)
- Purpose: Async message processing, event-driven communication

**Kafka:**
- Package: `Item.Message.Kafka` (referenced in `Directory.Build.props`)
- AWS MSK Auth: `AWS.MSK.Auth 1.0.0`
- Purpose: Event streaming (available but usage secondary to RabbitMQ)

**In-Process:**
- MediatR 12.3.0 - Domain event handling within application boundary

## Email

**SMTP (Mailgun):**
- Server: `smtp.mailgun.org:587` (TLS)
- Config section: `Email`
- Package: `Item.Email.Lib 8.0.0`
- Templates: `packages/flowFlex-backend/Application/Templates/Email/*.html` (embedded resources)
- Services: `packages/flowFlex-backend/Application/Services/MessageCenter/EmailService.cs`, `EmailTemplateService.cs`

## Background Processing

**Hangfire:**
- Package: `Hangfire.AspNetCore 1.8.12`
- Storage: PostgreSQL (`Hangfire.PostgreSql 1.20.9`)
- Dashboard: Basic auth (`Hangfire.Dashboard.BasicAuthorization 1.0.2`)

**Custom Background Services:**
- `BackgroundTaskQueue` + `BackgroundTaskService` - Generic task queue (`packages/flowFlex-backend/Infrastructure/Services/`)
- `EmailSyncBackgroundService` - Outlook email sync (`packages/flowFlex-backend/Application/Services/MessageCenter/`)
- `MessageCleanupBackgroundService` - Message cleanup (`packages/flowFlex-backend/Application/Services/MessageCenter/`)

## Environment Configuration

**Required env vars (Backend - appsettings sections):**
- `Database:ConnectionString` - PostgreSQL connection
- `Redis:ConnectionString` - Redis connection
- `Security:JwtSecretKey` - JWT signing key (min 32 chars)
- `Security:EncryptionKey` / `Security:EncryptionIV` - Data encryption
- `IdmApis:BaseUrl` / `IdmApis:ClientId` / `IdmApis:ClientSecret` - IDM auth
- `BlobStore:AccessKeyId` / `BlobStore:SecretAccessKey` - Object storage
- `Email:Username` / `Email:Password` - SMTP credentials
- `OutlookApis:ClientId` / `OutlookApis:ClientSecret` / `OutlookApis:TenantId` - Microsoft Graph

**Required env vars (Frontend - .env files):**
- `VITE_GLOB_API_URL` - Backend API base URL
- `VITE_PROXY_URL` - Dev proxy target
- `VITE_GLOB_IDM_URL` - IDM service URL
- `VITE_GLOB_CODE` - App code identifier
- `VITE_GLOB_DOMAIN_URL` - Domain URL for callbacks

**Secrets location:**
- Backend: `appsettings.{Environment}.json` files (not committed for production)
- Production secrets: Nacos configuration service or environment variables

## Webhooks & Callbacks

**Incoming:**
- `/api/ow/email-binding/v1/callback` - Microsoft OAuth2 redirect callback for Outlook email binding

**Outgoing:**
- External Integration HTTP calls - Configurable per-integration (via `IntegrationHttpClient`)
- AI Provider API calls - Per-user configured endpoints

## HTTP Resilience

**Polly Policies:**
- Retry: 3 attempts with exponential backoff (2^n seconds)
- Timeout: 30s per request (IDM client)
- Applied to: IDM HttpClient, Outlook HttpClient
- Config: `packages/flowFlex-backend/WebApi/Program.cs` (lines 593-600)

---

*Integration audit: 2026-06-08*
