# Architecture Patterns

**Domain:** FlowFlex OW-621 Workflow Component Enhancements
**Researched:** 2026-06-02

## Existing Architecture Context

The system is a standard layered .NET 8 + Vue 3 stack. Component boundaries are well-established:

```
Controller (WebApi) → IService (Application.Contracts) → Service (Application)
                                                              ↓
                                               IRepository (Domain) → BaseRepository (SqlSugarDB)
                                                              ↓
                                                          PostgreSQL
```

**Key structural facts confirmed from codebase:**

- `Stage.ComponentsJson` (JSONB) stores the list of `StageComponent` objects (each with `Key`, `QuestionnaireIds`, `ChecklistIds`)
- `Stage.ChecklistId` / `Stage.QuestionnaireId` are legacy scalar FK fields — `ComponentsJson` is the live source of truth
- `ChecklistStageMapping` / `QuestionnaireStageMapping` are denormalized lookup tables populated by `ComponentMappingService.SyncStageMappingsAsync`
- `OperationChangeLog` stores all audit events; `OperationTypeEnum` is the discriminator enum
- `EntityBaseCreateInfo` provides `ModifyBy` / `ModifyUserId` / `ModifyDate` on every entity
- `ChecklistTaskNote` has a `NoteType` field (default "General"; values: General, Security, Progress, Issue, etc.) — the comment-vs-changelog distinction is a query filter concern
- `WorkflowService.DuplicateAsync` already copies stages but does **not** copy `ComponentsJson` — confirmed at lines 885-906

---

## Integration Points for Each of the 12 Items

### Item 1 — ChangeLog text + date-to-seconds

**Files touched:**
- `Application/Services/OW/ChangeLog/ChecklistLogService.cs` — `BuildTaskCompletionDescription()` and `BuildTaskUncompletionDescription()` private helpers
- `Application/Services/OW/ChangeLog/BaseOperationLogService.cs` — date formatting in `BuildEnhancedOperationDescriptionAsync`
- Frontend: `src/app/views/onboard/onboardingList/components/ChangeLog.vue` — date display column template

**Data flow:** `ChecklistTaskCompletionService` calls `ChecklistLogService` → inserts `OperationChangeLog` row with description text → `ChangeLog.vue` reads and formats `operation_time` column.

**Change scope:** Pure text/format change. Backend: update two string-building private methods to use "Completed/Cancelled the task" wording. Frontend: update date format string in the table column to include seconds (`HH:mm:ss`).

---

### Item 2 — Cascade cleanup on Component delete

**Files touched:**
- `Application/Services/OW/ChecklistService.cs` — `DeleteAsync` method
- `Application/Services/OW/QuestionnaireService.cs` — `DeleteAsync` method
- `Application/Services/OW/ComponentMappingService.cs` — add `RemoveComponentFromAllStagesAsync` helper
- `Domain/Entities/OW/Stage.cs` — `ComponentsJson` JSONB mutated in-place
- SqlSugar transaction scope

**Data flow:**
```
DELETE Checklist/Questionnaire
  → ChecklistService.DeleteAsync / QuestionnaireService.DeleteAsync
    → query ChecklistStageMapping / QuestionnaireStageMapping for all stages referencing this id
    → foreach affected stage:
        deserialize ComponentsJson
        remove the component id from ChecklistIds / QuestionnaireIds list
        reserialize back to ComponentsJson
        UPDATE ff_stage
    → ComponentMappingService.SyncStageMappingsAsync(stageId)  [cleanup mapping rows]
  → soft-delete ff_checklist / ff_questionnaire (IsValid = false)
```

**Cross-cutting concern:** The JSONB mutation + mapping sync + soft-delete must run inside one SqlSugar transaction. Use `SyncStageMappingsInTransactionAsync` (already exists in `ComponentMappingService`).

---

### Item 3 — Workflow DuplicateAsync deep copy of ComponentsJson

**Files touched:**
- `Application/Services/OW/WorkflowService.cs` — `DuplicateAsync` method (lines 847-940)
- `Application/Services/OW/ComponentMappingService.cs` — `SyncStageMappingsAsync` called per new stage

**Data flow:**
```
DuplicateAsync
  → copy Workflow entity (already done)
  → foreach originalStage:
      duplicatedStage.ComponentsJson = stage.ComponentsJson   ← ADD THIS LINE
      duplicatedStage.ChecklistId = stage.ChecklistId         ← already copied
      duplicatedStage.QuestionnaireId = stage.QuestionnaireId ← already copied
      InsertAsync(duplicatedStage)
      → ComponentMappingService.SyncStageMappingsAsync(newStageId)  ← ADD
```

**Key decision (from PROJECT.md):** Components are shared by reference (same checklist/questionnaire IDs). No deep-clone of Checklist or Questionnaire entities. Only the stage-to-component mapping rows are new.

---

### Item 4 — Frontend scroll-to-top on questionnaire Next

**Files touched:**
- The internal questionnaire section-paged renderer (not the demo `CustomerQuestionnaire.vue` in sub-portal — that is a stub). Look for the component that drives `CurrentSectionIndex` advancement and calls the section save/next API.

**Data flow:** Pure frontend. The Next button click handler advances `CurrentSectionIndex`; insert `window.scrollTo({ top: 0, behavior: 'smooth' })` immediately after the section index update.

**Note:** Router-level `scrollBehavior: () => ({ left: 0, top: 0 })` in `src/app/router/index.ts` handles route transitions only — it does not fire on in-page section navigation. The scroll call must be explicit in the component.

---

### Item 5 — Case status tag position + Stage Detail collapsible

**Files touched:**
- `src/app/views/onboard/onboardingList/detail.vue` — status tag currently in `#description` slot of `PageHeader`; move inline with case name in title area
- Stage detail panel component (likely `StageCardList.vue` or the stage detail section inside `detail.vue`) — add collapse toggle

**Data flow:** Pure frontend layout change. No backend touch.

---

### Item 6 — UpdatedBy propagation to Workflow on Stage/Component update

**Files touched:**
- `Application/Services/OW/StageService.cs` — `UpdateAsync` (line 251+) and component-level update paths
- `Application/Services/OW/ChecklistService.cs` — `UpdateAsync`
- `Application/Services/OW/QuestionnaireService.cs` — `UpdateAsync`
- `Domain/Entities/Base/EntityBaseCreateInfo.cs` — `ModifyBy` / `ModifyUserId` / `ModifyDate` already on `Workflow`

**Data flow:**
```
StageService.UpdateAsync(stageId)
  → after successful stage update:
      workflow = _workflowRepository.GetByIdAsync(stage.WorkflowId)
      workflow.ModifyBy = currentUserName
      workflow.ModifyUserId = currentUserId
      workflow.ModifyDate = DateTimeOffset.UtcNow
      _workflowRepository.UpdateAsync(workflow)

ChecklistService.UpdateAsync / QuestionnaireService.UpdateAsync
  → after successful update:
      stageIds = ComponentMappingService.GetChecklistAssignmentsAsync(checklistId)
      foreach stageId → get workflowId → touch workflow ModifyBy/Date
```

**Cross-cutting concern:** Wrap the workflow touch in try/catch — do not let it roll back the primary operation. Use `IBackgroundTaskQueue` if latency is a concern (pattern already exists in `WorkflowService`).

---

### Item 7 — File upload metadata in JSONB answer

**Files touched:**
- Frontend: file upload question renderer inside the questionnaire section view — add `uploadedBy` + `uploadDate` fields to the answer payload
- `Application/Services/OW/QuestionnaireAnswerService.cs` — verify it does not strip extra fields when saving `JToken` (it stores the full `JToken` as-is — confirmed from entity definition)
- Frontend display: render `uploadedBy` + `uploadDate` when displaying file-type answers

**Data flow:**
```
Frontend file upload → answer JSON shape extended:
  {
    "fileUrl": "...",
    "fileName": "...",
    "uploadedBy": "<userName>",    ← ADD
    "uploadDate": "<ISO datetime>" ← ADD
  }
→ QuestionnaireAnswerService saves JToken unchanged into answer_json JSONB
→ Frontend display reads uploadedBy + uploadDate from answer JSON
```

No migration needed. The `answer_json` JSONB column already accepts arbitrary structure. No backend logic change required if the service passes `JToken` through without transformation.

---

### Item 8 — Remove StageSave log type

**Files touched:**
- `Application/Services/OW/OnboardingServices/OnboardingStageProgressService.cs` — remove the `await LogStageSaveAsync(...)` call at line 1284

**Data flow:** Remove the single call site. The private `LogStageSaveAsync` method and `OperationTypeEnum.StageSave = 47` can remain for backward compatibility with existing log records. No DB migration.

---

### Item 9 — Short Answer Grid required validation

**Files touched:**
- Frontend: `src/app/views/onboard/questionnaire/components/GridEditor.vue` — required validator for grid question rows
- Frontend: questionnaire section renderer's completeness check before Next/Submit
- Backend (if server-side validation exists): `QuestionnaireAnswerService` grid required check

**Data flow:** Change the required predicate from "all cells filled" to "at least one cell filled":
```
// Before (assumed)
isValid = rows.every(row => row.cells.every(cell => cell.value?.trim()))

// After
isValid = rows.some(row => row.cells.some(cell => cell.value?.trim()))
```

If backend re-validates, the same logic change applies there.

---

### Item 10 — Workflow status consistency in Case creation

**Files touched:**
- Frontend: Create Case dialog/form — the workflow selector dropdown
- `src/app/apis/ow/onboarding.ts` — confirm which endpoint the Case creation form uses for workflow list
- Backend: `WorkflowController` — verify the list endpoint used in Case creation returns the same `status` field and filtering as the management page

**Data flow:**
```
Create Case modal → fetch workflows list
  → currently may call a different endpoint or omit status field
  → fix: use same endpoint and apply same status display mapping
         (management page shows active/inactive badge using Workflow.Status string)
```

This is likely a frontend display mapping gap: the management page applies a styled tag to `Status`, while the Case creation dropdown renders the raw workflow name without status indication.

---

### Item 11 — User Group permission investigation

**Files touched (investigation-dependent):**
- `Application/Services/OW/PermissionService.cs` — coordinator; delegates to `WorkflowPermissionService`, `StagePermissionService`, `CasePermissionService`
- `Application/Services/OW/Permission/WorkflowPermissionService.cs` — team matching against `ViewTeams` / `OperateTeams` JSONB arrays
- `Application/Services/OW/Permission/CasePermissionService.cs` — case-level operate check
- External: `IdentityHubClient.GetUserGroups(userId)` — returns user's group membership
- Frontend: `src/app/views/authorityManagement/userGroup.vue`

**Data flow:**
```
Request → PermissionService.CanOperateCaseAsync(onboardingId)
  → WorkflowPermissionService: user's teams ∩ workflow.OperateTeams
  → StagePermissionService: user's teams ∩ stage.OperateTeams
  → CasePermissionService: case-level override
  → IdentityHubClient.GetUserGroups(userId) → team list
```

**Investigation points before coding:**
1. Confirm whether `OperateTeams` JSONB stores team IDs or team names — a mismatch with what IDM returns is the most likely root cause
2. Confirm `UseSameTeamForOperate` flag is applied consistently for workflows created before the flag was introduced (default `false` on old records)
3. Check if the UserGroup permission grant actually populates `OperateTeams` or a different field

---

### Item 12 — Note count: Notes only, exclude ChangeLog entries

**Files touched:**
- Backend: `ChecklistTaskService.cs` or `ChecklistService.cs` — the query that populates `notesCount` on the task DTO
- `Domain/Repository/OW/IChecklistTaskNoteRepository.cs` + implementation — add a filtered count method

**Data flow:**
```
GET /checklist-tasks (with task list)
  → service aggregates notesCount per task
  → currently: COUNT(*) FROM ff_checklist_task_note WHERE task_id = ?
  → fix: COUNT(*) WHERE task_id = ? AND note_type != 'ChangeLog'
           (confirm exact NoteType discriminator value used for changelog entries)
```

**Pre-condition:** Confirm how ChangeLog-style entries are inserted into `ff_checklist_task_note` — via `ChecklistLogService` or via note creation API — to identify the correct `NoteType` discriminator value before writing the filter.

---

## Component Boundaries Summary

| Component | Layer | Items Affected |
|-----------|-------|----------------|
| `ChecklistLogService` | Application/ChangeLog | 1, 8 |
| `BaseOperationLogService` | Application/ChangeLog | 1 |
| `OnboardingStageProgressService` | Application/Onboarding | 8 |
| `ChecklistService` | Application | 2, 6 |
| `QuestionnaireService` | Application | 2, 6 |
| `ComponentMappingService` | Application | 2, 3 |
| `WorkflowService.DuplicateAsync` | Application | 3 |
| `StageService.UpdateAsync` | Application | 6 |
| `QuestionnaireAnswerService` | Application | 7 |
| `PermissionService` + sub-services | Application/Permission | 11 |
| `ChecklistTaskService` / repository | Application | 12 |
| `OperationTypeEnum` | Domain.Shared | 8 |
| `Stage.ComponentsJson` (JSONB) | Domain | 2, 3 |
| `detail.vue` | Frontend/Views | 5, 10 |
| `ChangeLog.vue` | Frontend/Components | 1 |
| `CheckList.vue` | Frontend/Components | 12 |
| Questionnaire section renderer | Frontend/Views | 4, 9 |
| `GridEditor.vue` | Frontend/Components | 9 |
| `userGroup.vue` | Frontend/Views | 11 |
| File upload question renderer | Frontend | 7 |

---

## Suggested Build Order

Dependencies drive this order. Independent items can be parallelized within groups.

**Group A — Pure backend text/config changes (no schema, no cross-service coordination):**
1. Item 8 — Remove StageSave log (single call-site removal, zero risk)
2. Item 1 — ChangeLog text + date format (string changes in two private methods + one frontend format string)

**Group B — Pure frontend changes (no backend touch):**
3. Item 4 — Scroll to top on Next (single event handler addition)
4. Item 5 — Status tag position + collapsible stage detail (layout restructuring)
5. Item 10 — Workflow status in Case creation (frontend display mapping fix)

**Group C — Single-service logic additions, no new tables:**
6. Item 9 — Grid required validation (frontend predicate change + optional backend mirror)
7. Item 12 — Note count filter (single repository query change)
8. Item 7 — File upload metadata in JSONB (frontend adds fields; backend passes through)
9. Item 6 — UpdatedBy propagation (add workflow touch after stage/checklist/questionnaire update)

**Group D — Cross-service coordination, transaction discipline required:**
10. Item 3 — Workflow DuplicateAsync deep copy (add `ComponentsJson` copy + mapping sync)
11. Item 2 — Cascade cleanup on Component delete (transaction across 3 tables; build after Item 3 proves mapping sync pattern)

**Group E — Investigation before coding:**
12. Item 11 — User Group permission (investigate IDM team ID vs name matching before writing any fix)

**Rationale:**
- Items 8 and 1 are lowest-risk removals/text changes — do first to clear log noise
- Pure frontend items (4, 5, 10) are independent of backend changes and can be reviewed in parallel
- Item 3 (duplicate + mapping sync) should precede Item 2 (cascade delete) because both use `ComponentMappingService.SyncStageMappingsInTransactionAsync` and proving the pattern on the simpler case first reduces risk
- Item 11 requires reading actual data (IdentityHub group IDs vs stored team names) before a fix can be designed — do not guess the fix

---

## Cross-Cutting Concerns

**Transactions (Items 2 and 3):**
Both mutate `ComponentsJson` on multiple Stage rows and must keep `ChecklistStageMapping` / `QuestionnaireStageMapping` consistent. Use `ISqlSugarClient` transaction scope. `ComponentMappingService.SyncStageMappingsInTransactionAsync` already accepts a transaction client — use that overload, not the non-transactional version.

**JSONB mutation safety (Items 2, 3, 7):**
`ComponentsJson` uses `System.Text.Json`. The `TryUnwrapComponentsJson` private helper in `ComponentMappingService` handles double-encoded strings — reuse it. For `QuestionnaireAnswer.Answer` (Item 7), the field is `JToken` (Newtonsoft) — stay consistent with the library already used in that file.

**Cache invalidation (Items 2, 3, 6):**
`StageService` uses cache key `ow:stage:workflow:{workflowId}` with 10-minute TTL. Any operation that mutates `Stage.ComponentsJson` or workflow audit fields must call `_cacheService.RemoveAsync(cacheKey)` after the DB write. This pattern already exists in `StageService.UpdateAsync` at the end of the transaction block.

**Audit field propagation (Item 6):**
`EntityBaseCreateInfo.ModifyBy` + `ModifyUserId` + `ModifyDate` exist on all entities. Check whether `AuditHelper.ApplyUpdateAudit` already sets these before adding new code.

**No new migrations needed:**
All 12 items work within the existing schema. JSONB columns accept the extended structures. `OperationTypeEnum.StageSave` stays in the enum for historical log record compatibility. No `ALTER TABLE` statements required for any item.

---

## Anti-Patterns to Avoid

**JSONB table scan for component lookup (Item 2):**
Do not iterate all stages and deserialize `ComponentsJson` to find which stages reference a deleted component. Use `ChecklistStageMapping` / `QuestionnaireStageMapping` — they exist for this purpose.

**Letting the workflow ModifyBy touch fail the primary operation (Item 6):**
The workflow audit touch is a side effect of stage/component updates. Wrap in try/catch + log warning. Do not let it roll back the stage update transaction.

**Bypassing multi-tenancy filter during cascade (Item 2):**
Do not use `.Filter(null, true)` when scanning stages for component references. Cascade cleanup must stay tenant-scoped — the global filter handles this automatically if not bypassed.

**Guessing the permission root cause (Item 11):**
The permission system has three nested layers (Workflow ∩ Stage ∩ Case) plus `UseSameTeamForOperate` flag semantics. Writing a fix before confirming the data mismatch will likely add incorrect logic. Investigate first with actual UserGroup + OperateTeams data.

---

## Sources

- Direct codebase inspection: `WorkflowService.cs`, `StageService.cs`, `ChecklistLogService.cs`, `BaseOperationLogService.cs`, `ComponentMappingService.cs`, `ChecklistTaskCompletionService.cs`, `ChecklistTaskNoteService.cs`, `PermissionService.cs`, `OnboardingStageProgressService.cs`
- Entity inspection: `Workflow.cs`, `Stage.cs`, `OperationChangeLog.cs`, `QuestionnaireAnswer.cs`, `Checklist.cs`, `ChecklistTaskNote.cs`, `EntityBaseCreateInfo.cs`
- Frontend inspection: `detail.vue`, `ChangeLog.vue`, `CheckList.vue`, `CustomerQuestionnaire.vue`, `router/index.ts`
- Enum inspection: `OperationTypeEnum.cs` (confirmed `StageSave = 47`, `ChecklistTaskComplete = 1`, `ChecklistTaskUncomplete = 2`)
- Confidence: HIGH — all findings from direct source code inspection
