# Coding Conventions

**Analysis Date:** 2026-06-08

## Naming Patterns

**Files (Backend):**
- Classes: PascalCase matching class name (e.g., `WorkflowService.cs`, `StageController.cs`)
- Interfaces: `I` prefix + PascalCase (e.g., `IWorkflowService.cs`, `IStageRepository.cs`)
- Entities: PascalCase singular noun (e.g., `Workflow.cs`, `Onboarding.cs`, `ChecklistTask.cs`)
- DTOs: `{Entity}{Input|Output}Dto.cs` grouped in folders matching entity
- Enums: PascalCase + `Enum` suffix (e.g., `ViewPermissionModeEnum`, `ErrorCodeEnum`)
- AutoMapper profiles: `{Entity}MapProfile.cs` (e.g., `OnboardingMapProfile.cs`, `ActionMapProfile.cs`)
- Test files: `{ServiceUnderTest}Tests.cs` (e.g., `ActionExecutorTests.cs`, `PermissionHelpersTests.cs`)

**Files (Frontend):**
- Vue components: PascalCase `.vue` files (e.g., `StageForm.vue`, `WorkflowCardView.vue`, `PermissionSelector.vue`)
- API modules: camelCase or kebab-case `.ts` files (e.g., `index.ts`, `change-log.ts`, `userInvitation.ts`)
- Store modules: camelCase `.ts` (e.g., `user.ts`, `workflowCanvas.ts`, `permission.ts`)
- Composables/hooks: `use` prefix, camelCase (e.g., `useI18n.ts`, `useStreamAIWorkflow.ts`, `useAdaptiveScrollbar.ts`)
- Enum files: camelCase + `Enum.ts` suffix (e.g., `permissionEnum.ts`, `stageColorEnum.ts`, `conditionEnum.ts`)

**Functions (Backend):**
- Async methods: `PascalCaseAsync` suffix (e.g., `CreateAsync`, `GetByIdAsync`, `ValidateRulesJsonAsync`)
- Sync methods: PascalCase (e.g., `GetUserTeamIds`, `CheckTeamWhitelist`, `HasAdminPrivileges`)
- Private methods: PascalCase (same as public — no underscore prefix on methods)
- Test methods: `MethodName_Scenario_ExpectedBehavior` (e.g., `ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess`)

**Functions (Frontend):**
- API functions: camelCase verbs (e.g., `createWorkflow`, `getWorkflowList`, `getWorkflowDetail`)
- Event handlers: `handle` prefix or inline arrow functions
- Composable functions: `use` prefix (e.g., `useI18n`, `useWujie`, `useUserStore`)
- Computed properties: descriptive nouns (e.g., `displayAction`, `isFormValid`, `sizeClasses`)

**Variables (Backend):**
- Private fields: `_camelCase` with underscore prefix (e.g., `_workflowRepository`, `_mapper`, `_logger`)
- Constants: `UPPER_SNAKE_CASE` or PascalCase (e.g., `STAGE_CACHE_PREFIX`, `STAGE_CACHE_DURATION`)
- Local variables: camelCase
- Test constants: PascalCase in static class (e.g., `DefaultUserId`, `TeamA`, `DefaultTenantId`)

**Variables (Frontend):**
- Refs: camelCase (e.g., `formRef`, `allUserData`, `currentTab`)
- Constants: camelCase for module-level (e.g., `globSetting`, `colorOptions`)

**Types (Backend):**
- Namespaces: `FlowFlex.<Layer>.<Domain>` (e.g., `FlowFlex.Application.Services.OW`, `FlowFlex.Domain.Entities.OW`)
- Interfaces: `I` prefix (e.g., `IWorkflowService`, `IStageRepository`, `IScopedService`)
- DTOs grouped in: `Application.Contracts/Dtos/OW/{Entity}/`

**Types (Frontend):**
- Type imports use `#/` alias → `./src/types/*` (e.g., `import { Stage } from '#/onboard'`)
- TypeScript interfaces: PascalCase (e.g., `ActionInfo`, `UserState`, `Props`)
- Store IDs: `item-wfe-app-{name}` prefix (e.g., `'item-wfe-app-user'`)
- Enum values: PascalCase or SCREAMING_SNAKE_CASE depending on module

## Code Style

**Formatting (Backend):**
- 4-space indentation
- File-scoped namespaces for newer files: `namespace FlowFlex.Domain.Entities.Base;`
- Block-scoped namespaces in older files: `namespace FlowFlex.Domain.Entities.OW { ... }`
- `#region` blocks for organizing related methods and test groups
- XML doc comments on public APIs (`/// <summary>`)
- `nullable enable` and `ImplicitUsings enable` in project settings

**Formatting (Frontend — `.prettierrc`):**
- Tabs (width 4)
- Single quotes
- Semicolons: yes
- Trailing commas: ES5
- Bracket spacing: yes
- Print width: 100
- End of line: auto
- HTML whitespace sensitivity: ignore

**Linting (Frontend — `.eslintrc.cjs`):**
- Extends `@uni` base config
- `vue/html-self-closing`: void elements always self-close, normal elements never, components always
- `@typescript-eslint/no-duplicate-enum-values`: off
- `@typescript-eslint/no-unused-vars`: error on all vars, ignore function args and rest siblings

## Import Organization

**Backend (C#):**
1. System namespaces (`System`, `System.Collections.Generic`, `System.Linq`)
2. Third-party packages (`AutoMapper`, `Microsoft.Extensions.Logging`, `SqlSugar`, `Newtonsoft.Json`)
3. Internal packages (`Item.Redis`, `Item.Internal.Auth`, `Item.ThirdParty.IdentityHub`)
4. Project namespaces (`FlowFlex.Application.*`, `FlowFlex.Domain.*`, `FlowFlex.Domain.Shared.*`)

**Frontend (TypeScript):**
1. Vue core imports (`import { ref, computed, onMounted } from 'vue'`)
2. Third-party libraries (`import type { FormInstance } from 'element-plus'`)
3. Internal path-aliased imports (`import { defHttp } from '@/apis/axios'`)
4. Components (`import StageComponentsSelector from './StageComponentsSelector.vue'`)
5. Type imports (`import { Stage, Checklist } from '#/onboard'`)

**Path Aliases (Frontend — `tsconfig.json`):**
- `@/` → `./src/app/`
- `@*` → `./src/*`
- `#/` → `./src/types/*`

## Error Handling

**Backend Pattern:**
- Services throw `CRMException(ErrorCodeEnum, message)` for business errors
- `CRMException` carries an `ErrorCodeEnum` code and optional `HttpStatusCode`
- Located at `packages/flowFlex-backend/Domain.Shared/Exceptions/CRMException.cs`
- Error codes defined in `packages/flowFlex-backend/Domain.Shared/Exceptions/ErrorCode.cs`
- Global `ExceptionHandlingMiddleware` catches all exceptions and converts to standard response
- Guard clauses: validate early, throw with specific error codes

```csharp
// Standard error throwing pattern
if (input == null)
{
    throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
}

// HTTP status code variant
throw new CRMException(HttpStatusCode.NotFound, "Workflow not found");
```

**Frontend Pattern:**
- Axios `transformResponseHook` unwraps `SuccessResponse` envelope
- Error responses show `ElMessage.error(...)` via interceptors
- `checkStatus.ts` handles HTTP status-specific error messages
- Promise rejections propagate to component-level try/catch

## Response Wrapping

**Backend:**
- All controllers extend `ControllerBase` at `packages/flowFlex-backend/WebApi/Controllers/ControllerBase.cs`
- Use `Success<T>(data)` or `Success()` — never raw `Ok()`
- Response wrapped in `SuccessResponse` from `Item.Internal.StandardApi.Response`

```csharp
// Correct pattern — always wrap response
public async Task<IActionResult> Create([FromBody] WorkflowInputDto input)
{
    var id = await _workflowService.CreateAsync(input);
    return Success(id);
}
```

## Dependency Injection

**Registration Pattern:**
- Services implement marker interfaces for auto-registration: `IScopedService`, `ITransientService`, `ISingletonService`
- Defined in `packages/flowFlex-backend/Domain.Shared/IDIService.cs`
- DI framework scans for these markers — no manual registration needed

```csharp
// Service implements both its interface and DI lifetime marker
public class WorkflowService : IWorkflowService, IScopedService
{
    // ...
}
```

**Constructor Injection:**
- All dependencies injected via constructor
- Private readonly fields with `_camelCase` naming
- Optional dependencies use default `= null` parameter

```csharp
private readonly IWorkflowRepository _workflowRepository;
private readonly IMapper _mapper;
private readonly UserContext _userContext;
private readonly ILogger<WorkflowService> _logger;

public WorkflowService(
    IWorkflowRepository workflowRepository,
    IMapper mapper,
    UserContext userContext,
    ILogger<WorkflowService> logger,
    IRulesEngineService rulesEngineService = null)  // Optional dependency
{
    _workflowRepository = workflowRepository;
    _mapper = mapper;
    _userContext = userContext;
    _logger = logger;
}
```

## Entity Design

**Base Class Hierarchy:**
```
IdEntityBase (Id: long, Snowflake)               — packages/flowFlex-backend/Domain/Entities/Base/IdEntityBase.cs
  └── AbstractEntityBase (TenantId, AppCode)     — packages/flowFlex-backend/Domain/Entities/Base/AbstractEntityBase.cs
       └── EntityBase (IsValid — soft delete)    — packages/flowFlex-backend/Domain/Entities/Base/EntityBase.cs
            └── EntityBaseCreateInfo (audit)      — packages/flowFlex-backend/Domain/Entities/Base/EntityBaseCreateInfo.cs
```

**Entity Conventions:**
- `[SugarTable("ff_table_name")]` attribute for table mapping — all tables prefixed `ff_`
- `[SugarColumn(ColumnName = "snake_case")]` for column names; SqlSugar auto-converts PascalCase via `ToUnderLine()`
- `[StringLength(N)]` for string field validation
- `[JsonConverter(typeof(LongToStringConverter))]` on long ID fields (prevents JS precision loss)
- `[ChangeLogColumn(IsIgnore = true)]` on audit fields to exclude from change logs
- `[IgnoreDisplay]` on system fields

**Soft Delete:**
- `IsValid` flag (true = active, false = deleted) — NOT `IsDeleted`
- SqlSugar global filter via `IValidFilter` interface

## Controller Design

**Attributes:**
```csharp
[ApiController]
[Route("ow/workflows/v{version:apiVersion}")]
[Display(Name = "workflow")]
[Authorize]
public class WorkflowController : Controllers.ControllerBase
```

**Permission Attributes:**
- `[WFEAuthorize(PermissionConsts.Workflow.Create)]` — module-level permission check
- `[RequirePermission(PermissionEntityTypeEnum.Workflow, OperationTypeEnum.Operate)]` — entity-level permission (Action Filter)
- `[PortalAccess]` — marks endpoints accessible by portal tokens
- `[ProducesResponseType<SuccessResponse<T>>((int)HttpStatusCode.OK)]` — Swagger documentation

**Route Pattern:**
- Domain prefix: `ow/` for OW domain, `action/` for Action domain
- Versioned: `v{version:apiVersion}`
- RESTful: GET list, GET /{id}, POST, PUT /{id}, DELETE /{id}

## Vue Component Conventions

**Script Section:**
- Always use `<script setup lang="ts">` — no Options API

**Props Pattern (two styles coexist):**
```typescript
// Style 1 — PropType approach (more common in this codebase):
const props = defineProps({
    stage: {
        type: Object as PropType<Stage | null>,
        default: null,
    },
    loading: {
        type: Boolean,
        default: false,
    },
    checklists: {
        type: Array as PropType<Checklist[]>,
        default: () => [],
    },
});

// Style 2 — Generic approach (also acceptable):
// withDefaults(defineProps<Props>(), { ... })
```

**Emits Pattern:**
```typescript
const emit = defineEmits(['submit', 'cancel']);
```

**Styles:**
- `<style scoped lang="scss">` + Tailwind CSS utilities
- Scoped styles preferred
- Element Plus components styled via Tailwind utility classes

## Store Pattern (Pinia)

```typescript
import { defineStore } from 'pinia';

export const useUserStore = defineStore({
    id: 'item-wfe-app-user',
    state: (): UserState => ({
        userInfo: null,
        token: undefined,
        roleList: [],
        // ...
    }),
    // actions, getters...
});
```

## API Module Pattern (Frontend)

```typescript
import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

// URL builder function — centralizes endpoint paths
const Api = (id?: string | number) => {
    return {
        workflows: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}`,
        workflow: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${id}`,
    };
};

// Named exports, camelCase function names
export function createWorkflow(params: any) {
    return defHttp.post({ url: `${Api().workflows}`, params });
}

export function getWorkflowDetail(id: string | number) {
    return defHttp.get({ url: `${Api(id).workflow}` });
}
```

## Multi-Tenancy

- Every entity carries `TenantId` + `AppCode` (from `AbstractEntityBase`)
- SqlSugar global filters auto-apply tenant isolation via `ITenantFilter` and `IAppFilter`
- Bypass with `.Filter(null, true)` when cross-tenant access is explicitly needed
- `AppIsolationMiddleware` extracts `AppCode` + `TenantId` from headers/JWT
- `TenantMiddleware` sets the active tenant for the request

## JSON Serialization

**Backend:**
- Mixed: both `System.Text.Json` and `Newtonsoft.Json` coexist
- **Use `System.Text.Json` for new code**
- `LongToStringConverter` serializes `long` IDs as strings (prevents JS precision loss)
- Type aliasing used when both libraries are needed in one file:
```csharp
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;
```

## Logging

**Framework:** Serilog (backend), `console.log` (frontend, minimal)

**Backend Patterns:**
- Inject `ILogger<T>` via constructor
- Structured logging with named placeholders: `_logger.LogWarning("Action definition not found: {ActionId}", actionDefinitionId)`
- Log at `Warning` for expected missing data, `Error` for unexpected failures

## Comments

**When to Comment:**
- XML doc comments required on all public API surface (classes, methods, properties) in backend
- Inline comments for non-obvious logic, especially permission checks and JSON parsing
- Chinese-language inline comments are common throughout the codebase
- Test methods use `// Arrange`, `// Act`, `// Assert` structure consistently

---

*Convention analysis: 2026-06-08*
