---
inclusion: always
---

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Frontend (Vue.js) — working directory: `packages/flowFlex-common/`
```bash
pnpm dev                    # Start development server
pnpm serve:local           # Start with localhost config
pnpm serve:staging         # Start with staging config
pnpm build:production      # Build for production
pnpm lint                  # Run all linters in parallel
pnpm type:check           # TypeScript type checking
pnpm test                  # Run Jest tests
```

### Backend (.NET) — working directory: `packages/flowFlex-backend/`
```bash
dotnet restore             # Restore NuGet packages
dotnet build              # Build the solution
dotnet run --project WebApi  # Run the WebApi project
dotnet test               # Run all tests (xUnit)
dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests  # Unit tests only
```

## Architecture Overview

### System Overview
```
┌─────────────────────────────────────────────────────────────────────┐
│                        Frontend (Vue 3 SPA)                          │
│              packages/flowFlex-common/src/                           │
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

### Request Data Flow

**Backend API Request:**
1. HTTP → Kestrel → `GlobalExceptionHandlingMiddleware`
2. → `AppIsolationMiddleware` (extracts `AppCode` + `TenantId` from headers/JWT)
3. → `TenantMiddleware` → JWT authentication → `WFEAuthorize` permission check
4. → Controller → `IService` method → Repository → SqlSugar → PostgreSQL
5. → AutoMapper (entity → DTO) → Controller returns `Success<T>(data)`

**Frontend Request:**
1. Component calls API function (e.g., `getWorkflows()`)
2. → Axios instance adds JWT token + `AppCode` + timezone headers
3. → `transformResponseHook` unwraps `SuccessResponse` envelope
4. → Result to Pinia store or component directly

### Key Technical Decisions

- **Multi-Tenancy**: `AppCode` + `TenantId` on every entity; SqlSugar global filter auto-applies
- **Authentication**: 3 JWT schemes simultaneously — local JWT, IdentityHub (IDM), ItemIAM
- **ID Strategy**: Snowflake `long` IDs, serialized as strings via `LongToStringConverter` (JS precision)
- **Soft Deletes**: `IsValid` flag (not `IsDeleted`); SqlSugar global filter via `IValidFilter`
- **DI Auto-Registration**: Services implement `IScopedService`/`ISingletonService`/`ITransientService` markers
- **ORM**: SqlSugar with `SqlSugarScope` per-request (not singleton)
- **Micro-Frontend**: Supports Wujie sub-app mode (`window.__POWERED_BY_WUJIE__`)

### Database Conventions
- All tables prefixed with `ff_` (e.g., `ff_workflow`, `ff_onboarding_stage_progress`)
- Columns: snake_case (auto-converted from PascalCase by SqlSugar `ToUnderLine()`)
- Primary keys: `id` (snowflake long)
- Audit fields: `create_date`, `modify_date`, `create_by`, `modify_by`, `create_user_id`, `modify_user_id`
- Soft delete: `is_valid` (bool, true = active)
- Multi-tenancy: `app_code`, `tenant_id`
- JSONB columns for flexible/dynamic data

### API Patterns
- Controllers extend `ControllerBase` — use `Success<T>(data)`, never raw `Ok()`
- Business errors: throw `CRMException(ErrorCodeEnum, message)`
- Validation: FluentValidation on request DTOs
- Response envelope: `SuccessResponse` wrapper (frontend unwraps automatically)

### Workflow System Components
- **Workflows**: Main definitions with ordered stages
- **Stages**: Steps with components (Questionnaires, Checklists, Actions)
- **Stage Progress**: Tracks user progress through stages
- **Condition Actions**: Rules that trigger on stage transitions (GoToStage, SkipStage, SendNotification, etc.)

## Where to Add New Code

### New Backend Feature (OW domain)
1. Entity: `Domain/Entities/OW/{Entity}.cs` — extend `EntityBaseCreateInfo`
2. Repository interface: `Domain/Repository/OW/I{Entity}Repository.cs` — extend `IBaseRepository<{Entity}>`
3. Repository impl: `SqlSugarDB/Repositories/OW/{Entity}Repository.cs` — extend `BaseRepository<{Entity}>`
4. DTOs: `Application.Contracts/Dtos/OW/{Entity}/`
5. Service interface: `Application.Contracts/IServices/OW/I{Entity}Service.cs`
6. Service impl: `Application/Services/OW/{Entity}Service.cs` — implement `I{Entity}Service, IScopedService`
7. AutoMapper profile: `Application/Maps/{Entity}MapProfile.cs` — register in `Program.cs`
8. Controller: `WebApi/Controllers/OW/{Entity}Controller.cs` — extend `ControllerBase`
9. Migration (if needed): `SqlSugarDB/Migrations/{timestamp}_{description}.sql`

### New Frontend Feature
1. API module: `src/app/apis/{domain}/{feature}.ts`
2. View: `src/app/views/{feature}/index.vue`
3. Route: `src/app/router/routers/modules/{feature}.ts`
4. Store (if needed): `src/app/stores/modules/{feature}.ts` — ID prefix `item-wfe-app-`

### New Shared UI Component
- Reusable: `src/app/components/global/{ComponentName}/index.vue`
- Form input: `src/app/components/form/{componentName}/`

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

## Technology Stack

**Languages:** C# 12 (.NET 8), TypeScript 5.3, SQL (PostgreSQL), SCSS, HTML

**Backend Core:**
- ASP.NET Core 8.0, SqlSugar ORM, AutoMapper, FluentValidation, Hangfire, Serilog
- Internal packages: `Item.Redis`, `Item.Internal.Auth`, `Item.Message.RabbitMq`, `Item.Message.Kafka`, `Item.BlobProvider`, `Item.Internal.Nacos`

**Frontend Core:**
- Vue 3.5, Vite 5.4, Element Plus 2.9, Pinia 2.2, Tailwind CSS 3.4, Axios
- Dev tools: unplugin-auto-import, unplugin-vue-components, ESLint + Prettier, Husky

**Testing:** xUnit + Moq + FluentAssertions (backend), Jest + @vue/test-utils (frontend)

**Configuration:**
- Backend: `appsettings.json` → sections: `Database`, `Redis`, `Cache`, `Security`, `Global`, `BlobStore`, `AI`, `Email`, `IdmApis`, `OutlookApis`
- Frontend: `.env.{mode}` → vars: `VITE_GLOB_API_URL`, `VITE_PROXY_URL`, `VITE_GLOB_IDM_URL`, `VITE_GLOB_CODE`

**Platform:** Node.js >= 18.12, pnpm >= 8.10, .NET 8 SDK, PostgreSQL, Redis, Docker (port 8080)

## Conventions

### Naming Patterns

**Frontend:**
- API modules: `src/app/apis/<domain>/index.ts`, camelCase function names (`getWorkflows`, `addAction`)
- Components: PascalCase `.vue` files
- Stores: `defineStore('item-wfe-app-<name>', ...)`
- Composables: `use` prefix
- Event handlers: `handle` prefix
- Path aliases: `@/` → `src/app/`, `#/` → `types/`

**Backend:**
- Files: PascalCase matching class name
- Async methods: `PascalCaseAsync` suffix
- Private fields: `_camelCase`
- Tests: `MethodName_Scenario_ExpectedBehavior`
- Enums: PascalCase with `Enum` suffix (e.g., `ViewPermissionModeEnum`)
- Namespaces: `FlowFlex.<Layer>.<Domain>`

### Code Style

**Frontend (.prettierrc):** Tabs (width 4), single quotes, semicolons, trailing commas ES5, print width 100

**Backend:** 4-space indent, `#region` blocks, XML doc comments on public API, `nullable enable`, guard clauses with `?? throw new ArgumentNullException(nameof(dep))`

### Vue Component Conventions
- `<script setup lang="ts">` only — no Options API
- Props: `withDefaults(defineProps<Props>(), {...})`
- Emits: `defineEmits<{ eventName: [argType] }>()`
- Styles: `<style scoped lang="scss">` + Tailwind utilities

### Error Handling
- Backend: services throw `CRMException(ErrorCodeEnum, message)`, global middleware catches all
- Frontend: Axios interceptors → `ElMessage.error(...)`, promise rejections propagate

### Testing Patterns
- Backend: constructor-based mock setup, `// Arrange / Act / Assert` in every test
- Shared helpers: `MockHelper.cs` (mock factories), `TestDataBuilder.cs` (entity builders)
- What to mock: all repository interfaces, external services, `ISqlSugarClient`, `ILogger<T>`
- What NOT to mock: value objects, DTOs, `PermissionHelpers`

## External Integrations

| Integration | Purpose | Config Key |
|-------------|---------|-----------|
| Item IDM | User auth, team queries | `IdmApis` |
| Microsoft Graph | Outlook email sync | `OutlookApis` |
| AI Providers | Workflow/questionnaire generation | `AI` (per-user apiKey in DB) |
| Nacos | Service discovery/config | via `Item.Internal.Nacos` |
| Mailgun SMTP | Email sending | `Email` |
| Aliyun OSS / AWS S3 | File storage | `BlobStore` + `Global:BlobStoreType` |
| RabbitMQ / Kafka | Messaging | via `Item.Message.*` |
| Hangfire | Background jobs | PostgreSQL-backed |

## Known Pitfalls

- **Bypassing multi-tenancy filter:** must use `.Filter(null, true)` explicitly — forgetting causes silent data isolation
- **Mixed JSON libraries:** both `System.Text.Json` and `Newtonsoft.Json` coexist; use `System.Text.Json` for new code
- **In-memory distributed cache:** `AddDistributedMemoryCache()` in non-dev — cache not shared across pods
- **Large service files:** `BaseOperationLogService` (4,726 lines), `AIWorkflowGenerator.vue` (6,378 lines) — tread carefully
- **XSS vectors:** some `v-html` bindings lack `DOMPurify.sanitize()` — always sanitize user/AI content
- **StageService stubs:** 10 methods throw `NotImplementedException` — don't call them

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
