# Phase 2: Log & Audit Fixes - Research

**Researched:** 2026-06-02
**Domain:** Backend C# log service + Frontend Vue date formatting + UX scroll behavior
**Confidence:** HIGH — all findings verified directly from source files

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Use user timezone via existing `timeZoneConvert()` utility — store UTC, display converted per user's browser timezone
- **D-02:** Format: `MM/DD/YYYY HH:mm:ss` using existing `projectTenMinutesSsecondsDate` constant
- **D-03:** Use `window.scrollTo({ top: 0, behavior: 'smooth' })` after `await nextTick()` in questionnaire Next handler
- **D-04:** Query staging DB first to identify exact `NoteType` discriminator values before implementing the count filter
- **D-05:** Must also filter `is_deleted = false` explicitly since `ChecklistTaskNote` uses non-standard `IsDeleted` flag
- **D-06:** Change only `operationDescription` text in `ChecklistLogService` — do NOT modify `OperationType` enum values
- **D-07:** `OperationTypeEnum.StageSave = 47` single call site in `OnboardingStageProgressService` line ~1284 — remove the call, not the enum value
- **D-08:** Ensure the Workflow dropdown in Case creation fetches/displays `isActive` status matching the Workflow management list API

### Claude's Discretion
None specified.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LOG-01 | Checklist Done 操作日志显示 "Completed the task" + 时间精确到秒 (MM/DD/YYYY HH:mm:ss) | Text is in `BuildTaskCompletionDescription`; timestamp format already exists as `projectTenMinutesSsecondsDate` |
| LOG-02 | Checklist Cancel 操作日志显示 "Cancelled the task" + 时间精确到秒 | Text is in `BuildTaskUncompletionDescription`; same timestamp path as LOG-01 |
| LOG-03 | StageSave 类型的 Change Log 不再写入 | Single call site confirmed at line 1284 of `OnboardingStageProgressService.cs`; `LogStageSaveAsync` method can be removed |
| LOG-04 | Checklist Task 的 comment 计数只统计 Notes 类型，不计入 Change Log 条目 | `CountByTaskIdAsync` has no `NoteType` filter; `NoteType` is a plain string field on `ChecklistTaskNote` |
| UX-01 | 问卷点击 Next 后页面自动滚动到顶部 | `goToNextSection` in `dynamicForm.vue` has no scroll call; `window.scrollTo` must be added after `currentSectionIndex.value++` |
| DATA-03 | 创建 Case 选择 Workflow 时，下拉框中的 Active/Inactive 状态与 Workflow 管理页面一致 | Both pages call the same `getWorkflowList` API; Case page renders `isActive` correctly but does NOT filter — all workflows are shown including inactive ones with an "Inactive" tag; management page shows the same set filtered by search params |
</phase_requirements>

---

## Summary

Phase 2 is a set of six targeted, low-risk fixes across the backend log service, one repository method, and two frontend components. No schema migrations are needed. No new packages are required. Every change touches a precisely located code path verified by direct file inspection.

The backend changes (LOG-01 through LOG-04) are entirely within `ChecklistLogService.cs`, `OnboardingStageProgressService.cs`, and `ChecklistTaskService.cs` / `ChecklistTaskNoteRepository.cs`. The frontend changes (UX-01, DATA-03) are in `dynamicForm.vue` and `onboardingList/index.vue`. All six items are independent of each other and can be implemented in any order.

The main subtlety is LOG-04: the `NoteType` field on `ChecklistTaskNote` is a plain `string` (not an enum), defaulting to `"General"`. Change-log entries are stored in a completely separate table (`ff_operation_change_log`) and do not appear in `ff_checklist_task_note` at all — so the fix is simply adding a `NoteType == "Notes"` filter to `CountByTaskIdAsync`. Decision D-04 says to query staging DB first to confirm the discriminator string value before hardcoding it.

**Primary recommendation:** Implement in backend-first order — LOG-01/02 (text-only), LOG-03 (remove call), LOG-04 (add filter), then UX-01 (scroll), then DATA-03 (dropdown investigation).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Checklist log text (LOG-01/02) | API / Backend | — | Log text is constructed in `ChecklistLogService.cs` service layer |
| Timestamp precision (LOG-01/02) | Frontend | — | `formatDateTime` in `ChangeLog.vue` already uses `projectTenMinutesSsecondsDate`; no backend change needed |
| StageSave removal (LOG-03) | API / Backend | — | Call site is in `OnboardingStageProgressService.cs` |
| Notes count filter (LOG-04) | Database / Storage | API / Backend | Fix is in the SqlSugar query in `ChecklistTaskNoteRepository.cs`, surfaced via `ChecklistTaskService.cs` |
| Questionnaire scroll (UX-01) | Browser / Client | — | `goToNextSection` is a client-side nav function in `dynamicForm.vue` |
| Workflow status in dropdown (DATA-03) | Browser / Client | — | `fetchAllWorkflows` in `index.vue` fetches all workflows; display is already correct; issue is whether filtering is needed |

---

## LOG-01/LOG-02: Checklist Log Text + Timestamp

### Current State

`ChecklistLogService.cs` — `BuildTaskCompletionDescription` (line ~413):
```csharp
// CURRENT
var description = $"Task '{taskName}' has been marked as completed by {GetOperatorDisplayName()}";
```

`BuildTaskUncompletionDescription` (line ~433):
```csharp
// CURRENT
var description = $"Task '{taskName}' has been marked as uncompleted by {GetOperatorDisplayName()}";
```

The `operationTitle` (not `operationDescription`) is set separately in `LogChecklistTaskCompleteAsync`:
```csharp
$"Checklist Task Completed: {taskName}"   // operationTitle
BuildTaskCompletionDescription(...)        // operationDescription
```

### Required Changes

Per JIRA spec: "Completed the task" / "Cancelled the task". Decision D-06 says change only `operationDescription` text, not `OperationType` enum values.

```csharp
// LOG-01: BuildTaskCompletionDescription target text
private string BuildTaskCompletionDescription(string taskName, string completionNotes, int actualHours)
{
    return "Completed the task";
}

// LOG-02: BuildTaskUncompletionDescription target text
private string BuildTaskUncompletionDescription(string taskName, string reason)
{
    return "Cancelled the task";
}
```

### Timestamp Display (D-01, D-02)

The `ChangeLog.vue` `formatDateTime` function (line 1074) **already** uses the correct format and timezone conversion:

```typescript
// Source: packages/flowFlex-common/src/app/views/onboard/onboardingList/components/ChangeLog.vue line 1074
const formatDateTime = (dateString: string): string => {
    try {
        return dateString
            ? timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate)
            : defaultStr;
    } catch {
        return dateString || defaultStr;
    }
};
```

`projectTenMinutesSsecondsDate` is defined as `'MM/DD/YYYY HH:mm:ss'` in `src/app/settings/projectSetting.ts` line 33. [VERIFIED: direct file read]

The `Date & Time` column in `ChangeLog.vue` (line 665) calls `formatDateTime(row.dateTime)` — already correct.

**Conclusion for LOG-01/02 timestamps:** No frontend change needed. The display format is already seconds-precision with timezone conversion. The backend stores UTC (`DateTimeOffset.UtcNow`), which is correct.

---

## LOG-03: Remove StageSave Log

### Call Site — Confirmed

`OnboardingStageProgressService.cs` lines 1279–1285 (single call site):

```csharp
var result = await SafeUpdateOnboardingAsync(onboarding);

// Log stage save to operation_change_log
if (result)
{
    await LogStageSaveAsync(onboarding, stageId, stageProgress);  // LINE 1284 — REMOVE THIS
}

return result;
```

The `LogStageSaveAsync` private method lives at lines 1302–1357 in the same file.

### What to Remove

Per D-07: remove only the call at line 1284. Also remove the `LogStageSaveAsync` private method (lines 1302–1357) as it becomes dead code. Do NOT remove `OperationTypeEnum.StageSave = 47` — the enum value may be referenced elsewhere or exist in historical log data.

### Risk

Low. `LogStageSaveAsync` is private with a single call site. The method itself has a `try/catch` that suppresses exceptions, so its removal cannot break the main save flow (line 1354: "Don't re-throw to avoid breaking the main flow").

---

## LOG-04: Notes Count Filter

### Current Repository Implementation

`ChecklistTaskNoteRepository.cs` `CountByTaskIdAsync` (line 124):

```csharp
public async Task<int> CountByTaskIdAsync(long taskId)
{
    return await base.db.Queryable<ChecklistTaskNote>()
        .LeftJoin<Onboarding>((ctn, ob) => ctn.OnboardingId == ob.Id)
        .Where((ctn, ob) => ctn.TaskId == taskId && !ctn.IsDeleted && ob.IsValid)
        .CountAsync();
}
```

This counts ALL note types. It already correctly filters `IsDeleted = false` and joins to active onboardings.

### NoteType Field

`ChecklistTaskNote` entity (line 37):
```csharp
[SugarColumn(ColumnName = "note_type")]
public string NoteType { get; set; } = "General";
```

NoteType is a plain `string`, not an enum. Known values from code: `"General"`. The value used for user-visible notes in the Notes tab must be confirmed from DB before hardcoding (Decision D-04). The typical convention would be `"Notes"` or `"General"` — verify against staging data.

### Service Layer

`ChecklistTaskService.cs` `GetNotesCountByTaskIdsAsync` (line 1063) calls `CountByTaskIdAsync` in a loop per task ID. The fix must be made either in the repository method signature (add a `noteType` parameter) or via a new dedicated method. Recommended: add an optional `noteType` filter param to `CountByTaskIdAsync`.

### Interface Change Required

`IChecklistTaskNoteRepository.cs` line 53:
```csharp
Task<int> CountByTaskIdAsync(long taskId);
```
Must be updated to accept the filter:
```csharp
Task<int> CountByTaskIdAsync(long taskId, string noteType = null);
```

And service call updated:
```csharp
var count = await _noteRepository.CountByTaskIdAsync(taskId, "Notes"); // or confirmed type string
```

### Pre-Implementation Step Required

Per D-04: query staging DB (`wfe-flowflex-postgres-staging`) before implementing:
```sql
SELECT DISTINCT note_type FROM ff_checklist_task_note LIMIT 20;
```
Confirm the exact string value that represents user-created notes (vs change-log entries, if any are stored in this table at all).

**Important:** Change-log entries live in `ff_operation_change_log`, a completely separate table. `ff_checklist_task_note` only stores user notes. The discriminator value to filter for is whatever the Notes tab displays — likely `"General"` (the default). The comment count badge discrepancy may be caused by notes with `is_deleted = true` being counted, not by NoteType mixing. Verify both hypotheses against DB data.

---

## UX-01: Questionnaire Scroll to Top

### Current State

`dynamicForm.vue` `goToNextSection` (line 1559):

```typescript
const goToNextSection = async () => {
    // ... jump rule logic ...
    const targetSectionId = getJumpTargetSection();
    if (targetSectionId) {
        const targetSectionIndex = findSectionIndexById(targetSectionId);
        if (targetSectionIndex !== -1) {
            currentSectionIndex.value = ...;
            return;   // <-- early return path, no scroll
        }
    }

    // default next section
    if (!isLastSection.value) {
        currentSectionIndex.value++;
    }
    // <-- no scroll call here either
};
```

`goToPreviousSection` (line 1553) similarly has no scroll. The Next button template calls `@click="goToNextSection"`.

### Required Change

Decision D-03: `window.scrollTo({ top: 0, behavior: 'smooth' })` after `await nextTick()`.

All navigation paths in `goToNextSection` must trigger scroll — both the jump-rule path (early return) and the default path. Also apply to `goToSection` (line 1591) and `goToPreviousSection` (line 1553) for consistent UX:

```typescript
const scrollToTop = () => {
    nextTick(() => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
};

const goToNextSection = async () => {
    const targetSectionId = getJumpTargetSection();
    if (targetSectionId) {
        const targetSectionIndex = findSectionIndexById(targetSectionId);
        if (targetSectionIndex !== -1) {
            currentSectionIndex.value = ...;
            scrollToTop();  // ADD
            return;
        }
    }
    if (!isLastSection.value) {
        currentSectionIndex.value++;
    }
    scrollToTop();  // ADD
};
```

`nextTick` is already imported at line 670: `import { ref, computed, onMounted, watch, nextTick, readonly } from 'vue';`

### Risk

`window.scrollTo` scrolls the entire page. If the questionnaire is rendered inside a scrollable container (not `window`), this will have no visible effect. Verify in browser that the questionnaire form scrolls via `window` and not a parent `overflow: auto` div. If it is a container scroll, the container ref will need to be targeted instead.

---

## DATA-03: Workflow Status Alignment

### Investigation Findings

Both the Case creation page (`onboardingList/index.vue`) and the Workflow management page (`workflow/index.vue`) call the same `getWorkflowList` API function from `@/apis/ow/index.ts`:

```typescript
// apis/ow/index.ts line 61
export function getWorkflowList(params?: any) {
    return defHttp.get({ url: `${Api().workflows}`, params });
}
```

The Case creation `fetchAllWorkflows` (line 2111) calls it with **no params** — returns all workflows including inactive.

The Workflow management page `fetchWorkflows` (line 863) calls it with **search params** (name, status filters from UI) — but by default also returns all.

### Dropdown Rendering

The Case creation dropdown already renders `isActive` status correctly (lines 622–635):

```html
<el-option
    v-for="workflow in allWorkflows"
    :key="workflow.id"
    :label="workflow.name"
    :value="workflow.id"
    :disabled="!workflow.isActive"
>
    <div class="flex items-center justify-between">
        <span>{{ workflow.name }}</span>
        <el-tag v-if="!workflow.isActive" type="danger" size="small">Inactive</el-tag>
        <el-tag v-else type="success" size="small">Active</el-tag>
    </div>
</el-option>
```

Inactive workflows are shown but disabled (`:disabled="!workflow.isActive"`).

### Root Cause Hypothesis

The `WorkflowOutputDto` has both `Status` (string: `"active"/"inactive"`) and `IsActive` (bool). The backend `ActivateWorkflow` sets both: `IsActive = true` and `Status = "active"`. The `DeactivateWorkflow` path sets `Status = "inactive"` and `IsDefault = false` via `updateWorkflow`. However, `IsActive` in the deactivate path is set via the update DTO field (`isActive: false` sent from frontend — line 1427 of workflow/index.vue does NOT explicitly set `isActive: false` in the params object, only `status: 'inactive'`).

This means a deactivated workflow may have `Status = "inactive"` but `IsActive = true` in the DB if the backend `UpdateAsync` doesn't derive `IsActive` from `Status`.

### What to Verify Against DB

```sql
SELECT id, name, status, is_active 
FROM ff_workflow 
WHERE is_valid = true 
ORDER BY modify_date DESC LIMIT 20;
```

If rows exist with `status = 'inactive'` AND `is_active = true`, the backend `UpdateAsync` is not keeping them in sync, and the dropdown correctly reads `isActive` while the management page may filter by `status`. That's the DATA-03 mismatch.

### Fix Options

1. **Frontend only:** In `fetchAllWorkflows`, map `isActive` from `status`: `workflow.isActive = workflow.status?.toLowerCase() === 'active'`. Simple but treats symptom.
2. **Backend only:** In `WorkflowService.UpdateAsync`, add `entity.IsActive = input.Status?.ToLower() == "active"` to keep fields in sync. Preferred — single source of truth.
3. **Both:** Update backend to sync fields AND confirm frontend reads `isActive`.

Decision D-08 says ensure alignment — backend sync is the correct fix.

---

## Implementation Risks

### Risk 1: NoteType string value unknown at research time
**What:** The exact string used to identify user-created notes vs other entries in `ff_checklist_task_note` is not verifiable from code alone — it's stored data.
**Mitigation:** D-04 mandates a DB query before implementation. Do not hardcode without confirming.

### Risk 2: Scroll target may not be `window`
**What:** If `dynamicForm.vue` renders inside a scrollable panel with `overflow: auto`, `window.scrollTo` silently does nothing.
**Mitigation:** Test in browser after implementation. If no scroll occurs, find the scrollable container and target it with a `ref`.

### Risk 3: DATA-03 root cause is backend sync bug
**What:** The `deactivateWorkflow` path in `workflow/index.vue` sends `status: 'inactive'` but not `isActive: false` explicitly (line 1427 only sets `status` and `isDefault`). If backend `UpdateAsync` doesn't reconcile `IsActive` from `Status`, workflows can be in an inconsistent state.
**Mitigation:** Run the DB verification query above. If confirmed, fix in `WorkflowService.UpdateAsync`.

### Risk 4: `LogStageSaveAsync` removal leaves orphan `_stageRepository` usage
**What:** `LogStageSaveAsync` calls `_stageRepository.GetByIdAsync`. If this is the only call site for `_stageRepository` in the file, the field becomes unused after removal.
**Mitigation:** Search for other `_stageRepository` usages in `OnboardingStageProgressService.cs` before removing the method; remove the private field if no other usages remain.

---

## Recommended Plan Structure

Based on the above, the plan should have these tasks in order:

**Wave 0 — Pre-work:**
- T01: DB query on staging to confirm `NoteType` discriminator values (manual step, blocks LOG-04)

**Wave 1 — Backend fixes (independent):**
- T02: LOG-01 — Change `BuildTaskCompletionDescription` to return `"Completed the task"`
- T03: LOG-02 — Change `BuildTaskUncompletionDescription` to return `"Cancelled the task"`
- T04: LOG-03 — Remove `LogStageSaveAsync` call at line 1284; remove the private method; check `_stageRepository` orphan
- T05: LOG-04 — Add `noteType` param to `CountByTaskIdAsync` interface + implementation; update service call with confirmed type string

**Wave 2 — Frontend fixes (independent):**
- T06: UX-01 — Add `scrollToTop()` helper to `goToNextSection`, both code paths; also `goToPreviousSection` and `goToSection` for consistency
- T07: DATA-03 — Verify DB state; fix `WorkflowService.UpdateAsync` to sync `IsActive` from `Status` on update

**Verification per task:** Build + unit tests after each backend task. Browser smoke test after each frontend task.

---

## Environment Availability

Step 2.6: SKIPPED — phase is purely code/config changes with no new external dependencies.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `NoteType = "Notes"` is the discriminator for user-visible notes in `ff_checklist_task_note` | LOG-04 | Wrong type string silently filters out all notes or no notes; D-04 mandates DB verification |
| A2 | `goToNextSection` renders inside a `window`-scrollable page, not a container with `overflow: auto` | UX-01 | `window.scrollTo` has no effect; need container ref instead |
| A3 | DATA-03 root cause is `IsActive/Status` sync gap in `UpdateAsync`, not a different API endpoint | DATA-03 | Fix wrong code path; DB query must confirm before implementing |

---

## Sources

### Primary (HIGH confidence — direct file reads)
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/ChecklistLogService.cs` — `BuildTaskCompletionDescription`, `BuildTaskUncompletionDescription` exact text
- `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingStageProgressService.cs` lines 1279–1357 — `LogStageSaveAsync` call site and method body
- `packages/flowFlex-backend/Application/Services/OW/ChecklistTaskService.cs` lines 1063–1074 — `GetNotesCountByTaskIdsAsync`
- `packages/flowFlex-backend/SqlSugarDB/Repositories/OW/ChecklistTaskNoteRepository.cs` lines 124–130 — `CountByTaskIdAsync`
- `packages/flowFlex-backend/Domain/Entities/OW/ChecklistTaskNote.cs` — `NoteType` field definition
- `packages/flowFlex-backend/Domain/Repository/OW/IChecklistTaskNoteRepository.cs` — interface signature
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/ChangeLog.vue` lines 1074–1082 — `formatDateTime` implementation
- `packages/flowFlex-common/src/app/settings/projectSetting.ts` line 33 — `projectTenMinutesSsecondsDate` value
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` lines 1559–1589 — `goToNextSection` implementation
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/index.vue` lines 2111–2120, 614–636 — `fetchAllWorkflows`, dropdown rendering
- `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Workflow/WorkflowOutputDto.cs` — `IsActive` and `Status` fields
- `packages/flowFlex-backend/Domain/Entities/OW/Workflow.cs` — entity `IsActive` and `Status` fields
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` lines 750–814 — `ActivateWorkflow`/`DeactivateWorkflow` logic

## Metadata

**Confidence breakdown:**
- Backend log text changes (LOG-01/02): HIGH — exact method bodies read
- StageSave removal (LOG-03): HIGH — single call site confirmed, method body read
- Notes count filter (LOG-04): HIGH for fix location; LOW for exact `NoteType` string (requires DB query)
- Scroll fix (UX-01): HIGH for location; MEDIUM for scroll target (window vs container — requires browser test)
- Workflow status alignment (DATA-03): HIGH for fix location; MEDIUM for root cause (requires DB query to confirm)

**Research date:** 2026-06-02
**Valid until:** 2026-07-02 (stable codebase, no fast-moving dependencies)
