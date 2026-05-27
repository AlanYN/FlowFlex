# Coding Conventions

**Analysis Date:** 2026-05-25

## Naming Patterns

**Files (Frontend):**
- API modules: camelCase, grouped by domain in `src/app/apis/<domain>/index.ts` (e.g., `src/app/apis/action/index.ts`)
- Vue components: PascalCase filenames (e.g., `ActionResultDialog.vue`, `ActionTag.vue`)
- Store modules: camelCase (e.g., `user.ts`, `workflowCanvas.ts`)
- Utility files: camelCase (e.g., `axiosCancel.ts`, `storageCache.ts`)
- Type definition files: camelCase, placed in `types/` at project root

**Files (Backend):**
- C# files: PascalCase matching the class name (e.g., `ActionExecutionService.cs`, `OnboardingStageManagementService.cs`)
- Test files: `<ClassName>Tests.cs` (e.g., `ActionExecutorTests.cs`, `WorkflowPermissionServiceTests.cs`)
- Map profiles: `<Entity>MapProfile.cs` (e.g., `OnboardingMapProfile.cs`)
- Repository interfaces: `I<Entity>Repository.cs`
- Service interfaces: `I<Name>Service.cs`

**Functions (Frontend):**
- Exported API functions: camelCase verbs (e.g., `addAction`, `getActionDefinitions`, `deleteAction`, `updateAction`)
- Vue composables/hooks: `use` prefix (e.g., `useUserStore`, `useGlobSetting`, `useI18n`)
- Event handlers: `handle` prefix (e.g., `handleClick`)
- Computed properties: descriptive nouns (e.g., `displayAction`, `isMultipleActions`, `sizeClasses`)

**Functions (Backend):**
- Public methods: PascalCase with `Async` suffix for async methods (e.g., `ExecuteActionAsync`, `GetActionDefinitionAsync`)
- Private helpers: PascalCase (e.g., `CreateAxios`, `GetTransform`)
- Test methods: `MethodName_Scenario_ExpectedBehavior` (e.g., `ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess`)

**Variables:**
- Frontend: camelCase (`actionsJson`, `dialogVisible`, `userContext`)
- Backend: camelCase for locals, `_camelCase` for private fields (e.g., `_actionDefinitionRepository`, `_logger`)
- Backend constants: PascalCase in static classes (e.g., `DefaultUserId`, `TeamA`)

**Types/Interfaces (Frontend):**
- TypeScript interfaces: PascalCase (e.g., `ActionInfo`, `TestResult`, `Props`)
- Enums: PascalCase name, SCREAMING_SNAKE_CASE values (e.g., `ActionType.PYTHON_SCRIPT`)
- Const maps: SCREAMING_SNAKE_CASE (e.g., `ACTION_TYPE_MAPPING`, `FRONTEND_TO_BACKEND_TYPE_MAPPING`)

**Types (Backend):**
- Classes, interfaces, enums: PascalCase (e.g., `ActionExecutionService`, `IActionManagementService`, `ActionExecutionStatusEnum`)
- Enum names end with `Enum` suffix (e.g., `ViewPermissionModeEnum`, `OperationTypeEnum`)
- Namespaces mirror directory structure: `FlowFlex.<Layer>.<Domain>` (e.g., `FlowFlex.Application.Services.Action`)

## Code Style

**Frontend Formatting (`.prettierrc`):**
- Tabs for indentation, tab width 4
- Print width: 100 characters
- Single quotes
- Trailing commas: ES5
- Semicolons: required
- End of line: auto
- HTML whitespace sensitivity: ignore

**Frontend Linting (`.eslintrc.cjs`):**
- Extends `@uni/eslint-config/strict`
- Vue HTML self-closing: void elements always, normal elements never, components always
- `@typescript-eslint/no-unused-vars`: error on all vars, ignore function args and rest siblings
- Duplicate enum values: disabled

**Backend Style:**
- 4-space indentation
- `#region` / `#endregion` blocks used to group related methods (e.g., `#region Helper Methods`, `#region ExecuteActionsAsync Tests`)
- XML doc comments (`/// <summary>`) on all public classes and methods
- `ArgumentNullException` guard clauses in constructors for required dependencies
- `nullable enable` and `ImplicitUsings enable` in all projects

## Import Organization

**Frontend Order:**
1. Vue core imports (`import { ref, computed } from 'vue'`)
2. Third-party libraries (`import { Icon } from '@iconify/vue'`)
3. Internal path-aliased imports (`import { defHttp } from '@/apis/axios'`)
4. Type imports (`import type { ... }`)
5. Relative component imports (`import ActionResultDialog from './ActionResultDialog.vue'`)

**Path Aliases (Frontend):**
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

**Backend:**
- System namespaces first, then third-party, then internal `FlowFlex.*`
- No explicit ordering enforced beyond convention

## Error Handling

**Frontend:**
- Axios errors caught in `requestCatchHook` and `responseInterceptorsCatch` in `src/app/apis/axios/Axios.ts`
- Promise rejections propagate via `reject(e)` in the request wrapper
- HTTP status errors handled in `src/app/apis/axios/checkStatus.ts`
- UI errors shown via `ElMessage.error(...)` from Element Plus

**Backend:**
- Services return domain objects or throw exceptions; controllers wrap in `Success(result)` or `NotFound()`
- Constructor guard clauses: `?? throw new ArgumentNullException(nameof(dep))`
- Structured logging with `_logger.LogWarning(...)` / `_logger.LogError(...)` using message templates
- Action execution results use a `Success`/`ErrorMessage` result object pattern rather than exceptions

## Logging

**Framework:** Serilog (backend), `console.log` (frontend, minimal)

**Backend Patterns:**
- Inject `ILogger<T>` via constructor
- Use structured logging with named placeholders: `_logger.LogWarning("Action definition not found: {ActionId}", actionDefinitionId)`
- Log at `Warning` for expected missing data, `Error` for unexpected failures
- Frontend has a `console.log('signal', conf.signal)` debug statement in `src/app/apis/axios/Axios.ts` (line 241) — not a pattern to follow

## Comments

**When to Comment:**
- XML doc comments required on all public API surface (classes, methods, properties) in backend
- Inline comments for non-obvious logic, especially JSON parsing and permission checks
- Chinese-language inline comments are common throughout the codebase (both frontend and backend)
- Test methods use `// Arrange`, `// Act`, `// Assert` structure consistently

**JSDoc/TSDoc:**
- Not used in frontend TypeScript files; type safety via TypeScript interfaces instead
- Backend uses XML `/// <summary>` / `/// <param>` / `/// <returns>` on public members

## Function Design

**Size:** Services tend to be large (100–300+ lines per method for complex orchestration). Helpers extracted to `Application/Helpers/` for reusable logic.

**Parameters:** Constructor injection for all dependencies. Method parameters kept minimal; complex inputs use DTO objects.

**Return Values:**
- Frontend API functions return `Promise<T>` via `defHttp.get/post/put/delete`
- Backend async methods return `Task<T>` or `Task`
- Action execution returns a result object with `Success`, `Details`, and `ErrorMessage` fields rather than throwing

## Module Design

**Frontend Exports:**
- API modules export named functions directly (no default export)
- Stores use `defineStore` with a string ID prefixed `item-wfe-app-<name>`
- Barrel files (`index.ts`) used in API and store directories

**Backend Exports:**
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

---

*Convention analysis: 2026-05-25*
