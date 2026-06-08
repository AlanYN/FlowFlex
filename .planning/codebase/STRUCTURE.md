# Codebase Structure

**Analysis Date:** 2026-06-08

## Directory Layout

```
FlowFlex/
├── packages/
│   ├── flowFlex-backend/          # .NET 8 backend (WebApi + layered architecture)
│   └── flowFlex-common/           # Vue 3 SPA frontend
├── crm-api-integration/           # CRM API integration specs/docs
├── wfe-api-integration/           # WFE API integration specs/docs
├── ontology-work/                 # Ontology/domain modeling work
├── docs/                          # Project documentation
├── scripts/                       # Utility scripts
├── specs/                         # Feature specifications
├── .planning/                     # GSD planning artifacts
│   └── codebase/                  # Codebase analysis documents
├── CLAUDE.md                      # AI assistant project instructions
├── README.md                      # Project readme
└── logo.png                       # Project logo
```

## Backend Structure

```
packages/flowFlex-backend/
├── WebApi/                        # ASP.NET Core Web API host
│   ├── Program.cs                 # App bootstrap, DI, middleware pipeline
│   ├── Controllers/               # API controllers by domain
│   │   ├── ControllerBase.cs      # Abstract base (Success<T>, GetCurrentUserId)
│   │   ├── OW/                    # Core workflow/onboarding controllers
│   │   ├── AI/                    # AI generation controllers
│   │   ├── Action/                # Action/automation controllers
│   │   ├── Integration/           # External integration controllers
│   │   ├── DynamicData/           # Dynamic field/property controllers
│   │   ├── Admin/                 # Admin-only controllers
│   │   ├── Shared/                # Shared/utility controllers
│   │   └── HealthController.cs    # Health check endpoint
│   ├── Authentication/            # Token validation handlers
│   ├── Authorization/             # Custom auth attributes/handlers
│   ├── Middlewares/               # Request pipeline middlewares
│   ├── Filters/                   # Action filters (permissions, rate limit, logging)
│   ├── Extensions/                # Service registration, MVC config
│   ├── Converters/                # JSON converters (LongToString)
│   ├── Routes/                    # Route configuration
│   ├── Model/                     # Request/response view models
│   └── appsettings.json           # App configuration
│
├── Application/                   # Business logic implementations
│   ├── Services/                  # Service implementations by domain
│   │   ├── OW/                    # Core services (Workflow, Stage, Onboarding, etc.)
│   │   │   ├── OnboardingServices/  # Onboarding sub-services (CRUD, Query, Permission, etc.)
│   │   │   ├── Permission/        # Permission services (Case, Stage, Workflow)
│   │   │   ├── StageCondition/    # Condition engine services
│   │   │   ├── ChangeLog/         # Operation change log services
│   │   │   └── Extensions/        # Service extension methods
│   │   ├── AI/                    # AI workflow/questionnaire generation
│   │   ├── Action/                # Action execution services
│   │   ├── Integration/           # External integration services
│   │   ├── MessageCenter/         # Email/Outlook sync services
│   │   └── DynamicData/           # Dynamic field services
│   ├── Maps/                      # AutoMapper profiles
│   ├── Notification/              # MediatR notification handlers
│   ├── Filter/                    # Application-level filters/attributes
│   ├── Helpers/                   # Utility helpers
│   ├── Client/                    # External HTTP clients
│   └── Templates/                 # Email/notification templates
│
├── Application.Contracts/         # Interface definitions and DTOs
│   ├── IServices/                 # Service interface definitions
│   │   ├── OW/                    # Core service interfaces
│   │   ├── AI/                    # AI service interfaces
│   │   ├── Action/                # Action interfaces
│   │   ├── Integration/           # Integration interfaces
│   │   └── DynamicData/           # Dynamic data interfaces
│   ├── Dtos/                      # Data transfer objects
│   │   ├── OW/                    # Core DTOs (User, Workflow, Stage, etc.)
│   │   ├── AI/                    # AI-related DTOs
│   │   ├── Action/                # Action DTOs
│   │   ├── Integration/           # Integration DTOs
│   │   ├── DynamicData/           # Dynamic field DTOs
│   │   └── Shared/                # Shared/common DTOs
│   ├── Helpers/                   # Contract-level helpers
│   └── Options/                   # Configuration option classes
│
├── Domain/                        # Domain entities and repository contracts
│   ├── Entities/                  # Entity definitions
│   │   ├── Base/                  # Base entity classes (EntityBaseCreateInfo, etc.)
│   │   ├── OW/                    # Core domain entities (Workflow, Stage, Onboarding, etc.)
│   │   ├── Action/                # Action entities
│   │   ├── Integration/           # Integration entities
│   │   ├── DynamicData/           # Dynamic field entities
│   │   └── Shared/                # Shared entities
│   ├── Repository/                # Repository interface definitions
│   │   ├── IBaseRepository.cs     # Generic base repository interface
│   │   ├── OW/                    # OW repository interfaces
│   │   ├── Action/                # Action repository interfaces
│   │   ├── Integration/           # Integration repository interfaces
│   │   ├── DynamicData/           # Dynamic data repository interfaces
│   │   └── Shared/                # Shared repository interfaces
│   ├── Abstracts/                 # Filter interfaces (IValidFilter, ITenantFilter, IAppFilter)
│   ├── Manager/                   # Domain managers
│   └── Shared/                    # Domain shared utilities
│
├── Domain.Shared/                 # Shared enums, constants, models (no dependencies)
│   ├── Enums/                     # All enum definitions
│   ├── Const/                     # Constants (AuthSchemes, ClaimTypes, Permissions)
│   ├── Models/                    # Shared models (UserContext, AppContext)
│   ├── Helpers/                   # Helper utilities
│   ├── JsonConverters/            # JSON serialization converters
│   ├── Extensions/                # Extension methods
│   ├── Exceptions/                # Custom exception types
│   ├── Events/                    # Domain event interfaces
│   └── IDIService.cs              # DI marker interfaces (IScopedService, etc.)
│
├── SqlSugarDB/                    # ORM and data access implementation
│   ├── BaseRepository.cs          # Generic repository implementation
│   ├── Context/                   # SqlSugar context/session management
│   ├── Repositories/              # Concrete repository implementations
│   │   ├── OW/                    # OW repositories
│   │   ├── Action/                # Action repositories
│   │   ├── Integration/           # Integration repositories
│   │   ├── DynamicData/           # Dynamic data repositories
│   │   └── Shared/                # Shared repositories
│   ├── Implements/                # Additional implementations
│   ├── Migrations/                # SQL migration scripts
│   ├── Extensions/                # DB initialization, SqlSugar extensions
│   ├── Scripts/                   # Utility SQL scripts
│   └── Tools/                     # Database tooling
│
├── Infrastructure/                # Cross-cutting infrastructure
│   ├── Exceptions/                # Global exception handling middleware
│   ├── Services/                  # Background tasks, Redis cache, migrations
│   │   ├── Logging/               # Logging infrastructure
│   │   └── Security/              # Security utilities
│   ├── Extensions/                # Service registration, logging extensions
│   ├── Configuration/             # Config helpers
│   └── CodeGenerator/             # Code generation tools
│
├── Tests/
│   └── FlowFlex.Tests/            # xUnit test project
│
├── FlowFlex.sln                   # Solution file
├── Directory.Build.props          # Shared build properties
├── Dockerfile                     # Container build
├── docker-compose.yml             # Docker compose for deployment
├── docker-compose.dev.yml         # Docker compose for development
└── nuget.config                   # NuGet package sources
```

## Frontend Structure

```
packages/flowFlex-common/src/
├── main.ts                        # Vue app bootstrap
├── App.vue                        # Root component
├── app/                           # Application code
│   ├── apis/                      # API modules (Axios calls)
│   │   ├── ow/                    # Core APIs (workflow, onboarding, checklist, etc.)
│   │   ├── ai/                    # AI generation APIs
│   │   ├── action/                # Action APIs
│   │   ├── integration/           # Integration APIs
│   │   ├── dashboard/             # Dashboard APIs
│   │   ├── messageCenter/         # Message/email APIs
│   │   ├── login/                 # Auth/login APIs
│   │   ├── comments/              # Comment APIs
│   │   ├── global/                # Global/shared APIs
│   │   ├── model/                 # Model-related APIs
│   │   ├── pass/                  # Pass-through APIs
│   │   └── axios/                 # Axios instance config
│   ├── views/                     # Page-level components
│   │   ├── onboard/               # Onboarding/case views
│   │   ├── dashboard/             # Dashboard views
│   │   ├── actions/               # Action views
│   │   ├── authorityManagement/   # Permission management views
│   │   ├── dynamicFields/         # Dynamic field config views
│   │   ├── integration-settings/  # Integration config views
│   │   ├── messageCenter/         # Message center views
│   │   ├── login/                 # Login/auth views
│   │   └── error/                 # Error page views
│   ├── components/                # Reusable components
│   │   ├── global/                # App-wide global components
│   │   ├── form/                  # Form input components
│   │   ├── workflow-canvas/       # Workflow canvas/builder
│   │   ├── action-config/         # Action configuration components
│   │   ├── ai/                    # AI-related components
│   │   ├── draggableTable/        # Sortable table components
│   │   ├── RichTextEditor/        # Rich text editor
│   │   ├── layout/                # Layout components
│   │   ├── sidebar/               # Sidebar navigation
│   │   ├── common/                # Common utilities
│   │   └── ...                    # Many more component directories
│   ├── stores/                    # Pinia state management
│   │   ├── index.ts               # Store registration
│   │   ├── modules/               # Store modules
│   │   │   ├── user.ts            # User state
│   │   │   ├── permission.ts      # Permission state
│   │   │   ├── workflowCanvas.ts  # Workflow canvas state
│   │   │   ├── locale.ts          # i18n state
│   │   │   ├── appEnum.ts         # App enum cache
│   │   │   ├── multipleTab.ts     # Tab management
│   │   │   └── menuFunction.ts    # Menu function state
│   │   └── plugin/                # Store plugins
│   ├── router/                    # Vue Router configuration
│   │   ├── index.ts               # Router instance
│   │   ├── guard/                 # Navigation guards
│   │   ├── routers/               # Route definitions
│   │   │   ├── modules/           # Feature route modules
│   │   │   ├── basic.ts           # Base routes
│   │   │   └── index.ts           # Route aggregation
│   │   ├── helper/                # Router helpers
│   │   ├── constant.ts            # Route constants
│   │   └── types.ts               # Route types
│   ├── hooks/                     # Vue composables (use* prefix)
│   ├── utils/                     # Utility functions
│   ├── logics/                    # Business logic helpers
│   ├── config/                    # App configuration
│   ├── enums/                     # Frontend enum definitions
│   ├── settings/                  # App settings
│   └── mitt/                      # Event bus
├── assets/                        # Static assets (images, icons)
├── locales/                       # i18n translation files
├── styles/                        # Global SCSS styles
└── types/                         # Global TypeScript type definitions
    ├── onboard.d.ts               # Onboarding types
    ├── workflow-canvas.d.ts       # Workflow canvas types
    ├── action.d.ts                # Action types
    ├── checklist.d.ts             # Checklist types
    ├── condition.d.ts             # Condition types
    ├── dashboard.d.ts             # Dashboard types
    ├── integration.d.ts           # Integration types
    ├── permission.ts              # Permission types
    └── ...                        # More type files
```

## Directory Purposes

**`packages/flowFlex-backend/WebApi/Controllers/OW/`:**
- Purpose: Core business API endpoints for the workflow engine
- Contains: WorkflowController, StageController, OnboardingController, ChecklistController, QuestionnaireController, UserController, PermissionController, DashboardController
- Key files: `WorkflowController.cs`, `OnboardingController.cs`, `StageController.cs`

**`packages/flowFlex-backend/Application/Services/OW/`:**
- Purpose: Core business logic implementations
- Contains: Service classes implementing IService interfaces from Application.Contracts
- Key files: `WorkflowService.cs`, `StageService.cs`, `OnboardingServices/OnboardingService.cs`, `PermissionService.cs`

**`packages/flowFlex-backend/Domain/Entities/OW/`:**
- Purpose: Domain entity definitions for the workflow engine
- Contains: Workflow, Stage, StageCondition, Onboarding, OnboardingStageProgress, Checklist, ChecklistTask, Questionnaire, QuestionnaireAnswer, User, etc.
- Key files: `Workflow.cs`, `Stage.cs`, `Onboarding.cs`, `StageCondition.cs`

**`packages/flowFlex-backend/SqlSugarDB/Migrations/`:**
- Purpose: Incremental SQL migration scripts applied on startup
- Contains: Timestamped `.sql` files
- Generated: No (hand-written)
- Committed: Yes

## Key File Locations

**Entry Points:**
- `packages/flowFlex-backend/WebApi/Program.cs`: Backend app bootstrap
- `packages/flowFlex-common/src/main.ts`: Frontend app bootstrap

**Configuration:**
- `packages/flowFlex-backend/WebApi/appsettings.json`: Backend config (Database, Redis, Security, IdmApis, etc.)
- `packages/flowFlex-common/.env.development`: Frontend dev environment vars

**Core Logic:**
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs`: Workflow CRUD
- `packages/flowFlex-backend/Application/Services/OW/StageService.cs`: Stage management
- `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/`: Onboarding case logic (split into sub-services)
- `packages/flowFlex-backend/Application/Services/OW/Permission/`: Permission evaluation

**Testing:**
- `packages/flowFlex-backend/Tests/FlowFlex.Tests/`: Backend unit tests (xUnit)

## Naming Conventions

**Files (Backend):**
- Entities: PascalCase matching class name (`Workflow.cs`, `StageCondition.cs`)
- Services: `{Entity}Service.cs` (`WorkflowService.cs`)
- Interfaces: `I{Entity}Service.cs` or `I{Entity}Repository.cs`
- Controllers: `{Entity}Controller.cs`
- DTOs: Grouped in `Dtos/{Domain}/{Entity}/` folders
- AutoMapper: `{Entity}MapProfile.cs`
- Migrations: `{timestamp}_{description}.sql`

**Files (Frontend):**
- Views: `index.vue` inside feature directories
- APIs: `camelCase.ts` inside domain directories
- Components: PascalCase directories with `index.vue`
- Stores: `camelCase.ts` inside `modules/`
- Types: `kebab-case.d.ts`
- Route modules: `camelCase.ts`

**Directories:**
- Backend domain grouping: `OW/`, `AI/`, `Action/`, `Integration/`, `DynamicData/`, `Shared/`
- Frontend domain grouping: `ow/`, `ai/`, `action/`, `integration/`, `messageCenter/`

## Where to Add New Code

**New Backend Feature (OW domain):**
1. Entity: `Domain/Entities/OW/{Entity}.cs` - extend `EntityBaseCreateInfo`
2. Repository interface: `Domain/Repository/OW/I{Entity}Repository.cs` - extend `IBaseRepository<{Entity}>`
3. Repository impl: `SqlSugarDB/Repositories/OW/{Entity}Repository.cs` - extend `BaseRepository<{Entity}>`
4. DTOs: `Application.Contracts/Dtos/OW/{Entity}/`
5. Service interface: `Application.Contracts/IServices/OW/I{Entity}Service.cs`
6. Service impl: `Application/Services/OW/{Entity}Service.cs` - implement `I{Entity}Service, IScopedService`
7. AutoMapper profile: `Application/Maps/{Entity}MapProfile.cs`
8. Controller: `WebApi/Controllers/OW/{Entity}Controller.cs` - extend `ControllerBase`
9. Migration (if needed): `SqlSugarDB/Migrations/{timestamp}_{description}.sql`

**New Frontend Feature:**
1. API module: `src/app/apis/{domain}/{feature}.ts`
2. View: `src/app/views/{feature}/index.vue`
3. Route: `src/app/router/routers/modules/{feature}.ts`
4. Store (if needed): `src/app/stores/modules/{feature}.ts` - ID prefix `item-wfe-app-`
5. Types: `src/types/{feature}.d.ts`

**New Shared UI Component:**
- Reusable: `src/app/components/global/{ComponentName}/index.vue`
- Form input: `src/app/components/form/{componentName}/`

**New Backend Integration:**
1. Entity: `Domain/Entities/Integration/{Entity}.cs`
2. Repository: `Domain/Repository/Integration/` + `SqlSugarDB/Repositories/Integration/`
3. Service: `Application.Contracts/IServices/Integration/` + `Application/Services/Integration/`
4. Controller: `WebApi/Controllers/Integration/{Entity}Controller.cs`

**New Middleware:**
- File: `WebApi/Middlewares/{Name}Middleware.cs`
- Register in: `WebApi/Program.cs` (order matters - see existing pipeline)

**New Background Service:**
- Implementation: `Infrastructure/Services/{Name}Service.cs`
- Register in: `WebApi/Program.cs` via `AddHostedService<>()`

## Special Directories

**`SqlSugarDB/Migrations/`:**
- Purpose: SQL migration scripts run at startup by MigrationManager
- Generated: No (manually written)
- Committed: Yes

**`packages/flowFlex-common/node_modules/`:**
- Purpose: npm dependencies
- Generated: Yes (pnpm install)
- Committed: No (.gitignore)

**`packages/flowFlex-backend/*/bin/` and `obj/`:**
- Purpose: Build output
- Generated: Yes (dotnet build)
- Committed: No (.gitignore)

**`.planning/`:**
- Purpose: GSD workflow planning artifacts and codebase analysis
- Generated: By AI tooling
- Committed: Yes

**`specs/`:**
- Purpose: Feature specifications and design documents
- Generated: No (manually authored)
- Committed: Yes

---

*Structure analysis: 2026-06-08*
