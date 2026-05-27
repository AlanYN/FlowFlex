<!-- refreshed: 2026-05-25 -->
# Architecture

**Analysis Date:** 2026-05-25

## System Overview

```text
┌─────────────────────────────────────────────────────────────────────┐
│                        Frontend (Vue 3 SPA)                          │
│              packages/flowFlex-common/src/                           │
│   Views → Stores (Pinia) → API Layer (Axios) → Backend REST API      │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTP/REST (JWT Bearer)
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     WebApi Layer                                      │
│         packages/flowFlex-backend/WebApi/                            │
│  Controllers → Middleware Pipeline → Auth (JWT + IDM + ItemIAM)      │
└──────────┬──────────────────────────────────────────────────────────┘
           │ Constructor Injection
           ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Application Layer (Business Logic)                       │
│  packages/flowFlex-backend/Application/                              │
│  packages/flowFlex-backend/Application.Contracts/                    │
│   IServices (interfaces) ← Services (implementations)               │
│   DTOs · AutoMapper Profiles · Notification Handlers                 │
└──────────┬──────────────────────────────────────────────────────────┘
           │ Repository Interfaces
           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Domain Layer                                       │
│         packages/flowFlex-backend/Domain/                            │
│   Entities · Repository Interfaces · Abstracts · Shared Enums        │
└──────────┬──────────────────────────────────────────────────────────┘
           │ SqlSugar ORM
           ▼
┌──────────────────────┬──────────────────────────────────────────────┐
│  SqlSugarDB Layer    │  Infrastructure Layer                         │
│  BaseRepository<T>   │  Cross-cutting: Logging, Encryption,          │
│  Migrations          │  GlobalExceptionHandling, BackgroundTasks      │
│  PostgreSQL          │  packages/flowFlex-backend/Infrastructure/     │
└──────────────────────┴──────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | Path |
|-----------|----------------|------|
| WebApi Controllers | HTTP routing, request/response mapping, auth enforcement | `packages/flowFlex-backend/WebApi/Controllers/` |
| Application Services | Business logic, orchestration, workflow rules | `packages/flowFlex-backend/Application/Services/` |
| Application.Contracts | DTOs, service interfaces, options | `packages/flowFlex-backend/Application.Contracts/` |
| Domain Entities | Core business objects, table mappings | `packages/flowFlex-backend/Domain/Entities/` |
| Domain Repository Interfaces | Data access contracts | `packages/flowFlex-backend/Domain/Repository/` |
| SqlSugarDB | Repository implementations, migrations | `packages/flowFlex-backend/SqlSugarDB/` |
| Infrastructure | Logging, encryption, exception handling, background tasks | `packages/flowFlex-backend/Infrastructure/` |
| Frontend Views | Page-level UI components | `packages/flowFlex-common/src/app/views/` |
| Frontend Stores | Pinia state management | `packages/flowFlex-common/src/app/stores/` |
| Frontend APIs | Axios-based HTTP client modules | `packages/flowFlex-common/src/app/apis/` |

## Pattern Overview

**Overall:** Clean Architecture with vertical slice organization by domain (OW = Onboarding Workflow)

**Key Characteristics:**
- Dependency inversion: controllers depend on `IService` interfaces, not concrete implementations
- Auto-registration: services implement `IScopedService`, `ISingletonService`, or `ITransientService` marker interfaces for DI scanning
- Multi-tenancy: every entity carries `AppCode` + `TenantId`; `AppIsolationMiddleware` extracts these per-request
- Soft deletes: `IsValid` flag on all entities via `IValidFilter` interface; SqlSugar applies global filter
- Snowflake IDs: all primary keys are `long` generated via `SnowFlakeSingle.Instance.NextId()`

## Layers

**WebApi Layer:**
- Purpose: HTTP entry point, authentication, authorization, middleware pipeline
- Location: `packages/flowFlex-backend/WebApi/`
- Contains: Controllers, Middlewares, Filters, Authentication handlers, Converters
- Depends on: Application.Contracts (IServices, DTOs)
- Used by: Frontend, external API consumers

**Application Layer:**
- Purpose: Business logic, service orchestration, AutoMapper profiles
- Location: `packages/flowFlex-backend/Application/`
- Contains: Service implementations, Maps (AutoMapper profiles), Helpers, Notification handlers, Email templates
- Depends on: Application.Contracts, Domain, Infrastructure
- Used by: WebApi

**Application.Contracts Layer:**
- Purpose: Shared contracts between WebApi and Application
- Location: `packages/flowFlex-backend/Application.Contracts/`
- Contains: DTOs (organized by domain), IService interfaces, Options classes
- Depends on: Domain.Shared
- Used by: WebApi, Application

**Domain Layer:**
- Purpose: Core business entities and repository contracts
- Location: `packages/flowFlex-backend/Domain/`
- Contains: Entities (OW, Action, DynamicData, Integration), Repository interfaces, Abstracts
- Depends on: Domain.Shared only
- Used by: Application, SqlSugarDB

**SqlSugarDB Layer:**
- Purpose: Data access implementation using SqlSugar ORM
- Location: `packages/flowFlex-backend/SqlSugarDB/`
- Contains: `BaseRepository<T>`, concrete repository implementations, migrations, context setup
- Depends on: Domain
- Used by: Application (via injected repository interfaces)

**Infrastructure Layer:**
- Purpose: Cross-cutting technical concerns
- Location: `packages/flowFlex-backend/Infrastructure/`
- Contains: `GlobalExceptionHandlingMiddleware`, `ApplicationLogger`, `EncryptionService`, `BackgroundTaskQueue`, `BackgroundTaskService`
- Depends on: Domain.Shared
- Used by: WebApi, Application

## Data Flow

### Primary API Request Path

1. HTTP request arrives at Kestrel → `Program.cs` middleware pipeline (`packages/flowFlex-backend/WebApi/Program.cs`)
2. `GlobalExceptionHandlingMiddleware` wraps the pipeline for error capture (`packages/flowFlex-backend/Infrastructure/Exceptions/`)
3. `AppIsolationMiddleware` extracts `AppCode` + `TenantId` from headers/JWT/query (`packages/flowFlex-backend/WebApi/Middlewares/AppIsolationMiddleware.cs`)
4. `TenantMiddleware` sets tenant context (`packages/flowFlex-backend/WebApi/Middlewares/TenantMiddleware.cs`)
5. JWT authentication validates Bearer token; `TokenValidatedHandler` enriches claims
6. `WFEAuthorize` attribute checks fine-grained permissions via `IPermissionService`
7. Controller method receives validated DTO, calls `IService` method (`packages/flowFlex-backend/WebApi/Controllers/OW/`)
8. Service executes business logic, calls repository interface (`packages/flowFlex-backend/Application/Services/OW/`)
9. `BaseRepository<T>` executes SqlSugar query against PostgreSQL (`packages/flowFlex-backend/SqlSugarDB/BaseRepository.cs`)
10. AutoMapper maps entity → DTO; controller returns `Success<T>(data)` wrapped in `SuccessResponse`

### Frontend Request Path

1. Vue component calls API module function (`packages/flowFlex-common/src/app/apis/`)
2. Axios instance (configured in `packages/flowFlex-common/src/app/apis/axios/index.ts`) adds JWT token, `AppCode`, timezone headers
3. `transformResponseHook` unwraps `SuccessResponse` envelope
4. Result stored in Pinia store (`packages/flowFlex-common/src/app/stores/modules/`) or returned directly to component

### Background Task Flow

1. Service enqueues work via `IBackgroundTaskQueue` (`packages/flowFlex-backend/Infrastructure/Services/`)
2. `BackgroundTaskService` (hosted service) dequeues and executes
3. `EmailSyncBackgroundService` runs independently for Outlook email sync

**State Management (Frontend):**
- Pinia stores in `packages/flowFlex-common/src/app/stores/modules/` handle: `user`, `permission`, `locale`, `multipleTab`, `menuFunction`, `workflowCanvas`
- Persisted state via `pinia-plugin-persistedstate`

## Key Abstractions

**OwEntityBase / EntityBaseCreateInfo:**
- Purpose: Base class for all domain entities; provides `Id` (snowflake), `AppCode`, `TenantId`, `IsValid`, audit fields
- Examples: `packages/flowFlex-backend/Domain/Entities/Base/OwEntityBase.cs`, `packages/flowFlex-backend/Domain/Entities/Base/EntityBaseCreateInfo.cs`
- Pattern: All OW entities extend `EntityBaseCreateInfo` which extends `EntityBase` → `AbstractEntityBase`

**IBaseRepository<T>:**
- Purpose: Generic CRUD contract for all repositories
- Examples: `packages/flowFlex-backend/Domain/Repository/IBaseRepository.cs`
- Pattern: Domain defines interface; `BaseRepository<T>` in SqlSugarDB implements it; specific repos (e.g., `IWorkflowRepository`) extend it

**IScopedService / ISingletonService / ITransientService:**
- Purpose: Marker interfaces for automatic DI registration scanning
- Pattern: Services implement the appropriate marker; `AddService()` in `packages/flowFlex-backend/WebApi/Extensions/ServiceCollectionExtensions.cs` scans and registers

**AutoMapper Profiles:**
- Purpose: Entity ↔ DTO mapping
- Examples: `packages/flowFlex-backend/Application/Maps/WorkflowMapProfile.cs`, `packages/flowFlex-backend/Application/Maps/OnboardingMapProfile.cs`
- Pattern: One profile per aggregate root; registered explicitly in `Program.cs`

**UserContext:**
- Purpose: Per-request user identity (userId, email, tenantId, appCode)
- Location: `packages/flowFlex-backend/Domain/Shared/Models/`
- Pattern: Scoped service populated by middleware; injected into services via constructor

## Entry Points

**Backend API:**
- Location: `packages/flowFlex-backend/WebApi/Program.cs`
- Triggers: Kestrel HTTP server
- Responsibilities: DI registration, middleware pipeline, database initialization, hosted services startup

**Frontend SPA:**
- Location: `packages/flowFlex-common/src/main.ts`
- Triggers: Browser load or Wujie micro-app mount (`window.__WUJIE_MOUNT`)
- Responsibilities: App bootstrap, i18n setup, Pinia store setup, router guard setup, Element Plus registration

**Database Migrations:**
- Location: `packages/flowFlex-backend/SqlSugarDB/Migrations/`
- Triggers: `app.Services.InitializeDatabase()` called at startup in `Program.cs`

## Architectural Constraints

- **Threading:** ASP.NET Core async/await throughout; `SqlSugarScope` (not singleton) used per-request to avoid concurrency conflicts
- **Global state:** `SnowFlakeSingle.Instance` is a module-level singleton for ID generation (`packages/flowFlex-backend/Domain/Shared/`); `UserContext` is scoped (per-request)
- **Multi-tenancy filter:** SqlSugar global filter applies `AppCode` + `IsValid` automatically on all queries; bypassing requires explicit `.Filter(null, true)`
- **Long → string serialization:** All `long` IDs are serialized as strings in JSON responses via `LongToStringConverter` to avoid JavaScript precision loss
- **Wujie micro-frontend:** The Vue app supports running as a Wujie sub-app; `window.__POWERED_BY_WUJIE__` controls mount/unmount lifecycle

## Anti-Patterns

### Duplicate using directives in service files

**What happens:** Some service files (e.g., `WorkflowService.cs`) contain duplicate `using` statements for the same namespace
**Why it's wrong:** Causes compiler warnings and indicates copy-paste without cleanup
**Do this instead:** Keep one `using` per namespace; use IDE cleanup tools before committing

### Distributed cache fallback to memory in production

**What happens:** `Program.cs` uses `AddDistributedMemoryCache()` in non-development environments with a TODO comment for Redis
**Why it's wrong:** Memory cache is not shared across instances; breaks horizontal scaling
**Do this instead:** Configure `AddStackExchangeRedisCache()` for production; the Redis client is already registered via `builder.Services.AddRedis()`

### Token blacklist validation disabled

**What happens:** `OnTokenValidated` in `Program.cs` has the token revocation check commented out
**Why it's wrong:** Revoked tokens remain valid until expiry; logout does not invalidate tokens server-side
**Do this instead:** Uncomment and implement the `IAccessTokenService.ValidateTokenAsync` path when the access token table is stable

## Error Handling

**Strategy:** Global exception middleware catches all unhandled exceptions; services throw typed `CRMException` with `ErrorCodeEnum`

**Patterns:**
- `GlobalExceptionHandlingMiddleware` in `packages/flowFlex-backend/Infrastructure/Exceptions/` converts exceptions to structured JSON responses
- Services throw `CRMException(ErrorCodeEnum, message)` for business rule violations
- Controllers use `Success<T>(data)` helper from `packages/flowFlex-backend/WebApi/Controllers/ControllerBase.cs` — never return raw `Ok()`
- Frontend `transformResponseHook` in `packages/flowFlex-common/src/app/apis/axios/index.ts` handles non-success codes and shows `ElMessage` errors

## Cross-Cutting Concerns

**Logging:** Serilog (structured); `IApplicationLogger` wrapper in `packages/flowFlex-backend/Infrastructure/Services/Logging/`; slow requests (>3s) logged as warnings by `AppIsolationMiddleware`

**Validation:** FluentValidation for request DTOs; model state validation suppressed (`SuppressModelStateInvalidFilter = true`) — validation errors pass through to manual handling in controllers

**Authentication:** Three JWT schemes supported simultaneously: local JWT Bearer, IdentityHub (IDM), ItemIAM — configured via `IdentityHubConfig` and `ItemIamConfig` sections; `WFEAuthorize` attribute enforces fine-grained permissions

**Multi-tenancy:** `AppCode` + `TenantId` extracted by `AppIsolationMiddleware` from headers (`X-App-Code`, `X-Tenant-Id`), query params, JWT claims, or email domain inference; stored in `HttpContext.Items["AppContext"]`

---

*Architecture analysis: 2026-05-25*
