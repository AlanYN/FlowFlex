<!-- refreshed: 2026-06-08 -->
# Architecture

**Analysis Date:** 2026-06-08

## System Overview

```text
┌─────────────────────────────────────────────────────────────────────┐
│                        Frontend (Vue 3 SPA)                          │
│              packages/flowFlex-common/src/app/                       │
│   Views → Stores (Pinia) → API Layer (Axios) → Backend REST API     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTP/REST (JWT Bearer)
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     WebApi Layer                                      │
│         packages/flowFlex-backend/WebApi/                            │
│  Controllers → Middleware Pipeline → Auth (JWT + IDM + ItemIAM)      │
└──────────┬──────────────────────────────────────────────────────────┘
           │ Constructor Injection (IService interfaces)
           ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Application Layer (Business Logic)                       │
│  packages/flowFlex-backend/Application/                              │
│  packages/flowFlex-backend/Application.Contracts/                    │
│   IServices (interfaces) ← Services (implementations)               │
│   DTOs · AutoMapper Profiles · Notification Handlers                 │
└──────────┬──────────────────────────────────────────────────────────┘
           │ Repository Interfaces (Domain layer)
           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Domain Layer                                       │
│         packages/flowFlex-backend/Domain/                            │
│   Entities · Repository Interfaces · Abstracts · Filter Interfaces   │
└──────────┬──────────────────────────────────────────────────────────┘
           │ SqlSugar ORM (ISqlSugarClient)
           ▼
┌──────────────────────┬──────────────────────────────────────────────┐
│  SqlSugarDB Layer    │  Infrastructure Layer                         │
│  BaseRepository<T>   │  Cross-cutting: Logging, Encryption,          │
│  Migrations          │  GlobalExceptionHandling, BackgroundTasks      │
│  PostgreSQL          │  packages/flowFlex-backend/Infrastructure/     │
└──────────────────────┴──────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| ControllerBase | Success response wrapping, user ID extraction | `WebApi/Controllers/ControllerBase.cs` |
| AppIsolationMiddleware | Extract AppCode + TenantId from headers/JWT/query | `WebApi/Middlewares/AppIsolationMiddleware.cs` |
| TenantMiddleware | Ensure X-Tenant-Id header, Portal token tenant enforcement | `WebApi/Middlewares/TenantMiddleware.cs` |
| TokenValidatedHandler | 3 JWT scheme validation (local, IDM, ItemIAM) | `WebApi/Authentication/TokenValidatedHandler.cs` |
| WFEAuthorizeAttribute | Custom authorization attribute | `WebApi/Authorization/WFEAuthorizeAttribute.cs` |
| RequirePermissionAttribute | Action filter for permission checks | `WebApi/Filters/RequirePermissionAttribute.cs` |
| GlobalExceptionHandlingMiddleware | Catch-all exception handler | `Infrastructure/Exceptions/GlobalExceptionHandlingMiddleware.cs` |
| BaseRepository<T> | Generic CRUD operations via SqlSugar | `SqlSugarDB/BaseRepository.cs` |
| MigrationManager | SQL-file-based database migrations | `SqlSugarDB/Migrations/` |
| ServiceCollectionExtensions | SqlSugar registration, UserContext, global filters, AOP | `WebApi/Extensions/ServiceCollectionExtensions.cs` |

## Pattern Overview

**Overall:** Layered Architecture (Clean Architecture variant)

**Key Characteristics:**
- Dependency flows inward: WebApi → Application → Domain ← SqlSugarDB
- Application.Contracts defines interfaces; Application provides implementations
- Domain layer owns entity definitions and repository interfaces
- SqlSugarDB implements repositories against PostgreSQL
- Infrastructure provides cross-cutting concerns (logging, security, background tasks)

## Layers

**WebApi Layer:**
- Purpose: HTTP API surface, middleware pipeline, authentication/authorization
- Location: `packages/flowFlex-backend/WebApi/`
- Contains: Controllers, Middlewares, Filters, Authentication handlers, Program.cs
- Depends on: Application.Contracts (IServices), Domain.Shared (models/enums)
- Used by: Frontend SPA, external integrations

**Application Layer:**
- Purpose: Business logic, orchestration, mapping
- Location: `packages/flowFlex-backend/Application/`
- Contains: Service implementations, AutoMapper profiles, notification handlers, helpers
- Depends on: Application.Contracts, Domain (entities, repository interfaces)
- Used by: WebApi controllers via DI

**Application.Contracts Layer:**
- Purpose: Interface definitions, DTOs, options
- Location: `packages/flowFlex-backend/Application.Contracts/`
- Contains: IService interfaces, DTO classes, helper utilities, configuration options
- Depends on: Domain.Shared (enums, models)
- Used by: Application (implements), WebApi (consumes)

**Domain Layer:**
- Purpose: Entity definitions, repository interface contracts, filter abstractions
- Location: `packages/flowFlex-backend/Domain/`
- Contains: Entities, Repository interfaces, Abstract filter interfaces
- Depends on: Domain.Shared, SqlSugar (attributes only)
- Used by: Application, SqlSugarDB

**Domain.Shared Layer:**
- Purpose: Shared enums, constants, models, DI marker interfaces
- Location: `packages/flowFlex-backend/Domain.Shared/`
- Contains: Enums, Constants, Models (UserContext, AppContext), helpers, JSON converters
- Depends on: Nothing (leaf dependency)
- Used by: All layers

**SqlSugarDB Layer:**
- Purpose: ORM configuration, repository implementations, database migrations
- Location: `packages/flowFlex-backend/SqlSugarDB/`
- Contains: BaseRepository<T>, concrete repositories, migration scripts, context
- Depends on: Domain (IBaseRepository, entities), SqlSugar NuGet
- Used by: WebApi DI registration

**Infrastructure Layer:**
- Purpose: Cross-cutting concerns not tied to domain
- Location: `packages/flowFlex-backend/Infrastructure/`
- Contains: Global exception handling, background task queue, Redis cache, security utilities
- Depends on: Domain.Shared
- Used by: WebApi (middleware/service registration)

## Data Flow

### Primary API Request Path

1. HTTP Request → Kestrel (`WebApi/Program.cs`)
2. `GlobalExceptionHandlingMiddleware` catches unhandled exceptions (`Infrastructure/Exceptions/GlobalExceptionHandlingMiddleware.cs`)
3. `AppIsolationMiddleware` extracts AppCode + TenantId, creates AppContext (`WebApi/Middlewares/AppIsolationMiddleware.cs`)
4. `TenantMiddleware` validates/enforces tenant from Portal tokens (`WebApi/Middlewares/TenantMiddleware.cs`)
5. `FilterValidationMiddleware` ensures query filters are correctly applied (`WebApi/Middlewares/FilterValidationMiddleware.cs`)
6. JWT Authentication (one of 3 schemes) → `TokenValidatedHandler` populates `UserContext` (`WebApi/Authentication/TokenValidatedHandler.cs`)
7. Authorization → `WfeAuthorizationHandler` checks policies (`WebApi/Authorization/WfeAuthorizationHandler.cs`)
8. Controller action → calls `IService` method (`WebApi/Controllers/OW/`)
9. Service implementation → Repository call → SqlSugar → PostgreSQL (`Application/Services/OW/`)
10. AutoMapper transforms entity → DTO (`Application/Maps/`)
11. Controller returns `Success<T>(data)` wrapped in `SuccessResponse` envelope

### Authentication Flow

1. JWT token arrives in `Authorization: Bearer` header
2. ASP.NET Core tries all registered schemes (Bearer, IDM, ItemIAM)
3. On successful validation, `OnTokenValidated` event fires for matching scheme:
   - **Bearer (local JWT):** `OnTokenValidated` → lookup user by email → set UserContext (`TokenValidatedHandler.cs:33`)
   - **IDM:** `OnIdmTokenValidated` → call IdentityHub UserExtension API → populate UserContext with permissions/teams (`TokenValidatedHandler.cs:94`)
   - **ItemIAM:** `OnIamItemTokenValidated` → determine grant_type (password/client_credentials) → populate UserContext (`TokenValidatedHandler.cs:186`)
4. Client Credentials tokens: set `Schema = ItemIamClientIdentification`, `UserId = "0"`, bypass user-level permission checks

### Multi-Tenancy Data Isolation

1. Every entity inherits `ITenantFilter` (TenantId) and `IAppFilter` (AppCode) via `AbstractEntityBase`
2. SqlSugar global query filters automatically append `WHERE tenant_id = X AND app_code = Y`
3. Filters configured at repository level to avoid IServiceProvider disposal issues
4. To bypass filters (cross-tenant query): use `.ClearFilter()` or `QueryFilterClearAndBackup()`

**State Management:**
- `UserContext` is scoped per-request (registered as `AddScoped<UserContext>`)
- `AppContext` stored in `HttpContext.Items["AppContext"]`
- `ISqlSugarClient` registered as scoped (`SqlSugarScope` handles thread safety)

## Key Abstractions

**Entity Hierarchy:**
- `IdEntityBase`: Snowflake long ID, `ICloneable`, domain events support (`Domain/Entities/Base/IdEntityBase.cs`)
- `AbstractEntityBase` : IdEntityBase + `ITenantFilter` + `IAppFilter` (TenantId, AppCode) (`Domain/Entities/Base/AbstractEntityBase.cs`)
- `EntityBase` : AbstractEntityBase + `IValidFilter` (IsValid soft-delete flag) (`Domain/Entities/Base/EntityBase.cs`)
- `EntityBaseCreateInfo` : EntityBase + audit fields (CreateDate, ModifyDate, CreateBy, ModifyBy, CreateUserId, ModifyUserId) (`Domain/Entities/Base/EntityBaseCreateInfo.cs`)
- `OwEntityBase`: Standalone base with all fields inline (Id, TenantId, AppCode, IsValid, audit) (`Domain/Entities/Base/OwEntityBase.cs`)

**DI Auto-Registration:**
- Services implement marker interfaces: `IScopedService`, `ITransientService`, `ISingletonService` (`Domain.Shared/IDIService.cs`)
- Registration scanned at startup via `AddService()` extension

**Repository Pattern:**
- `IBaseRepository<T>`: Full CRUD + pagination + transactions (`Domain/Repository/IBaseRepository.cs`)
- `BaseRepository<T>`: SqlSugar implementation with `ISqlSugarClient` injection (`SqlSugarDB/BaseRepository.cs`)

## Entry Points

**Backend API:**
- Location: `packages/flowFlex-backend/WebApi/Program.cs`
- Triggers: HTTP requests (Kestrel)
- Responsibilities: DI configuration, middleware pipeline, auth schemes, Swagger, database init

**Frontend SPA:**
- Location: `packages/flowFlex-common/src/main.ts`
- Triggers: Browser navigation
- Responsibilities: Vue app bootstrap, router, Pinia stores, global components

## Authentication Schemes

| Scheme | Config Key | Token Validated Handler | Purpose |
|--------|-----------|----------------------|---------|
| Bearer (default) | `Security:JwtSecretKey` | `OnTokenValidated` | Local JWT for direct login |
| `Identification` (IDM) | `IdentityHubConfig` | `OnIdmTokenValidated` | IdentityHub SSO tokens |
| `ItemIamIdentification` | `ItemIamConfig` | `OnIamItemTokenValidated` | Item IAM (user + client_credentials) |

**Client Credentials bypass:**
- Schema set to `AuthSchemes.ItemIamClientIdentification`
- `UserId = "0"`, `SystemSource = SourceEnum.Client`
- `ControllerBase.GetCurrentUserId()` returns 0 for client tokens
- Permission checks skipped via `PermissionHelpers.IsClientCredentialsToken()`

## Workflow Domain Model

```text
┌────────────────────────────────────────────────────────────┐
│  Workflow (ff_workflow)                                      │
│  - Name, Status, Version, IsDefault, IsActive               │
│  - ViewPermissionMode, ViewTeams, OperateTeams              │
│  - VisibleInPortal, PortalPermission                        │
├────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Stage (ff_stage) [1:N via WorkflowId]              │   │
│  │  - Name, Order, Color, IsActive                      │   │
│  │  - ChecklistId, QuestionnaireId (component refs)     │   │
│  │  - DefaultAssignee, CoAssignees (JSONB)              │   │
│  │  - ViewPermissionMode, ViewTeams, OperateTeams       │   │
│  │  - components_json (JSONB - stage components config) │   │
│  │  - AttachmentManagementNeeded, Required              │   │
│  └────────────┬────────────────────────────────────────┘   │
│               │                                             │
│  ┌────────────▼────────────────────────────────────────┐   │
│  │  StageCondition (ff_stage_condition) [1:N via StageId]│  │
│  │  - RulesJson (Microsoft RulesEngine format)           │  │
│  │  - ActionsJson (GoToStage, SkipStage, SendNotif)     │  │
│  │  - FallbackStageId                                   │  │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│  Onboarding / Case (ff_onboarding)                          │
│  - WorkflowId, CurrentStageId, CurrentStageOrder            │
│  - CaseName, CaseCode, LeadEmail, ContactPerson             │
│  - stages_progress (JSONB - OnboardingStageProgress[])      │
├────────────────────────────────────────────────────────────┤
│  OnboardingStageProgress (embedded JSONB per stage)          │
│  - StageId, Status, IsCompleted, StartTime, CompletionTime  │
│  - EndTime (computed: CustomEndTime > StartTime+Days)        │
└────────────────────────────────────────────────────────────┘

Related Components:
- Checklist (ff_checklist) → ChecklistTask (ff_checklist_task) → ChecklistTaskCompletion
- Questionnaire (ff_questionnaire) → QuestionnaireSection → QuestionnaireAnswer
- InternalNote (ff_internal_note) - per-onboarding comments
- OnboardingFile (ff_onboarding_file) - attachments per stage
```

## Architectural Constraints

- **Threading:** Single-threaded per request (ASP.NET Core Kestrel). `SqlSugarScope` handles concurrent access. Background tasks use `BackgroundTaskQueue` (singleton).
- **Global state:** `UserContext` is scoped per-request. No module-level singletons with mutable state.
- **Circular imports:** None detected - layering is strict (WebApi → Application → Domain ← SqlSugarDB).
- **ID precision:** Snowflake `long` IDs exceed JavaScript `Number.MAX_SAFE_INTEGER`. `LongToStringConverter` serializes as strings. Frontend must treat IDs as strings.
- **JSON library coexistence:** Both `System.Text.Json` and `Newtonsoft.Json` are used. Controllers use Newtonsoft (configured in Program.cs). Use `System.Text.Json` for new code where possible.
- **Database column naming:** SqlSugar auto-converts PascalCase properties to snake_case via `UtilMethods.ToUnderLine()`. Table names prefixed with `ff_`.

## Anti-Patterns

### Bypassing Multi-Tenancy Filters

**What happens:** Using raw SqlSugar queries or forgetting filter context causes cross-tenant data leakage.
**Why it's wrong:** Silent data isolation breach - queries return data from all tenants.
**Do this instead:** Always rely on repository methods that have filters applied. Use `ClearFilter()` only when explicitly performing cross-tenant operations (e.g., admin dashboards). Reference: `SqlSugarDB/BaseRepository.cs` filter methods.

### Mixing OwEntityBase and EntityBaseCreateInfo

**What happens:** Some entities use `OwEntityBase` (standalone), others use the `EntityBaseCreateInfo` hierarchy.
**Why it's wrong:** Two parallel entity hierarchies with duplicated fields. Confusing which to extend.
**Do this instead:** For new entities, extend `EntityBaseCreateInfo` (the standard path). `OwEntityBase` is a legacy alternative used by `StageCondition` and similar.

## Error Handling

**Strategy:** Exception-based with global catch

**Patterns:**
- Business errors: throw `CRMException(ErrorCodeEnum, message)` from services
- Global handler: `GlobalExceptionHandlingMiddleware` catches all, returns structured error response (`Infrastructure/Exceptions/GlobalExceptionHandlingMiddleware.cs`)
- Auth errors: `context.Fail("message")` in token validation handlers
- Controller: never catches exceptions - let middleware handle them
- Frontend: Axios interceptors show `ElMessage.error()` for HTTP errors

## Cross-Cutting Concerns

**Logging:** Serilog (production), `ILogger<T>` injection, SQL logging via SqlSugar AOP (configurable via `Database:EnableSqlLogging`)

**Validation:** FluentValidation on request DTOs; model state suppressed (`SuppressModelStateInvalidFilter = true`) for manual handling

**Authentication:** Three JWT schemes registered simultaneously with priority ordering; `UserContext` populated on token validation

**Audit Fields:** Automatically populated via SqlSugar `Aop.DataExecuting` hook - sets CreateDate, ModifyDate, CreateBy, ModifyBy, CreateUserId, ModifyUserId on insert/update (`WebApi/Extensions/ServiceCollectionExtensions.cs:122`)

**Soft Deletes:** `IsValid = true` means active. Entities implement `IValidFilter`. SqlSugar global filter appends `WHERE is_valid = true`.

**Background Tasks:** `BackgroundTaskQueue` (singleton) + `BackgroundTaskService` (hosted service) for fire-and-forget operations (`Infrastructure/Services/BackgroundTaskQueue.cs`)

---

*Architecture analysis: 2026-06-08*
