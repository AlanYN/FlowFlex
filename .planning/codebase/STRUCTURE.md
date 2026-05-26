# Codebase Structure

**Analysis Date:** 2026-05-25

## Directory Layout

```
FlowFlex/                              # Monorepo root
├── packages/
│   ├── flowFlex-backend/              # .NET 8 Web API (Clean Architecture)
│   │   ├── WebApi/                    # HTTP entry point, controllers, middleware
│   │   │   ├── Controllers/           # API controllers grouped by domain
│   │   │   │   ├── OW/                # Onboarding Workflow controllers
│   │   │   │   ├── AI/                # AI feature controllers
│   │   │   │   ├── Action/            # Action controllers
│   │   │   │   ├── DynamicData/       # Dynamic data controllers
│   │   │   │   ├── Integration/       # Integration controllers
│   │   │   │   ├── MessageCenter/     # Message center controllers
│   │   │   │   ├── Shared/            # Shared/utility controllers
│   │   │   │   └── ControllerBase.cs  # Base controller with Success<T>() helpers
│   │   │   ├── Middlewares/           # Request pipeline middleware
│   │   │   ├── Authentication/        # JWT token validation handlers
│   │   │   ├── Authorization/         # WFEAuthorize attribute, policy handlers
│   │   │   ├── Filters/               # Action filters (e.g., IntegrationApiLogFilter)
│   │   │   ├── Converters/            # JSON converters (LongToString, DateTimeOffset)
│   │   │   ├── Extensions/            # IServiceCollection extensions, DI wiring
│   │   │   ├── Program.cs             # App entry point, DI registration, pipeline
│   │   │   └── appsettings.json       # Configuration (DB, JWT, Redis, AI, etc.)
│   │   │
│   │   ├── Application/               # Business logic implementations
│   │   │   ├── Services/
│   │   │   │   ├── OW/                # Onboarding Workflow services
│   │   │   │   │   ├── OnboardingServices/  # Onboarding split into 9 focused services
│   │   │   │   │   ├── StageCondition/      # Stage condition evaluation
│   │   │   │   │   ├── ChangeLog/           # Change log services
│   │   │   │   │   ├── Permission/          # Permission services
│   │   │   │   │   └── Extensions/          # OW service extension methods
│   │   │   │   ├── AI/                # AI services (Chat, Checklist, Workflow, etc.)
│   │   │   │   ├── Action/            # Action executor services
│   │   │   │   ├── DynamicData/       # Dynamic data services
│   │   │   │   ├── Integration/       # External integration services
│   │   │   │   └── MessageCenter/     # Email/Outlook sync services
│   │   │   ├── Maps/                  # AutoMapper profiles (one per aggregate)
│   │   │   ├── Helpers/               # Application-level helpers
│   │   │   ├── Notification/          # MediatR notification handlers
│   │   │   ├── Client/                # External HTTP clients (IDM, etc.)
│   │   │   ├── Filter/                # Application-level filters
│   │   │   └── Templates/Email/       # Email HTML templates
│   │   │
│   │   ├── Application.Contracts/     # Shared contracts (no implementation)
│   │   │   ├── Dtos/                  # Data Transfer Objects
│   │   │   │   ├── OW/                # OW DTOs (Workflow, Stage, Checklist, etc.)
│   │   │   │   ├── AI/                # AI DTOs
│   │   │   │   ├── Action/            # Action DTOs
│   │   │   │   ├── DynamicData/       # Dynamic data DTOs
│   │   │   │   ├── Integration/       # Integration DTOs
│   │   │   │   └── Shared/            # Shared/common DTOs
│   │   │   ├── IServices/             # Service interfaces
│   │   │   │   ├── OW/                # OW service interfaces
│   │   │   │   ├── AI/                # AI service interfaces
│   │   │   │   ├── Action/            # Action service interfaces
│   │   │   │   ├── DynamicData/       # Dynamic data service interfaces
│   │   │   │   └── Integration/       # Integration service interfaces
│   │   │   ├── Options/               # Configuration options classes
│   │   │   └── Helpers/               # Shared helper utilities
│   │   │
│   │   ├── Domain/                    # Core domain (no external dependencies)
│   │   │   ├── Entities/
│   │   │   │   ├── Base/              # Base entity classes (EntityBase, OwEntityBase, etc.)
│   │   │   │   ├── OW/                # Onboarding Workflow entities
│   │   │   │   ├── Action/            # Action entities
│   │   │   │   ├── DynamicData/       # Dynamic data entities
│   │   │   │   ├── Integration/       # Integration entities
│   │   │   │   └── Shared/            # Shared entities
│   │   │   ├── Repository/            # Repository interfaces
│   │   │   │   ├── IBaseRepository.cs # Generic CRUD interface
│   │   │   │   └── OW/                # OW-specific repository interfaces
│   │   │   ├── Abstracts/             # IValidFilter, ISoftDeletable, ITenantFilter
│   │   │   ├── Manager/               # Domain managers
│   │   │   └── Shared/                # Domain.Shared (enums, constants, models)
│   │   │
│   │   ├── Domain.Shared/             # Shared domain primitives
│   │   │   ├── Enums/                 # Domain enumerations
│   │   │   └── Constants/             # Domain constants
│   │   │
│   │   ├── SqlSugarDB/                # Data access layer
│   │   │   ├── BaseRepository.cs      # Generic repository implementation
│   │   │   ├── Repositories/          # Concrete repository implementations
│   │   │   │   └── OW/                # OW repository implementations
│   │   │   ├── Context/               # SqlSugar DB context setup
│   │   │   ├── Extensions/            # DB initialization extensions
│   │   │   ├── Migrations/            # SQL migration files (timestamp-named)
│   │   │   └── Tools/                 # DB tooling utilities
│   │   │
│   │   ├── Infrastructure/            # Cross-cutting concerns
│   │   │   ├── Exceptions/            # GlobalExceptionHandlingMiddleware
│   │   │   ├── Extensions/            # ServiceCollectionExtensions, LoggingExtensions
│   │   │   ├── Services/              # BackgroundTaskQueue, ApplicationLogger, EncryptionService
│   │   │   ├── Configuration/         # Options classes (DatabaseOptions, SecurityOptions, etc.)
│   │   │   └── CodeGenerator/         # Code generation utilities
│   │   │
│   │   ├── Tests/FlowFlex.Tests/      # Test project
│   │   ├── Database/Migrations/       # Additional database migration scripts
│   │   ├── Docs/                      # Backend API documentation
│   │   ├── FlowFlex.sln               # .NET solution file
│   │   └── Dockerfile                 # Container build definition
│   │
│   └── flowFlex-common/               # Vue 3 SPA frontend
│       └── src/
│           ├── main.ts                # App bootstrap, Wujie micro-app support
│           ├── App.vue                # Root Vue component
│           ├── app/
│           │   ├── apis/              # HTTP client modules
│           │   │   ├── axios/         # Axios instance, interceptors, token refresh
│           │   │   ├── ow/            # OW API calls (onboarding, checklist, etc.)
│           │   │   ├── ai/            # AI API calls
│           │   │   ├── action/        # Action API calls
│           │   │   ├── dashboard/     # Dashboard API calls
│           │   │   ├── integration/   # Integration API calls
│           │   │   └── messageCenter/ # Message center API calls
│           │   ├── views/             # Page-level components
│           │   │   ├── onboard/       # Onboarding views (list, checklist, questionnaire, workflow)
│           │   │   ├── dashboard/     # Dashboard views
│           │   │   ├── actions/       # Actions views
│           │   │   ├── dynamicFields/ # Dynamic fields views
│           │   │   ├── messageCenter/ # Message center views
│           │   │   ├── integration-settings/ # Integration settings views
│           │   │   ├── authorityManagement/  # Permission management views
│           │   │   ├── login/         # Login views
│           │   │   └── error/         # Error pages
│           │   ├── components/        # Shared UI components
│           │   │   ├── form/          # Form input components
│           │   │   ├── global/        # Global reusable components
│           │   │   ├── layout/        # Layout shell components
│           │   │   ├── sidebar/       # Sidebar navigation
│           │   │   └── workflow-canvas/ # Workflow visual canvas (nodes, panels)
│           │   ├── stores/            # Pinia state stores
│           │   │   ├── modules/       # user, permission, locale, multipleTab, menuFunction, workflowCanvas
│           │   │   └── plugin/        # Store plugins
│           │   ├── router/            # Vue Router
│           │   │   ├── routers/modules/ # Route modules per feature
│           │   │   └── guard/         # Navigation guards, dynamic route creation
│           │   ├── hooks/             # Composables (useI18n, useWujie, etc.)
│           │   ├── utils/             # Utility functions (auth, cache, helpers)
│           │   ├── enums/             # Frontend enumerations
│           │   ├── config/            # App configuration
│           │   ├── settings/          # Global settings (useGlobSetting)
│           │   ├── logics/            # Business logic composables
│           │   └── mitt/              # Event bus
│           ├── assets/                # Static assets (fonts, images, SVGs)
│           ├── locales/               # i18n translations (en, zh-CN)
│           ├── styles/                # Global styles, design tokens, Element Plus overrides
│           └── types/                 # TypeScript type declarations
│
├── .claude/                           # Claude AI skills and specs
│   ├── skills/                        # Reusable skill definitions
│   └── specs/                         # Feature specifications
├── .planning/codebase/                # Codebase analysis documents (this directory)
├── docs/                              # Project-level documentation
└── CLAUDE.md                          # Project instructions for Claude
```

## Directory Purposes

**`packages/flowFlex-backend/WebApi/Controllers/OW/`:**
- Purpose: REST API endpoints for the Onboarding Workflow domain
- Contains: `WorkflowController`, `StageController`, `OnboardingController`, `ChecklistController`, `QuestionnaireController`, `QuestionnaireAnswerController`, `UserController`, `PermissionController`, and more
- Key files: `packages/flowFlex-backend/WebApi/Controllers/ControllerBase.cs` (base with `Success<T>()` helper)

**`packages/flowFlex-backend/Application/Services/OW/OnboardingServices/`:**
- Purpose: Onboarding business logic split into focused service classes
- Contains: `OnboardingService.cs` (facade), `OnboardingCrudService.cs`, `OnboardingQueryService.cs`, `OnboardingStageManagementService.cs`, `OnboardingStageProgressService.cs`, `OnboardingStatusService.cs`, `OnboardingPermissionService.cs`, `OnboardingHelperService.cs`, `OnboardingUserManagementService.cs`

**`packages/flowFlex-backend/Domain/Entities/Base/`:**
- Purpose: Entity base class hierarchy
- Key files: `OwEntityBase.cs` (snowflake ID, AppCode, TenantId, audit fields), `EntityBaseCreateInfo.cs` (extends EntityBase with ISoftDeletable), `EntityBase.cs` (adds IsValid), `AbstractEntityBase.cs`

**`packages/flowFlex-backend/SqlSugarDB/Migrations/`:**
- Purpose: Timestamp-named SQL migration files applied at startup
- Generated: No (hand-authored)
- Committed: Yes

**`packages/flowFlex-common/src/app/apis/axios/`:**
- Purpose: Axios HTTP client configuration with interceptors
- Key files: `index.ts` (creates configured Axios instance), `axiosTransform.ts` (request/response transform), `tokenRefresh.ts` (JWT refresh logic), `axiosCancel.ts` (request deduplication)

## Key File Locations

**Entry Points:**
- `packages/flowFlex-backend/WebApi/Program.cs`: Backend startup, DI, middleware pipeline
- `packages/flowFlex-common/src/main.ts`: Frontend bootstrap, Wujie micro-app lifecycle

**Configuration:**
- `packages/flowFlex-backend/WebApi/appsettings.json`: Database, JWT, Redis, AI, Email, FileStorage config keys
- `packages/flowFlex-common/.env.development`: Frontend environment variables

**Core Logic:**
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs`: Workflow CRUD and business rules
- `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingService.cs`: Onboarding facade
- `packages/flowFlex-backend/SqlSugarDB/BaseRepository.cs`: Generic data access implementation
- `packages/flowFlex-backend/WebApi/Middlewares/AppIsolationMiddleware.cs`: Multi-tenancy context extraction

**DI Registration:**
- `packages/flowFlex-backend/WebApi/Extensions/ServiceCollectionExtensions.cs`: SqlSugar, repositories, scoped services
- `packages/flowFlex-backend/Infrastructure/Extensions/ServiceCollectionExtensions.cs`: Infrastructure services

**Testing:**
- `packages/flowFlex-backend/Tests/FlowFlex.Tests/`: Single test project

## Naming Conventions

**Backend Files:**
- Entities: `PascalCase.cs` matching table name without `ff_` prefix (e.g., `Workflow.cs` → `ff_workflow`)
- Services: `{Entity}Service.cs` (e.g., `WorkflowService.cs`)
- Service interfaces: `I{Entity}Service.cs` (e.g., `IWorkflowService.cs`)
- Repository interfaces: `I{Entity}Repository.cs` (e.g., `IWorkflowRepository.cs`)
- DTOs: `{Entity}Dto.cs`, `{Entity}InputDto.cs`, `{Entity}OutputDto.cs`
- Controllers: `{Entity}Controller.cs`
- AutoMapper profiles: `{Entity}MapProfile.cs`

**Database:**
- Tables: `ff_` prefix + snake_case (e.g., `ff_workflow`, `ff_onboarding_stage_progress`)
- Columns: snake_case (auto-converted from PascalCase by SqlSugar `ToUnderLine()`)
- Primary keys: `id` (snowflake long)
- Audit columns: `create_date`, `modify_date`, `create_by`, `modify_by`, `create_user_id`, `modify_user_id`
- Soft delete: `is_valid` (bool)
- Multi-tenancy: `app_code`, `tenant_id`

**Frontend Files:**
- Vue components: `PascalCase.vue` or `kebab-case.vue`
- TypeScript modules: `camelCase.ts`
- API modules: `kebab-case.ts` matching feature name (e.g., `onboarding.ts`, `change-log.ts`)
- Store modules: `camelCase.ts` (e.g., `user.ts`, `permission.ts`)
- Route modules: `camelCase.ts` (e.g., `workflow.ts`, `onboard.ts`)

## Where to Add New Code

**New Backend Feature (e.g., new OW entity):**
1. Domain entity: `packages/flowFlex-backend/Domain/Entities/OW/{Entity}.cs` — extend `EntityBaseCreateInfo`
2. Repository interface: `packages/flowFlex-backend/Domain/Repository/OW/I{Entity}Repository.cs` — extend `IBaseRepository<{Entity}>`
3. Repository implementation: `packages/flowFlex-backend/SqlSugarDB/Repositories/OW/{Entity}Repository.cs` — extend `BaseRepository<{Entity}>`
4. DTOs: `packages/flowFlex-backend/Application.Contracts/Dtos/OW/{Entity}/{Entity}Dto.cs` and `{Entity}InputDto.cs`
5. Service interface: `packages/flowFlex-backend/Application.Contracts/IServices/OW/I{Entity}Service.cs`
6. Service implementation: `packages/flowFlex-backend/Application/Services/OW/{Entity}Service.cs` — implement `I{Entity}Service, IScopedService`
7. AutoMapper profile: `packages/flowFlex-backend/Application/Maps/{Entity}MapProfile.cs` — register in `Program.cs`
8. Controller: `packages/flowFlex-backend/WebApi/Controllers/OW/{Entity}Controller.cs` — extend `Controllers.ControllerBase`
9. Migration: `packages/flowFlex-backend/SqlSugarDB/Migrations/{timestamp}_{description}.sql`

**New Frontend Feature:**
1. API module: `packages/flowFlex-common/src/app/apis/{domain}/{feature}.ts`
2. View: `packages/flowFlex-common/src/app/views/{feature}/index.vue`
3. Route: add to `packages/flowFlex-common/src/app/router/routers/modules/{feature}.ts`
4. Store (if needed): `packages/flowFlex-common/src/app/stores/modules/{feature}.ts`

**New Shared UI Component:**
- Reusable across views: `packages/flowFlex-common/src/app/components/global/{ComponentName}/index.vue`
- Form input: `packages/flowFlex-common/src/app/components/form/{componentName}/`

**New AI Service:**
- Implementation: `packages/flowFlex-backend/Application/Services/AI/{Feature}/`
- Interface: `packages/flowFlex-backend/Application.Contracts/IServices/AI/I{Feature}Service.cs`
- Base class: extend `AIServiceBase` at `packages/flowFlex-backend/Application/Services/AI/AIServiceBase.cs`

**Utilities:**
- Backend shared helpers: `packages/flowFlex-backend/Application/Helpers/`
- Frontend shared utils: `packages/flowFlex-common/src/app/utils/`

## Special Directories

**`.claude/skills/`:**
- Purpose: Reusable Claude AI skill definitions for this project
- Generated: No
- Committed: Yes

**`.planning/codebase/`:**
- Purpose: Codebase analysis documents consumed by GSD planning commands
- Generated: Yes (by `/gsd-map-codebase`)
- Committed: Yes

**`packages/flowFlex-backend/SqlSugarDB/Migrations/`:**
- Purpose: SQL migration files applied at startup via `InitializeDatabase()`
- Generated: No (hand-authored)
- Committed: Yes

**`packages/flowFlex-backend/WebApi/wwwroot/uploads/`:**
- Purpose: Local file upload storage (dev only; production uses cloud storage)
- Generated: Yes (created at runtime)
- Committed: No

**`packages/flowFlex-backend/bin/` and `obj/`:**
- Purpose: .NET build output
- Generated: Yes
- Committed: No

---

*Structure analysis: 2026-05-25*
