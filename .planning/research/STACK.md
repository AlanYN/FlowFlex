# Technology Stack

**Project:** OW-621 Workflow Component Enhancements
**Researched:** 2026-06-02
**Constraint:** No new dependencies — all 12 features must be implemented using the existing stack.

---

## Summary Verdict

No new libraries are needed. Every feature in OW-621 maps cleanly onto patterns already present in the codebase. The constraint in PROJECT.md ("不引入新依赖") is fully achievable.

---

## Existing Stack (Confirmed from codebase)

### Frontend

| Technology | Version (package.json) | Relevant to OW-621 |
|------------|------------------------|---------------------|
| Vue 3 | ^3.5.12 | All frontend features |
| Element Plus | ^2.9.1 | Collapse, tags, scrollbar, form validation |
| dayjs | ^1.11.13 | Date/time formatting to seconds |
| dayjs/plugin/utc + timezone | (bundled with dayjs) | Timezone-aware datetime display |
| lodash-es | ^4.17.21 | Deep copy via cloneDeep |
| Pinia | ^2.2.6 | State management |
| Axios | ^1.7.7 | File upload with metadata |
| Tailwind CSS | ^3.4.14 | Layout adjustments (tag positioning) |
| @vueuse/core | ^10.11.1 | Scroll behavior composables |

### Backend

| Technology | Version (.csproj) | Relevant to OW-621 |
|------------|-------------------|---------------------|
| .NET 8 / ASP.NET Core 8 | net8.0 | All backend features |
| SqlSugar ORM | (via SqlSugarDB project) | Entity queries, cascade operations |
| AutoMapper | 12.0.1 | DTO mapping for duplicate/deep copy |
| FluentValidation | 11.3.0 | Short Answer Grid custom validation rule |
| System.Text.Json | (built-in .NET 8) | Change log serialization |
| Newtonsoft.Json | (via Mvc.NewtonsoftJson 8.0.0) | JSONB answer manipulation |
| Serilog | 8.0.3 | Permission chain debug logging |

---

## Feature-to-Stack Mapping

### 1. Change Log text/date formatting (features #1, #8)

**What's needed:** Change "Done/Cancel" to "Completed/Cancelled the task" in log descriptions; format OperationTime to include seconds. Remove StageSave log type entries.

**Existing patterns to use:**
- `ChecklistLogService.BuildTaskCompletionDescription()` and `BuildTaskUncompletionDescription()` — change the string literals directly. No framework change.
- `OperationChangeLog.OperationTime` is `DateTimeOffset` — already stores full precision including seconds.
- Frontend `ChangeLog.vue` renders `row.operationTime`. The project-wide date format constant lives in `projectSetting.ts`: `projectTenMinutesSsecondsDate = 'MM/DD/YYYY HH:mm:ss'`. The ChangeLog component should use this constant instead of the date-only `projectDate` format.
- `timeZoneConvert()` in `hooks/time.ts` already handles timezone-aware formatting — pass `projectTenMinutesSsecondsDate` as the `format` argument.
- For #8 (remove StageSave log type): filter at the insert point in the log service or at the query level in `OperationChangeLogRepository`. No schema change needed.

**Confidence:** HIGH — patterns are fully present.

### 2. Component lifecycle / cascade cleanup on delete (feature #2)

**What's needed:** When a Checklist or Questionnaire is deleted, remove it from `Stage.ChecklistId` / `Stage.QuestionnaireId` and from `Stage.ComponentsJson`.

**Existing patterns to use:**
- `Stage` entity has `ChecklistId` (nullable long) and `QuestionnaireId` (nullable long). Set to null on component delete.
- `Stage.ComponentsJson` is a JSONB string column holding a serialized component list. After nulling the FK, deserialize with `System.Text.Json.JsonSerializer` (already imported in WorkflowService), filter out the deleted component entry, and re-serialize.
- `IStageRepository` is already injected throughout the service layer.
- Perform cleanup synchronously in the delete operation for data integrity — do not defer to background queue.

**Confidence:** HIGH — all building blocks exist.

### 3. Deep copy for Workflow Duplicate (feature #3)

**What's needed:** When duplicating a Workflow, also copy Stage component configuration (ChecklistId, QuestionnaireId, ComponentsJson) into the new Stage.

**Existing gap identified:** `WorkflowService.DuplicateAsync()` (lines 885-906) constructs a new Stage from the original but only copies `Name`, `PortalName`, `InternalName`, `Description`, `DefaultAssignedGroup`, `DefaultAssignee`, `Order`, `Color`, `IsActive`. It explicitly omits `ChecklistId`, `QuestionnaireId`, `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`.

**Pattern to use:**
- Extend the `duplicatedStage` constructor block to also copy `ChecklistId`, `QuestionnaireId`, `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, `PortalPermission`, `VisibleInPortal`, `Required`, `AttachmentManagementNeeded`.
- `ComponentsJson` is a string — a string assignment is a value copy in C#, so no special deep clone library is needed.
- For frontend-side cloning of stage/component objects before API submission, `lodash-es cloneDeep` is already available and used in the codebase.

**Confidence:** HIGH — it is a gap in existing code, not a missing library.

### 4. Scroll behavior on questionnaire Next (feature #4)

**What's needed:** After clicking Next in the questionnaire wizard, scroll the view back to the top.

**Existing patterns to use:**
- `el-scrollbar` (Element Plus) is already used in `QuestionnairePreview.vue` as `<el-scrollbar max-height="70vh">`. Obtain a template ref and call `scrollbarRef.value?.setScrollTop(0)`.
- For the sub-portal `CustomerQuestionnaire.vue` where the scroll container may be the browser window, use `window.scrollTo({ top: 0, behavior: 'smooth' })`.
- `@vueuse/core useScroll` is available if a composable is preferred, but a direct call is simpler and consistent with how `el-scrollbar` is used elsewhere.

**Confidence:** HIGH — standard Vue 3 + Element Plus pattern, no new library.

### 5. Collapsible Stage Detail section (feature #5, collapse part)

**What's needed:** Stage Detail area in Case view can be collapsed/expanded.

**Existing patterns to use:**
- `el-collapse` / `el-collapse-item` from Element Plus — the project already has `collapseUnfold = ref(['1', '2', '3'])` and `collapseRightUnfold = ref(['1', '2', '3', '4'])` in `projectSetting.ts` specifically to track collapse state.
- Add the Stage Detail panel as a new `el-collapse-item` in the relevant component. Default expanded state is controlled by the ref value.
- A simpler `v-show` toggle with a chevron icon button also matches patterns elsewhere in the codebase if a full `el-collapse` feels heavy.

**Confidence:** HIGH — infrastructure already in place.

### 6. Case status tag position (feature #5, layout part)

**What's needed:** Move the Case status tag to the right of the Case Name.

**Pattern to use:**
- Pure layout change in the relevant component (likely `onboardingList/detail.vue`). Use `flex items-center gap-2` — the dominant Tailwind layout pattern throughout this codebase. No logic or data change.

**Confidence:** HIGH.

### 7. UpdatedBy propagation on Stage/Component update (feature #6)

**What's needed:** When a Stage or its Component is updated, also update `Workflow.ModifyBy` and `Workflow.ModifyDate`.

**Existing patterns to use:**
- `EntityBaseCreateInfo` (base class for both Workflow and Stage) carries `ModifyBy` / `ModifyDate` audit fields.
- `AuditHelper.ApplyCreateAudit()` and `InitCreateInfo()` are already used in WorkflowService to set these fields.
- In `StageService.UpdateAsync()` and any component update path, after saving the stage, load the parent Workflow and call `AuditHelper.ApplyUpdateAudit(workflow, _operatorContextService)` then save — or add a targeted `UpdateModifyInfoAsync` method to `IWorkflowRepository`.
- `IWorkflowRepository` and `IOperatorContextService` are already injected throughout the service layer.

**Confidence:** HIGH — audit infrastructure is established.

### 8. File upload metadata (UploadedBy + UploadDate) (feature #7)

**What's needed:** Record who uploaded a file and when; display those fields in the frontend.

**Existing patterns to use:**
- `QuestionnaireAnswer.Answer` is a `JToken` JSONB column (Newtonsoft.Json). The existing `uploadQuestionFile` API in `questionnaire.ts` already sends file metadata. Embed `uploadedBy` and `uploadDate` string fields into the answer JSON payload at save time.
- `UserContext` (injected throughout services) provides current user name/id for `uploadedBy`.
- Use `DateTimeOffset.UtcNow` for `uploadDate`; convert to user timezone on the frontend with the existing `timeZoneConvert()` utility.
- `ChangeLog.vue` already has a "File Upload" render branch (`v-else-if="row.type === 'File Upload' && row.fileInfo"`) — the questionnaire answer display component needs a similar render block reading the new fields from the answer JSON.
- No migration needed — embeds into existing JSONB answer column. Consistent with the project decision in PROJECT.md: "#7 UploadedBy 信息嵌入 answer JSONB".

**Confidence:** HIGH — JSONB schema is flexible, pattern is established.

### 9. Permission chain debugging (feature #11)

**What's needed:** Diagnose why User Group permission configuration does not allow users to edit Cases.

**Existing patterns to use:**
- `WorkflowPermissionService.CheckWorkflowPermission()` already emits `LogDebug` calls tracing `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, and resolved `UserTeams`. Enable Debug log level for the `FlowFlex.Application.Services.OW.Permission` namespace to expose the full chain.
- `PermissionHelpers.GetUserTeamIds()` is the method that resolves which teams the current user belongs to via IDM — this is the most likely failure point if IDM returns empty or mismatched team IDs.
- Serilog structured logging is already configured with category-based filtering.
- No new tooling needed — this is a diagnosis and fix task using the existing logging infrastructure.

**Confidence:** HIGH for tooling approach; MEDIUM for the fix itself (root cause requires runtime investigation against IDM responses).

### 10. Short Answer Grid required validation (feature #9)

**What's needed:** Change the required rule for ShortAnswerGrid questions from "all cells must be filled" to "at least one cell must be filled".

**Existing patterns to use:**
- Backend: For answer content validation, the existing pattern uses inline service logic rather than FluentValidation (since answers are dynamic JSON). Change the grid validation predicate in `QuestionnaireAnswerService` from `AllCellsFilled(grid)` to `AnyCellFilled(grid)`.
- Frontend: Element Plus `el-form` with `:rules` array is used throughout the questionnaire answer form. Change the validator function for the grid field — pure TypeScript predicate, no library change.

**Confidence:** HIGH — both validation layers use existing libraries.

### 11. Workflow status consistency for Case creation (feature #10)

**What's needed:** Workflow status shown in the Case creation selector must match status shown in the Workflow management page.

**Existing patterns to use:**
- `Workflow.Status` is a plain string field (`"active"` / `"inactive"`). The fix is aligning the query filter or DTO mapping used by the Case creation endpoint with the one used by the Workflow list endpoint. This is a data/query fix, not a stack change.

**Confidence:** HIGH for diagnosis approach; root cause requires reading the two query paths.

### 12. Checklist comment count (feature #12)

**What's needed:** Comment count on Checklist should count only Notes, not Change Log entries.

**Existing patterns to use:**
- `ChecklistTaskNote` entity is separate from `OperationChangeLog`. The count query must use `IChecklistTaskNoteRepository` exclusively, not a combined query that also includes `OperationChangeLog` records.
- `IChecklistTaskNoteService` already exists — the fix is ensuring the count method queries the right repository.

**Confidence:** HIGH.

---

## What NOT to Use

| Candidate | Why Not |
|-----------|---------|
| `structuredClone()` for deep copy | `lodash-es cloneDeep` is already the codebase standard — use the consistent tool |
| `vue-router` scroll behavior config | Only applies to route transitions; questionnaire Next does not change routes |
| date-fns or any new date library | dayjs with utc+timezone plugins is already installed and used |
| EF Core cascade delete | SqlSugar is the ORM; cascade cleanup must be explicit in service layer |
| SignalR for real-time log updates | Not in scope for OW-621 |
| New Pinia store for collapse state | Vue 3 ref() is sufficient; collapseRightUnfold ref in projectSetting.ts already handles this pattern |

---

## Confidence Assessment

| Area | Confidence | Reason |
|------|------------|--------|
| Date formatting (dayjs) | HIGH | Library confirmed, format constants defined, timeZoneConvert utility verified |
| Deep copy (C# string assignment + lodash-es) | HIGH | No special library needed; gap is in the DuplicateAsync field copy list |
| Cascade cleanup (SqlSugar + System.Text.Json) | HIGH | Stage entity and repository patterns confirmed |
| Scroll behavior (Element Plus el-scrollbar) | HIGH | el-scrollbar in use in questionnaire preview, setScrollTop is standard API |
| Collapsible UI (el-collapse + projectSetting refs) | HIGH | collapseUnfold / collapseRightUnfold refs prove existing usage pattern |
| JSONB file metadata embedding | HIGH | QuestionnaireAnswer.Answer is JToken JSONB; file upload API chain confirmed |
| Custom validation rule (FluentValidation / el-form) | HIGH | Both layers confirmed present |
| Permission debugging (Serilog + PermissionHelpers) | MEDIUM | Logging infrastructure confirmed; actual bug root cause requires runtime investigation |

---

## Sources

- `packages/flowFlex-common/package.json` — frontend dependency versions
- `packages/flowFlex-backend/Application/Application.csproj` — backend package versions
- `packages/flowFlex-backend/WebApi/WebApi.csproj` — WebApi package versions
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` — DuplicateAsync implementation (lines 847-940)
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/ChecklistLogService.cs` — log text pattern
- `packages/flowFlex-backend/Domain/Entities/OW/Stage.cs` — ComponentsJson, ChecklistId, QuestionnaireId fields confirmed
- `packages/flowFlex-backend/Domain/Entities/OW/Workflow.cs` — audit fields, Navigate relation to stages
- `packages/flowFlex-backend/Domain/Entities/OW/QuestionnaireAnswer.cs` — JToken JSONB answer field confirmed
- `packages/flowFlex-backend/Application/Services/OW/Permission/WorkflowPermissionService.cs` — permission chain debug logging pattern
- `packages/flowFlex-common/src/app/hooks/time.ts` — timeZoneConvert, dayjs timezone usage
- `packages/flowFlex-common/src/app/settings/projectSetting.ts` — date format constants, collapseUnfold refs
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/ChangeLog.vue` — frontend log rendering, File Upload branch
- `packages/flowFlex-common/src/app/views/onboard/questionnaire/components/QuestionnairePreview.vue` — el-scrollbar usage pattern
