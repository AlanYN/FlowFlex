---
inclusion: always
---
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Frontend (Vue.js)
```bash
# Development
pnpm dev                    # Start development server
pnpm serve                  # Start development server (alias)
pnpm serve:local           # Start with localhost config
pnpm serve:staging         # Start with staging config
pnpm serve:preview         # Start with preview config

# Build
pnpm build:production      # Build for production
pnpm build:preview         # Build for preview environment
pnpm build:development     # Build for development
pnpm build:stage           # Build for staging

# Code Quality
pnpm lint                  # Run all linters in parallel
pnpm lint:eslint          # Run ESLint with auto-fix
pnpm lint:prettier        # Format code with Prettier
pnpm lint:stylelint       # Lint and fix styles
pnpm type:check           # TypeScript type checking

# Testing
pnpm test                  # Run tests (if configured)
```

### Backend (.NET)
```bash
# Development
dotnet restore             # Restore NuGet packages
dotnet build              # Build the solution
dotnet run --project WebApi  # Run the WebApi project

# Testing
dotnet test               # Run all tests
dotnet test tests/Unit.Tests  # Run unit tests
dotnet test tests/Integration.Tests  # Run integration tests

# Database
# Run migrations from SqlSugarDB project
dotnet run --project SqlSugarDB migrate  # Apply migrations
```

## Architecture Overview

### Solution Structure
FlowFlex is a monorepo containing both frontend and backend applications:

- **Frontend**: Vue.js 3 SPA located in `packages/flowFlex-common/`
- **Backend**: .NET 8 Web API located in `packages/flowFlex-backend/`

### Backend Architecture
The backend follows Clean Architecture principles with clear layer separation:

1. **WebApi Layer** (`WebApi/`)
   - Entry point and API controllers
   - Middleware for authentication, exception handling, and app isolation
   - JWT-based authentication with multi-tenancy support

2. **Application Layer** (`Application/` & `Application.Contracts/`)
   - Business logic and service implementations
   - DTOs and service interfaces
   - AutoMapper profiles for entity-DTO mapping
   - Notification handlers and workflow helpers

3. **Domain Layer** (`Domain/` & `Domain.Shared/`)
   - Core business entities and domain logic
   - Repository interfaces
   - Shared enums, constants, and domain events
   - Value objects and domain-specific exceptions

4. **Infrastructure Layer** (`Infrastructure/`)
   - Cross-cutting concerns and technical implementations
   - Database configuration and extensions
   - Third-party service integrations

5. **Data Access Layer** (`SqlSugarDB/`)
   - SqlSugar ORM implementation
   - Repository implementations
   - Database migrations (timestamp-based)
   - PostgreSQL as primary database

### Frontend Architecture
Vue.js 3 application with TypeScript:

- **State Management**: Pinia with persisted state
- **Routing**: Vue Router for SPA navigation
- **UI Framework**: Element Plus + Tailwind CSS
- **API Communication**: Axios with interceptors
- **Build Tool**: Vite with auto-imports and component registration

### Key Technical Decisions

1. **Multi-Tenancy**: App-level isolation using `AppCode` field throughout entities
2. **Authentication**: JWT Bearer tokens with email-based authentication
3. **Database**: PostgreSQL with JSONB columns for flexible data storage
4. **ORM**: SqlSugar for database operations with custom filters
5. **API Documentation**: Swagger/OpenAPI for API documentation
6. **Validation**: FluentValidation for request validation
7. **Logging**: Serilog with structured logging

### Workflow System Components

The core workflow system consists of:
- **Workflows**: Main workflow definitions with stages
- **Stages**: Individual steps in a workflow with components
- **Components**: Questionnaires, Checklists, Actions linked to stages
- **Stage Progress**: Tracks user progress through workflow stages
- **Events**: Event sourcing for workflow state changes

### Database Conventions
- All tables prefixed with `ff_` (FlowFlex)
- Snowflake IDs for primary keys
- JSONB columns for flexible/dynamic data
- Soft deletes using `IsDeleted` flag
- Multi-tenancy via `AppCode` field
- Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`

### API Patterns
- RESTful API design
- Consistent response format with `ApiResponse<T>`
- Pagination support with `PagedResult<T>`
- Global exception handling
- Request/response validation

<!-- GSD:project-start source:PROJECT.md -->
## Project

**FlowFlex Question Number Type**

为 FlowFlex 问卷系统的 Question 配置增加 Number 类型选项。当前 Question Type 缺少数字类型，导致无法约束用户输入为数字。本次改动在前后端同时支持 Number 类型，前端限制只能输入数字，后端验证格式。

**Core Value:** 问卷字段能通过 Number 类型约束用户只输入数字，确保数据质量。

### Constraints

- **Tech stack**: 必须使用现有的 Vue 3 + .NET 8 技术栈
- **兼容性**: 不能破坏现有 Question Type 的行为
- **数据库**: 无需 migration，Number 类型作为枚举值添加即可
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# 12 (.NET 8) - Backend API, all server-side logic
- TypeScript 5.3 - Frontend SPA, all client-side logic
- SQL (PostgreSQL dialect) - Database migrations in `packages/flowFlex-backend/SqlSugarDB/Migrations/*.sql`
- SCSS - Frontend styles in `packages/flowFlex-common/src/styles/`
- HTML - Email templates in `packages/flowFlex-backend/Application/Templates/Email/*.html`
## Runtime
- .NET 8.0 (ASP.NET Core Web API)
- Docker container: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Exposed port: 8080
- Node.js >= 18.12.0
- Browser targets: > 1%, last 2 versions, not dead (ES2015 build target)
- Frontend: pnpm 8.10.0 (enforced via `preinstall` hook)
- Lockfile: `packages/flowFlex-common/pnpm-lock.yaml` (present)
- Backend: NuGet (local packages folder at `packages/flowFlex-backend/packages/`)
## Frameworks
- ASP.NET Core 8.0 - Web API framework (`packages/flowFlex-backend/WebApi/WebApi.csproj`)
- Clean Architecture with 6 layers: WebApi, Application, Application.Contracts, Domain, Domain.Shared, Infrastructure, SqlSugarDB
- Vue 3.5.12 - SPA framework (`packages/flowFlex-common/`)
- Vue Router 4.4.5 - Client-side routing
- Pinia 2.2.6 - State management with `pinia-plugin-persistedstate` 3.2.3
- Vite 5.4.18 - Frontend build tool (`packages/flowFlex-common/vite.config.ts`)
- Turbo 1.13.4 - Monorepo task runner
- Terser - Production minification (configured in `vite.config.ts`)
- Backend: xUnit 2.6.2 + Moq 4.20.70 + FluentAssertions 6.12.0 (`packages/flowFlex-backend/Tests/FlowFlex.Tests/`)
- Frontend: Jest 29.7.0 + `@vue/test-utils` 2.4.6 (`packages/flowFlex-common/jest.config.ts`)
## Key Dependencies
- `SqlSugarCore` 5.1.4.159-preview23 - Primary ORM for PostgreSQL (`SqlSugarDB/SqlSugarDB.csproj`)
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.2 - JWT auth (`WebApi/WebApi.csproj`)
- `AutoMapper` 12.0.1 - Entity-DTO mapping (`Application/Application.csproj`)
- `FluentValidation.AspNetCore` 11.3.0 - Request validation (`Application/Application.csproj`)
- `Hangfire.AspNetCore` 1.8.12 + `Hangfire.PostgreSql` 1.20.9 - Background job scheduling (`WebApi/WebApi.csproj`)
- `Serilog.AspNetCore` 8.0.3 - Structured logging (`WebApi/WebApi.csproj`)
- `Polly` 8.2.0 + `Polly.Extensions.Http` 3.0.0 - HTTP resilience/retry (`Infrastructure/Infrastructure.csproj`)
- `RulesEngine` 5.0.3 - Business rules evaluation (`WebApi/WebApi.csproj`, `Application.Contracts/Application.Contracts.csproj`)
- `Newtonsoft.Json` 13.0.3 - JSON serialization (`Infrastructure/Infrastructure.csproj`)
- `Scrutor` 5.0.2 - Assembly scanning for DI auto-registration (`WebApi/WebApi.csproj`)
- `DotLiquid` 2.2.717 - Template engine for email rendering (`Application/Application.csproj`)
- `BCrypt.Net-Next` 4.0.3 - Password hashing (`Application/Application.csproj`)
- `NETCore.Encrypt` 2.1.1 - AES encryption (`Infrastructure/Infrastructure.csproj`)
- `EPPlus` 8.0.5 + `ClosedXML` 0.104.1 + `MiniExcel` 1.36.0 - Excel processing (`Application/Application.csproj`)
- `DocumentFormat.OpenXml` 3.2.0 - Word/Office document processing
- `Item.BlobProvider` 8.0.0 - Blob/file storage abstraction
- `Item.Internal.Framework` 8.0.3 - Internal framework base
- `Item.Internal.Nacos` 8.0.0 - Nacos service discovery/config
- `Item.Internal.StandardApi` 8.0.4 - Standard API conventions
- `Item.Common.Lib` 8.0.7 - Shared utilities
- `Item.Message.RabbitMq` 8.0.0 - RabbitMQ messaging
- `Item.Message.Kafka` 8.0.4 - Kafka messaging (`Application.Contracts/Application.Contracts.csproj`)
- `Item.Redis` 8.0.1 - Redis client wrapper
- `Item.Email.Lib` 8.0.0 - Email sending
- `Item.Internal.Auth` 8.0.0 - Internal auth/IAM
- `Item.ThirdParty` 8.0.6 - Third-party integrations
- `Item.Excel.Lib` 8.0.2 - Excel utilities
- `element-plus` 2.9.1 - Primary UI component library
- `axios` 1.7.7 - HTTP client with interceptors (`src/app/apis/axios/`)
- `@vueuse/core` 10.11.1 - Vue composition utilities
- `vue-i18n` 9.14.1 - Internationalization
- `dayjs` 1.11.13 - Date manipulation
- `lodash-es` 4.17.21 - Utility functions
- `echarts` 5.5.1 + `chart.js` 4.4.6 - Data visualization
- `@vue-flow/core` 1.48.1 - Workflow canvas/diagram
- `pinia-plugin-persistedstate` 3.2.3 - Persisted Pinia stores
- `crypto-js` 4.2.0 - Client-side encryption
- `dompurify` 3.3.1 - XSS sanitization
- `markdown-it` 14.1.0 - Markdown rendering
- `@monaco-editor/loader` 1.5.0 - Code editor
- `vuedraggable` 4.1.0 - Drag-and-drop
- `gsap` 3.12.7 - Animations
- `xlsx-js-style` 1.2.0 - Excel export
- `jspdf` 3.0.1 + `html2canvas` 1.4.1 - PDF generation
- `unplugin-auto-import` 0.17.8 - Auto-imports for Vue/Pinia APIs
- `unplugin-vue-components` 0.27.4 - Auto component registration
- `unplugin-icons` 22.1.0 - Icon auto-import
- `tailwindcss` 3.4.14 - Utility CSS framework
- `eslint` 8.57.1 + `prettier` 3.0.3 - Linting and formatting
- `husky` 8.0.3 + `lint-staged` 15.2.10 - Git hooks
## Configuration
- `appsettings.json` - Base config (`packages/flowFlex-backend/WebApi/appsettings.json`)
- `appsettings.Development.json` - Dev overrides (DB, Redis, IDM, Outlook, BlobStore)
- `appsettings.{Environment}.json` - Per-environment overrides
- Key config sections: `Database`, `Redis`, `Cache`, `Security`, `Global`, `BlobStore`, `AI`, `Email`, `FileStorage`, `IdmApis`, `IdentityHubConfig`, `ItemIamConfig`, `OutlookApis`
- `.env` - Base vars (`packages/flowFlex-common/.env`)
- `.env.development`, `.env.localhost`, `.env.stage`, `.env.preview`, `.env.production` - Per-environment
- Key vars: `VITE_GLOB_API_URL`, `VITE_PROXY_URL`, `VITE_GLOB_IDM_URL`, `VITE_GLOB_DOMAIN_URL`, `VITE_GLOB_SSOURL`, `VITE_GLOB_CODE`, `VITE_USE_MOCK`, `VITE_BUILD_COMPRESS`
- `packages/flowFlex-common/vite.config.ts` - Vite build config
- `packages/flowFlex-common/tailwind.config.ts` - Tailwind theme (CSS variable-based)
- `packages/flowFlex-common/tsconfig.json` - TypeScript config
- `packages/flowFlex-backend/Dockerfile` - Docker multi-stage build (SDK 8.0 → aspnet 8.0 runtime)
## Platform Requirements
- Node.js >= 18.12.0
- pnpm >= 8.10.0
- .NET 8.0 SDK
- PostgreSQL instance
- Redis instance
- Docker (multi-stage build defined in `packages/flowFlex-backend/Dockerfile`)
- PostgreSQL database
- Redis cluster
- Blob storage: Aliyun OSS or AWS S3 (configurable via `Global.BlobStoreType`: Local/OSS/AWS)
- ASPNETCORE_ENVIRONMENT=Production, port 8080
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- API modules: camelCase, grouped by domain in `src/app/apis/<domain>/index.ts` (e.g., `src/app/apis/action/index.ts`)
- Vue components: PascalCase filenames (e.g., `ActionResultDialog.vue`, `ActionTag.vue`)
- Store modules: camelCase (e.g., `user.ts`, `workflowCanvas.ts`)
- Utility files: camelCase (e.g., `axiosCancel.ts`, `storageCache.ts`)
- Type definition files: camelCase, placed in `types/` at project root
- C# files: PascalCase matching the class name (e.g., `ActionExecutionService.cs`, `OnboardingStageManagementService.cs`)
- Test files: `<ClassName>Tests.cs` (e.g., `ActionExecutorTests.cs`, `WorkflowPermissionServiceTests.cs`)
- Map profiles: `<Entity>MapProfile.cs` (e.g., `OnboardingMapProfile.cs`)
- Repository interfaces: `I<Entity>Repository.cs`
- Service interfaces: `I<Name>Service.cs`
- Exported API functions: camelCase verbs (e.g., `addAction`, `getActionDefinitions`, `deleteAction`, `updateAction`)
- Vue composables/hooks: `use` prefix (e.g., `useUserStore`, `useGlobSetting`, `useI18n`)
- Event handlers: `handle` prefix (e.g., `handleClick`)
- Computed properties: descriptive nouns (e.g., `displayAction`, `isMultipleActions`, `sizeClasses`)
- Public methods: PascalCase with `Async` suffix for async methods (e.g., `ExecuteActionAsync`, `GetActionDefinitionAsync`)
- Private helpers: PascalCase (e.g., `CreateAxios`, `GetTransform`)
- Test methods: `MethodName_Scenario_ExpectedBehavior` (e.g., `ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess`)
- Frontend: camelCase (`actionsJson`, `dialogVisible`, `userContext`)
- Backend: camelCase for locals, `_camelCase` for private fields (e.g., `_actionDefinitionRepository`, `_logger`)
- Backend constants: PascalCase in static classes (e.g., `DefaultUserId`, `TeamA`)
- TypeScript interfaces: PascalCase (e.g., `ActionInfo`, `TestResult`, `Props`)
- Enums: PascalCase name, SCREAMING_SNAKE_CASE values (e.g., `ActionType.PYTHON_SCRIPT`)
- Const maps: SCREAMING_SNAKE_CASE (e.g., `ACTION_TYPE_MAPPING`, `FRONTEND_TO_BACKEND_TYPE_MAPPING`)
- Classes, interfaces, enums: PascalCase (e.g., `ActionExecutionService`, `IActionManagementService`, `ActionExecutionStatusEnum`)
- Enum names end with `Enum` suffix (e.g., `ViewPermissionModeEnum`, `OperationTypeEnum`)
- Namespaces mirror directory structure: `FlowFlex.<Layer>.<Domain>` (e.g., `FlowFlex.Application.Services.Action`)
## Code Style
- Tabs for indentation, tab width 4
- Print width: 100 characters
- Single quotes
- Trailing commas: ES5
- Semicolons: required
- End of line: auto
- HTML whitespace sensitivity: ignore
- Extends `@uni/eslint-config/strict`
- Vue HTML self-closing: void elements always, normal elements never, components always
- `@typescript-eslint/no-unused-vars`: error on all vars, ignore function args and rest siblings
- Duplicate enum values: disabled
- 4-space indentation
- `#region` / `#endregion` blocks used to group related methods (e.g., `#region Helper Methods`, `#region ExecuteActionsAsync Tests`)
- XML doc comments (`/// <summary>`) on all public classes and methods
- `ArgumentNullException` guard clauses in constructors for required dependencies
- `nullable enable` and `ImplicitUsings enable` in all projects
## Import Organization
- `@/` → `src/app/`
- `@assets/` → `src/assets/`
- `@apis/` → `src/app/apis/`
- `@components/` → `src/app/components/`
- `@hooks/` → `src/app/hooks/`
- `@models/` → `src/app/models/`
- `@stores/` → `src/app/stores/`
- `@utils/` → `src/app/utils/`
- `@views/` → `src/app/views/`
- `#/` → `types/` (type definitions)
- System namespaces first, then third-party, then internal `FlowFlex.*`
- No explicit ordering enforced beyond convention
## Error Handling
- Axios errors caught in `requestCatchHook` and `responseInterceptorsCatch` in `src/app/apis/axios/Axios.ts`
- Promise rejections propagate via `reject(e)` in the request wrapper
- HTTP status errors handled in `src/app/apis/axios/checkStatus.ts`
- UI errors shown via `ElMessage.error(...)` from Element Plus
- Services return domain objects or throw exceptions; controllers wrap in `Success(result)` or `NotFound()`
- Constructor guard clauses: `?? throw new ArgumentNullException(nameof(dep))`
- Structured logging with `_logger.LogWarning(...)` / `_logger.LogError(...)` using message templates
- Action execution results use a `Success`/`ErrorMessage` result object pattern rather than exceptions
## Logging
- Inject `ILogger<T>` via constructor
- Use structured logging with named placeholders: `_logger.LogWarning("Action definition not found: {ActionId}", actionDefinitionId)`
- Log at `Warning` for expected missing data, `Error` for unexpected failures
- Frontend has a `console.log('signal', conf.signal)` debug statement in `src/app/apis/axios/Axios.ts` (line 241) — not a pattern to follow
## Comments
- XML doc comments required on all public API surface (classes, methods, properties) in backend
- Inline comments for non-obvious logic, especially JSON parsing and permission checks
- Chinese-language inline comments are common throughout the codebase (both frontend and backend)
- Test methods use `// Arrange`, `// Act`, `// Assert` structure consistently
- Not used in frontend TypeScript files; type safety via TypeScript interfaces instead
- Backend uses XML `/// <summary>` / `/// <param>` / `/// <returns>` on public members
## Function Design
- Frontend API functions return `Promise<T>` via `defHttp.get/post/put/delete`
- Backend async methods return `Task<T>` or `Task`
- Action execution returns a result object with `Success`, `Details`, and `ErrorMessage` fields rather than throwing
## Module Design
- API modules export named functions directly (no default export)
- Stores use `defineStore` with a string ID prefixed `item-wfe-app-<name>`
- Barrel files (`index.ts`) used in API and store directories
- Services registered via DI; interfaces in `Application.Contracts/IServices/`
- Implementations in `Application/Services/`
- `IScopedService` marker interface used to auto-register scoped services (e.g., `OnboardingStageManagementService : IOnboardingStageManagementService, IScopedService`)
## Vue Component Conventions
- Composition API with `<script setup lang="ts">` — no Options API
- Props defined with TypeScript interfaces and `withDefaults(defineProps<Props>(), {...})`
- Emits typed with `defineEmits<{ eventName: [argType] }>()`
- Computed properties used extensively for derived display state
- Scoped SCSS styles with `<style scoped lang="scss">`
- Tailwind utility classes used inline alongside Element Plus components
- Template uses `v-if`/`v-else`, `:class` binding with arrays/objects, `@click.stop` for event isolation
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## System Overview
```text
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
- Dependency inversion: controllers depend on `IService` interfaces, not concrete implementations
- Auto-registration: services implement `IScopedService`, `ISingletonService`, or `ITransientService` marker interfaces for DI scanning
- Multi-tenancy: every entity carries `AppCode` + `TenantId`; `AppIsolationMiddleware` extracts these per-request
- Soft deletes: `IsValid` flag on all entities via `IValidFilter` interface; SqlSugar applies global filter
- Snowflake IDs: all primary keys are `long` generated via `SnowFlakeSingle.Instance.NextId()`
## Layers
- Purpose: HTTP entry point, authentication, authorization, middleware pipeline
- Location: `packages/flowFlex-backend/WebApi/`
- Contains: Controllers, Middlewares, Filters, Authentication handlers, Converters
- Depends on: Application.Contracts (IServices, DTOs)
- Used by: Frontend, external API consumers
- Purpose: Business logic, service orchestration, AutoMapper profiles
- Location: `packages/flowFlex-backend/Application/`
- Contains: Service implementations, Maps (AutoMapper profiles), Helpers, Notification handlers, Email templates
- Depends on: Application.Contracts, Domain, Infrastructure
- Used by: WebApi
- Purpose: Shared contracts between WebApi and Application
- Location: `packages/flowFlex-backend/Application.Contracts/`
- Contains: DTOs (organized by domain), IService interfaces, Options classes
- Depends on: Domain.Shared
- Used by: WebApi, Application
- Purpose: Core business entities and repository contracts
- Location: `packages/flowFlex-backend/Domain/`
- Contains: Entities (OW, Action, DynamicData, Integration), Repository interfaces, Abstracts
- Depends on: Domain.Shared only
- Used by: Application, SqlSugarDB
- Purpose: Data access implementation using SqlSugar ORM
- Location: `packages/flowFlex-backend/SqlSugarDB/`
- Contains: `BaseRepository<T>`, concrete repository implementations, migrations, context setup
- Depends on: Domain
- Used by: Application (via injected repository interfaces)
- Purpose: Cross-cutting technical concerns
- Location: `packages/flowFlex-backend/Infrastructure/`
- Contains: `GlobalExceptionHandlingMiddleware`, `ApplicationLogger`, `EncryptionService`, `BackgroundTaskQueue`, `BackgroundTaskService`
- Depends on: Domain.Shared
- Used by: WebApi, Application
## Data Flow
### Primary API Request Path
### Frontend Request Path
### Background Task Flow
- Pinia stores in `packages/flowFlex-common/src/app/stores/modules/` handle: `user`, `permission`, `locale`, `multipleTab`, `menuFunction`, `workflowCanvas`
- Persisted state via `pinia-plugin-persistedstate`
## Key Abstractions
- Purpose: Base class for all domain entities; provides `Id` (snowflake), `AppCode`, `TenantId`, `IsValid`, audit fields
- Examples: `packages/flowFlex-backend/Domain/Entities/Base/OwEntityBase.cs`, `packages/flowFlex-backend/Domain/Entities/Base/EntityBaseCreateInfo.cs`
- Pattern: All OW entities extend `EntityBaseCreateInfo` which extends `EntityBase` → `AbstractEntityBase`
- Purpose: Generic CRUD contract for all repositories
- Examples: `packages/flowFlex-backend/Domain/Repository/IBaseRepository.cs`
- Pattern: Domain defines interface; `BaseRepository<T>` in SqlSugarDB implements it; specific repos (e.g., `IWorkflowRepository`) extend it
- Purpose: Marker interfaces for automatic DI registration scanning
- Pattern: Services implement the appropriate marker; `AddService()` in `packages/flowFlex-backend/WebApi/Extensions/ServiceCollectionExtensions.cs` scans and registers
- Purpose: Entity ↔ DTO mapping
- Examples: `packages/flowFlex-backend/Application/Maps/WorkflowMapProfile.cs`, `packages/flowFlex-backend/Application/Maps/OnboardingMapProfile.cs`
- Pattern: One profile per aggregate root; registered explicitly in `Program.cs`
- Purpose: Per-request user identity (userId, email, tenantId, appCode)
- Location: `packages/flowFlex-backend/Domain/Shared/Models/`
- Pattern: Scoped service populated by middleware; injected into services via constructor
## Entry Points
- Location: `packages/flowFlex-backend/WebApi/Program.cs`
- Triggers: Kestrel HTTP server
- Responsibilities: DI registration, middleware pipeline, database initialization, hosted services startup
- Location: `packages/flowFlex-common/src/main.ts`
- Triggers: Browser load or Wujie micro-app mount (`window.__WUJIE_MOUNT`)
- Responsibilities: App bootstrap, i18n setup, Pinia store setup, router guard setup, Element Plus registration
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
### Distributed cache fallback to memory in production
### Token blacklist validation disabled
## Error Handling
- `GlobalExceptionHandlingMiddleware` in `packages/flowFlex-backend/Infrastructure/Exceptions/` converts exceptions to structured JSON responses
- Services throw `CRMException(ErrorCodeEnum, message)` for business rule violations
- Controllers use `Success<T>(data)` helper from `packages/flowFlex-backend/WebApi/Controllers/ControllerBase.cs` — never return raw `Ok()`
- Frontend `transformResponseHook` in `packages/flowFlex-common/src/app/apis/axios/index.ts` handles non-success codes and shows `ElMessage` errors
## Cross-Cutting Concerns
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

| Skill | Description | Path |
|-------|-------------|------|
| api-design |  | `.claude/skills/api-design/SKILL.md` |
| architecture-confirm |  | `.claude/skills/architecture-confirm/SKILL.md` |
| auto-test-runner | Execute test cases against a codebase and collect structured results. Use when test cases are ready and need to be run, when verifying a fix resolves a failing test, or when generating a test execution report. | `.claude/skills/auto-test-runner/SKILL.md` |
| capability-evolver | A self-evolution engine for AI agents. Analyzes runtime history to identify improvements and applies protocol-constrained evolution. | `.claude/skills/capability-evolver/SKILL.md` |
| competitive-analysis | Analyze competitors with feature comparison matrices, positioning analysis, and strategic implications. Use when researching a competitor, comparing product capabilities, assessing competitive positioning, or preparing a competitive brief for product strategy. | `.claude/skills/competitor-analysis/SKILL.md` |
| component-breakdown |  | `.claude/skills/component-breakdown/SKILL.md` |
| C# | Write robust C# avoiding null traps, async deadlocks, and LINQ pitfalls. | `.claude/skills/csharp/SKILL.md` |
| database-design |  | `.claude/skills/database-design/SKILL.md` |
| design-token-spec | Generate a design token specification (colors, typography, spacing, radius, shadows) based on product type and brand style description. Use when UX needs to define a consistent visual language before Dev implements the UI. Integrates with ui-ux-pro-max for data-driven recommendations. | `.claude/skills/design-token-spec/SKILL.md` |
| dev-environment-setup |  | `.claude/skills/dev-environment-setup/SKILL.md` |
| domain-splitter | Analyze large or complex project requirements and split them into bounded domains (DDD-style). Use when a project spans multiple business capabilities, teams, or data ownership boundaries. Produces a domain map with confirmation before proceeding. | `.claude/skills/domain-splitter/SKILL.md` |
| feature-spec | Write structured product requirements documents (PRDs) with problem statements, user stories, requirements, and success metrics. Use when speccing a new feature, writing a PRD, defining acceptance criteria, prioritizing requirements, or documenting product decisions. | `.claude/skills/feature-spec/SKILL.md` |
| file-structure-generator |  | `.claude/skills/file-structure-generator/SKILL.md` |
| find-skills | Helps users discover and install agent skills when they ask questions like "how do I do X", "find a skill for X", "is there a skill that can...", or express interest in extending capabilities. This skill should be used when the user is looking for functionality that might exist as an installable skill. | `.claude/skills/find-skills/SKILL.md` |
| flowchart-generator | Convert a text description of a process or user flow into a Mermaid flowchart. Use when BA needs to generate business flow diagrams or UX needs to generate user flow diagrams. | `.claude/skills/flowchart-generator/SKILL.md` |
| Item API Layer | 'Item EAG API integration patterns. defHttp usage, axios configuration, request/response types, useGlobSetting for dynamic versioning. Actions: API integration, HTTP requests, axios setup, endpoint definition.' | `.claude/skills/item-api-layer-react/SKILL.md` |
| Item Vue API Layer | 'Item EAG Vue API integration patterns. defHttp usage, axios configuration, request/response types, useGlobSetting for dynamic versioning. Actions: API integration, HTTP requests, axios setup, endpoint definition.' | `.claude/skills/item-api-layer-vue/SKILL.md` |
| Item Components | 'Item EAG UI component library. Table with resizable columns, Loading/WaveLoading, shadcn/ui component usage patterns. Actions: build component, create UI, implement table, add loading state, use shadcn components.' | `.claude/skills/item-components-react/SKILL.md` |
| Item Vue Components | 'Item EAG Vue UI component library. Element Plus usage, custom Vue SFC components, composable patterns. Actions: build component, create UI, implement table, add loading state, use Element Plus components.' | `.claude/skills/item-components-vue/SKILL.md` |
| Item Type Patterns | 'Item EAG TypeScript type patterns. Namespace exports, #/ alias imports, router types, axios request/response types, user store types. Actions: type definition, TypeScript, interface, namespace, type import.' | `.claude/skills/item-type-patterns-react/SKILL.md` |
| Item Vue Type Patterns | 'Item EAG Vue TypeScript type patterns. Namespace exports, #/ alias imports, router types, axios request/response types, Pinia store types. Actions: type definition, TypeScript, interface, namespace, type import.' | `.claude/skills/item-type-patterns-vue/SKILL.md` |
| user-research-synthesis | Synthesize qualitative and quantitative user research into structured insights and opportunity areas. Use when analyzing interview notes, survey responses, support tickets, or behavioral data to identify themes, build personas, or prioritize opportunities. | `.claude/skills/market-research/SKILL.md` |
| pptx | "Use this skill any time a .pptx file is involved in any way — as input, output, or both. This includes: creating slide decks, pitch decks, or presentations; reading, parsing, or extracting text from any .pptx file (even if the extracted content will be used elsewhere, like in an email or summary); editing, modifying, or updating existing presentations; combining or splitting slide files; working with templates, layouts, speaker notes, or comments. Trigger whenever the user mentions \"deck,\" \"slides,\" \"presentation,\" or references a .pptx filename, regardless of what they plan to do with the content afterward. If a .pptx file needs to be opened, created, or touched, use this skill." | `.claude/skills/pptx/SKILL.md` |
| priority-matrix | Classify a list of requirements using the MoSCoW method (Must/Should/Could/Won't). Use when PM needs to determine delivery scope and make trade-off decisions, especially when resources or time are constrained. | `.claude/skills/priority-matrix/SKILL.md` |
| proactive-agent | "Transform AI agents from task-followers into proactive partners that anticipate needs and continuously improve. Now with WAL Protocol, Working Buffer, Autonomous Crons, and battle-tested patterns. Part of the Hal Stack 🦞" | `.claude/skills/proactive-agent/SKILL.md` |
| requirements-clarify | Identify and resolve ambiguous requirements in a PM Spec or user description. Use when BA encounters vague quantities, incomplete conditions, undefined terms, missing boundaries, or unclear subjects. Outputs a structured ambiguity list with suggested definitions for user confirmation. | `.claude/skills/requirements-clarify/SKILL.md` |
| risk-analysis | Identify technical risks and generate mitigation strategies for a project based on its architecture, tech stack, and requirements. Use when Dev needs to surface implementation risks before task breakdown, especially for high-complexity or high-stakes features. | `.claude/skills/risk-analysis/SKILL.md` |
| task-breakdown | Decompose user stories and technical requirements into structured, independently deliverable development tasks. Use when Dev needs to convert a confirmed Dev Spec into an actionable task list with priority, complexity, and dependency mapping. | `.claude/skills/task-breakdown/SKILL.md` |
| tavily | AI-optimized web search via Tavily API. Returns concise, relevant results for AI agents. | `.claude/skills/tavily-search/SKILL.md` |
| tech-stack-advisor | Recommend a technology stack based on project requirements, team constraints, and non-functional requirements. Use when Dev needs to make and justify technology choices for frontend, backend, database, and infrastructure with documented rationale and trade-offs. | `.claude/skills/tech-stack-advisor/SKILL.md` |
| test-case-generator | Generate structured test cases from BA acceptance criteria and Dev risk register. Use when QA needs to convert every AC into at least one verifiable test case with preconditions, steps, and expected results. Covers happy path, error cases, and edge cases for high-risk items. | `.claude/skills/test-case-generator/SKILL.md` |
| ui-ux-pro-max |  | `.claude/skills/ui-ux-pro-max/SKILL.md` |
| user-story-writer | Generate structured user stories with acceptance criteria from a feature description or PM Spec scope item. Use when BA needs to convert functional requirements into standard "As a / I want / So that" format with testable GIVEN/WHEN/THEN acceptance criteria. | `.claude/skills/user-story-writer/SKILL.md` |
| using-superpowers | Use when starting any conversation - establishes how to find and use skills, requiring Skill tool invocation before ANY response including clarifying questions | `.claude/skills/using-superpowers/SKILL.md` |
| wireframe-generator | Generate text-based wireframe descriptions and ASCII layout sketches for key pages based on user stories. Use when UX needs to document page structure, key elements, and layout before visual design begins. | `.claude/skills/wireframe-generator/SKILL.md` |
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
