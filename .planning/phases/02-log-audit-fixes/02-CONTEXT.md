# Phase 2: Log & Audit Fixes - Context

**Gathered:** 2026-06-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit logs are accurate and clean: correct Checklist action text, second-level timestamps with user timezone, no StageSave noise, correct Notes-only comment counts, questionnaire Next scrolls to top, and Workflow status display consistent with management page.

Requirements: LOG-01, LOG-02, LOG-03, LOG-04, UX-01, DATA-03 (6 items)

</domain>

<decisions>
## Implementation Decisions

### Timestamp Display
- **D-01:** Use user timezone via existing `timeZoneConvert()` utility — store UTC, display converted per user's browser timezone
- **D-02:** Format: `MM/DD/YYYY HH:mm:ss` using existing `projectTenMinutesSsecondsDate` constant (or equivalent seconds-precision format)

### Scroll Behavior
- **D-03:** Use `window.scrollTo({ top: 0, behavior: 'smooth' })` after `await nextTick()` in questionnaire Next handler — full page scroll, not container scroll

### Notes Count Filter
- **D-04:** Query staging DB first to identify exact `NoteType` discriminator values before implementing the count filter in `ChecklistTaskService.GetNotesCountByTaskIdsAsync`
- **D-05:** Must also filter `is_deleted = false` explicitly since `ChecklistTaskNote` uses non-standard `IsDeleted` flag (global SqlSugar filter won't apply)

### Log Text Changes
- **D-06:** Change only `operationDescription` text in `ChecklistLogService` — do NOT modify `OperationType` enum values (they are stored in DB)
- **D-07:** `OperationTypeEnum.StageSave = 47` single call site in `OnboardingStageProgressService` line ~1284 — remove the call, not the enum value

### Workflow Status Alignment
- **D-08:** Ensure the Workflow dropdown in Case creation fetches/displays `isActive` status matching the Workflow management list API — likely a frontend data source alignment issue

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Backend - ChangeLog Service
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/ChecklistLogService.cs` — Contains `BuildTaskCompletionDescription` / `BuildTaskUncompletionDescription` methods to modify

### Backend - Stage Progress
- `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingStageProgressService.cs` — Contains `LogStageSaveAsync` call site to remove (~line 1284)

### Backend - Checklist Task Count
- `packages/flowFlex-backend/Application/Services/OW/ChecklistTaskService.cs` — Contains `GetNotesCountByTaskIdsAsync` to filter

### Frontend - Questionnaire Form
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` — Next button scroll fix

### Frontend - Case Creation
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/index.vue` — Workflow dropdown (fetchAllWorkflows)

### Frontend - ChangeLog Display
- ChangeLog.vue component — date format column to use seconds-precision format + `timeZoneConvert()`

### Research
- `.planning/research/SUMMARY.md` — Full research synthesis with build order and pitfalls

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `timeZoneConvert()` utility: already handles UTC→user timezone conversion
- `projectTenMinutesSsecondsDate` (or similar): seconds-precision date format constant
- `OperationTypeEnum`: enum with `StageSave = 47` value

### Established Patterns
- ChangeLog text is set in service-layer description builders, not in enum values
- `ChangeLog.vue` uses `getTagType(row.operationType)` for tag styling — only text changes, not type
- SqlSugar global filter applies `IsValid` but NOT `IsDeleted` on `ChecklistTaskNote`

### Integration Points
- `ChecklistLogService` → ChangeLog display in frontend
- `OnboardingStageProgressService` → StageSave log writes
- `ChecklistTaskService` → task detail comment count badge
- `dynamicForm.vue` Next handler → page section navigation
- Case creation Workflow dropdown → Workflow list API

</code_context>

<specifics>
## Specific Ideas

- JIRA ticket specifies exact text: "Completed the task" / "Cancelled the task"
- Date format example from JIRA: `04/20/2026 14:20:47`
- Comment count badge (①) should match Notes tab (②) count exactly

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 2-Log & Audit Fixes*
*Context gathered: 2026-06-02*
