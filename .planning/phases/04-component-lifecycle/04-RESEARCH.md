# Research: Phase 4 — Component Lifecycle & Propagation

**Researched:** 2026-06-04
**Domain:** .NET 8 / SqlSugar ORM — cascade soft-delete, JSONB mutation, workflow duplication, audit propagation
**Confidence:** HIGH (all findings from direct codebase inspection)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Cascade Delete Cleanup (COMP-01, COMP-02)**
- D-01: When a Checklist or Questionnaire is soft-deleted (IsValid=false), immediately clean all Stage references in the same transaction
- D-02: Cleanup must cover: Stage.ChecklistId/QuestionnaireId FK → set null, Stage.ComponentsJson → remove the deleted component entry, ChecklistStageMapping/QuestionnaireStageMapping → remove rows
- D-03: Use `SyncStageMappingsInTransactionAsync` (accepts transaction client) for mapping table sync — NOT the fire-and-forget version
- D-04: All operations wrapped in one SqlSugar transaction (soft-delete + JSONB update + mapping cleanup + cache invalidation)
- D-05: Invalidate stage cache key `ow:stage:workflow:{workflowId}` after mutation
- D-06: Use mapping tables (`ChecklistStageMapping`/`QuestionnaireStageMapping`) to find affected Stages — do NOT full-table scan ComponentsJson
- D-07: Stay tenant-scoped — never use `.Filter(null, true)` during cascade delete

**Workflow Duplicate Deep Copy (COMP-03)**
- D-08: Extend `DuplicateAsync` stage copy block to include: `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, `PortalPermission`, `VisibleInPortal`
- D-09: Call `SyncStageMappingsAsync(newStageId)` per duplicated stage after copy
- D-10: Shared references are the intended behavior — new Workflow's Stages point to the same Checklist/Questionnaire entities as the original
- D-11: No user prompt or warning after Duplicate — shared references are accepted as-is
- D-12: Editing a Component in the copy WILL affect the original (accepted trade-off, not a bug)

**UpdatedBy Propagation (LOG-05)**
- D-13: Add a lightweight `TouchWorkflowAuditAsync(workflowId)` repo method that only updates `ModifyBy`/`ModifyDate` columns — do NOT call `WorkflowService.UpdateAsync`
- D-14: Call `TouchWorkflowAuditAsync` from: `StageService.UpdateAsync`, `ChecklistService.UpdateAsync`, `QuestionnaireService.UpdateAsync`
- D-15: Wrap in try/catch — on failure, log warning and continue (do NOT roll back the Stage/Component save)
- D-16: Only user-initiated saves propagate — background/system-triggered updates do not

### Claude's Discretion

None specified.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| COMP-01 | 删除 Checklist 时，自动清除关联 Stage 的 ChecklistId 引用和 ComponentsJson 中的对应条目 | `_mappingService.GetStagesUsingChecklistFastAsync` gives affected stage IDs; `SyncStageMappingsInTransactionAsync` handles mapping table; JSONB patch is deserialize→filter→reserialize |
| COMP-02 | 删除 Questionnaire 时，自动清除关联 Stage 的 QuestionnaireId 引用和 ComponentsJson 中的对应条目 | Same pattern as COMP-01 using `GetStagesUsingQuestionnaireFastAsync` |
| COMP-03 | Duplicate Workflow 时，新 Stage 的 ComponentsJson、ViewPermissionMode、ViewTeams、OperateTeams 完整复制，并调用 SyncStageMappingsAsync | DuplicateAsync lines 888-910 confirmed missing 8 fields; `SyncStageMappingsAsync` is the non-transactional variant appropriate here |
| LOG-05 | 更新 Stage 或 Component 时，所属 Workflow 的 UpdatedBy/UpdateDate 同步更新 | `db.Updateable<Workflow>().SetColumns(...)` targeting only `modify_by` and `modify_date` columns is the correct pattern, mirroring `SetDefaultWorkflowAsync` |
</phase_requirements>

---

## Summary

Phase 4 is entirely backend C#/.NET 8. It touches four integration points across three services. All required infrastructure already exists — no new tables, no migrations, no new service classes needed. The work is wiring existing pieces together correctly.

The riskiest item is COMP-01/COMP-02: they require a SqlSugar transaction that spans multiple entity types (Checklist/Questionnaire soft-delete + Stage JSONB mutation + mapping table delete + cache invalidation). The transaction pattern already exists in `StageService.UpdateAsync` (`_db.Ado.UseTranAsync`) and `SyncStageMappingsInTransactionAsync` already accepts the transaction client — the cascade delete just needs to compose these correctly.

COMP-03 is the lowest-risk item: it is a pure property copy extension in a single `foreach` block. Eight fields are provably absent from the current copy; adding them is straightforward.

LOG-05 requires a new `TouchWorkflowAuditAsync` method added to `IWorkflowRepository` + `WorkflowRepository`, then called from three `UpdateAsync` methods wrapped in try/catch. The `SetColumns` column-targeted update pattern already exists in `WorkflowRepository` and is safe to reuse.

**Primary recommendation:** Implement in order COMP-03 (lowest risk, no transaction), then LOG-05 (additive, try/catch isolated), then COMP-01/COMP-02 together (shared transaction pattern, highest risk).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Cascade delete cleanup | API / Backend (Application Services) | Database / Storage (mapping tables) | Business rule lives in service layer; mapping tables are the source of truth for reverse lookup |
| JSONB ComponentsJson mutation | API / Backend (Application Services) | Database / Storage (PostgreSQL JSONB) | Deserialization/mutation in C# service, persisted to JSONB column via SqlSugar |
| Workflow duplicate deep copy | API / Backend (Application Services) | — | Single service method, no cross-tier concerns |
| UpdatedBy audit propagation | API / Backend (Application Services) | Database / Storage (Repository layer) | Thin repo method targets specific DB columns; service layer calls it |
| Stage cache invalidation | API / Backend (Application Services) | — | `IDistributedCacheService.RemoveAsync` called post-mutation, already established pattern |

---

## COMP-01/COMP-02: Cascade Delete Cleanup

### Current State of DeleteAsync

**ChecklistService.DeleteAsync** (lines 317–353):
- Soft-deletes via `checklist.IsValid = false` + `_checklistRepository.UpdateAsync(checklist)`
- Logs delete via background queue
- Does NOT touch any Stage references or mapping tables

**QuestionnaireService.DeleteAsync** (lines 391–433):
- Hard-deletes via `_questionnaireRepository.DeleteAsync(entity)` — NOTE: this is a hard delete, not a soft delete
- Logs delete via background queue
- Does NOT touch any Stage references or mapping tables
- Blocks delete if `entity.Status == "Published"`

**Critical difference:** Checklist uses `IsValid = false` (soft), Questionnaire uses `DeleteAsync` (hard). The cascade cleanup must fire correctly in both cases.

### Reverse Lookup: How to Find Affected Stages

`IComponentMappingService` exposes two dedicated methods (confirmed in interface):

```csharp
Task<List<long>> GetStagesUsingChecklistFastAsync(long checklistId);
Task<List<long>> GetStagesUsingQuestionnaireFastAsync(long questionnaireId);
```

These query `ChecklistStageMapping` / `QuestionnaireStageMapping` tables — fast, no full-table scan of `ff_stage`. This satisfies D-06 exactly.

### Stage Entity Fields to Clean

From `Stage.cs`:
- `ChecklistId` (nullable `long?`, column `checklist_id`) — set to `null`
- `QuestionnaireId` (nullable `long?`, column `questionnaire_id`) — set to `null`
- `ComponentsJson` (string, column `components_json`, JSONB) — deserialize, remove matching entry, re-serialize

### ComponentsJson Structure

Based on `SyncStageMappingsInTransactionAsync` deserialization code:

```csharp
var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized, JsonOptions);
// component.Key == "checklist"        → component.ChecklistIds: List<long>
// component.Key == "questionnaires"   → component.QuestionnaireIds: List<long>
```

`StageComponent` has a `Key` discriminator and typed ID lists. To remove a deleted checklist entry:
1. Deserialize `ComponentsJson` to `List<StageComponent>`
2. For each component where `Key == "checklist"`, remove the deleted ID from `ChecklistIds`
3. Optionally remove the component entry entirely if `ChecklistIds` becomes empty
4. Re-serialize with `System.Text.Json` (not Newtonsoft — project convention for new code)

`TryUnwrapComponentsJson` is a private helper in `ComponentMappingService` that normalizes the JSON before deserialization — the cascade delete code will need to replicate this or call `SyncStageMappingsInTransactionAsync` which handles it internally.

### Transaction Pattern

The established pattern from `StageService.UpdateAsync`:

```csharp
var result = await _db.Ado.UseTranAsync(async () =>
{
    // 1. Soft-delete the component
    // 2. Load affected stages via mapping service
    // 3. For each stage: null the FK, patch ComponentsJson, UpdateAsync within transaction
    // 4. SyncStageMappingsInTransactionAsync(stageId, _db) — clears + rebuilds mapping rows
    return true;
});
if (!result.IsSuccess)
    throw new CRMException(ErrorCodeEnum.SystemError, result.ErrorMessage ?? "Transaction failed");

// 5. Cache invalidation OUTSIDE transaction (per D-05)
foreach (var workflowId in affectedWorkflowIds)
    await _cacheService.RemoveAsync($"ow:stage:workflow:{workflowId}");
```

Cache key confirmed: `$"{STAGE_CACHE_PREFIX}:workflow:{workflowId}"` where `STAGE_CACHE_PREFIX = "ow:stage"` — full key is `ow:stage:workflow:{workflowId}`.

**Important:** `_db` is `ISqlSugarClient`. Both `ChecklistService` and `QuestionnaireService` currently do NOT inject `ISqlSugarClient` — they will need it added to their constructors, OR the cascade logic can be extracted to a new private method in each service and `_db` injected. `IStageRepository` is already injected in both services.

### ChecklistService Dependencies

Currently injected (confirmed from constructor):
- `IChecklistRepository`, `IChecklistTaskRepository`, `IMapper`, `IStageRepository`, `UserContext`
- `IOperatorContextService`, `IComponentMappingService`, `IBackgroundTaskQueue`
- `IOperationChangeLogService`, `ILogger<ChecklistService>`, `IUserService`
- `IActionTriggerMappingRepository`, `IActionDefinitionRepository`

**Missing for COMP-01:** `ISqlSugarClient` (needed for `UseTranAsync` + `SyncStageMappingsInTransactionAsync`), `IDistributedCacheService` (needed for cache invalidation per D-05).

### QuestionnaireService Dependencies

Currently injected (confirmed from pattern; full constructor not shown but `IComponentMappingService` confirmed injected — `NotifyQuestionnaireNameChangeAsync` is called).

**Missing for COMP-02:** Same as COMP-01 — `ISqlSugarClient`, `IDistributedCacheService`.

---

## COMP-03: Workflow Duplicate Deep Copy

### Current DuplicateAsync Stage Copy Block (lines 888–910)

Fields currently copied:
```csharp
WorkflowId, Name, PortalName, InternalName, Description,
DefaultAssignedGroup, DefaultAssignee, EstimatedDuration,
Order, ChecklistId, QuestionnaireId, Color, IsActive
```

Fields confirmed MISSING (verified against `Stage.cs`):
```csharp
ComponentsJson      // JSONB stage components config
ViewPermissionMode  // enum ViewPermissionModeEnum
ViewTeams           // JSONB string
OperateTeams        // JSONB string
PortalPermission    // enum PortalPermissionEnum?
VisibleInPortal     // bool
UseSameTeamForOperate // bool (likely also missing)
AttachmentManagementNeeded // bool (likely also missing)
Required            // bool (likely also missing)
```

D-08 explicitly lists the 6 fields to add: `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, `PortalPermission`, `VisibleInPortal`. The plan should also evaluate `UseSameTeamForOperate`, `AttachmentManagementNeeded`, and `Required` for completeness since they are currently absent too.

### SyncStageMappingsAsync Call

After `await _stageRepository.InsertAsync(duplicatedStage)`, add:

```csharp
await _componentMappingService.SyncStageMappingsAsync(duplicatedStage.Id);
```

`_componentMappingService` is already injected in `WorkflowService` as `IComponentMappingService`. This is the non-transactional variant (D-09) — appropriate here since the stage insert already completed.

**Note:** `SyncStageMappingsAsync` does a full delete+rebuild of mapping rows for the stage, reading from `ComponentsJson`. Since `ComponentsJson` is now being copied, this will correctly register the new stage's component associations.

---

## LOG-05: UpdatedBy Propagation

### New Method: TouchWorkflowAuditAsync

Add to `IWorkflowRepository`:

```csharp
/// <summary>
/// Update only ModifyBy and ModifyDate on a workflow — lightweight audit touch.
/// Does NOT trigger change logging or IDM validation.
/// </summary>
Task<bool> TouchWorkflowAuditAsync(long workflowId, string modifyBy);
```

Implement in `WorkflowRepository` using the `SetColumns` pattern established in `SetDefaultWorkflowAsync`:

```csharp
public async Task<bool> TouchWorkflowAuditAsync(long workflowId, string modifyBy)
{
    var result = await db.Updateable<Workflow>()
        .SetColumns(x => x.ModifyBy == modifyBy)
        .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
        .Where(x => x.Id == workflowId && x.IsValid == true)
        .ExecuteCommandAsync();
    return result > 0;
}
```

This targets only two columns — no risk of overwriting other fields, no AutoMapper, no full entity load.

### Call Sites

Per D-14, add to three `UpdateAsync` methods. Pattern per D-15:

```csharp
// After the main save succeeds:
try
{
    await _workflowRepository.TouchWorkflowAuditAsync(workflowId, GetCurrentUserName());
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to touch workflow audit for workflow {WorkflowId}", workflowId);
    // Do not rethrow — Stage/Component save already succeeded
}
```

**StageService.UpdateAsync** — `workflowId` is `stageInTransaction.WorkflowId`, already available. `_workflowRepository` is already injected. Call site: after `transactionResult.Data` is `true`, outside the transaction.

**ChecklistService.UpdateAsync** — needs `workflowId`. The checklist entity does NOT have a `WorkflowId` field directly. To get the workflow ID: query the mapping table for stages using this checklist, then take the stage's `WorkflowId`. Alternatively, `_stageRepository` is already injected — query `GetListAsync(s => s.ChecklistId == id)` to get affected stages. If multiple stages/workflows are affected, touch all of them. `IWorkflowRepository` is NOT currently injected in `ChecklistService` — needs to be added.

**QuestionnaireService.UpdateAsync** — same challenge as ChecklistService. `IWorkflowRepository` needs injection. Use `_mappingService.GetStagesUsingQuestionnaireFastAsync(id)` to find stage IDs, then look up their `WorkflowId` values.

### Getting the Workflow ID from Component Services

The cleanest approach for ChecklistService and QuestionnaireService:

```csharp
// Get stage IDs that use this component (from mapping table)
var stageIds = await _mappingService.GetStagesUsingChecklistFastAsync(id);
if (stageIds.Any())
{
    var stages = await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id));
    var workflowIds = stages.Select(s => s.WorkflowId).Distinct();
    foreach (var wfId in workflowIds)
    {
        try { await _workflowRepository.TouchWorkflowAuditAsync(wfId, operatorName); }
        catch (Exception ex) { _logger.LogWarning(ex, "..."); }
    }
}
```

This avoids extra DB round-trips beyond what's already happening.

---

## Implementation Risks

### Risk 1: Missing ISqlSugarClient in ChecklistService / QuestionnaireService
**Impact:** Cannot call `UseTranAsync` or `SyncStageMappingsInTransactionAsync` without it.
**Mitigation:** Add to constructor. No functional impact — standard DI injection, same pattern as StageService and WorkflowService.

### Risk 2: QuestionnaireService uses Hard Delete, not Soft Delete
**Impact:** The cascade cleanup must fire before or as part of `DeleteAsync`. The current flow calls `_questionnaireRepository.DeleteAsync(entity)` — a hard delete. The transaction must execute cleanup before this line, or the transaction must wrap both.
**Mitigation:** In the transaction: (1) clean Stage references first, (2) then hard-delete the questionnaire. The mapping table rows for `QuestionnaireStageMapping` will be cleaned by `SyncStageMappingsInTransactionAsync` (it deletes all mappings for the stage, then rebuilds from the updated ComponentsJson — which no longer has the questionnaire ID).

### Risk 3: Cache Invalidation Outside Transaction
**Impact:** D-05 says invalidate cache after mutation. SqlSugar's `UseTranAsync` runs everything inside — cache calls using `IDistributedCacheService` should run outside to avoid holding connections open during cache I/O.
**Mitigation:** Collect affected `workflowId` values inside the transaction result, then invalidate after `result.IsSuccess`. This is already the established pattern in `StageService`.

### Risk 4: ComponentsJson Double-Escaping
**Impact:** The `NormalizeJsonStringField` pattern exists in WorkflowService to prevent multiple JSON-escaping of JSONB string columns. If ComponentsJson is set directly from deserialized+reserialized content, it could be double-escaped.
**Mitigation:** After re-serializing, assign the raw JSON string directly to `stage.ComponentsJson`. Do not JSON-encode a string that is already JSON. Review `TryUnwrapComponentsJson` logic in `ComponentMappingService` — the same unwrapping should be applied before deserialization in cascade delete code.

### Risk 5: SyncStageMappingsAsync in DuplicateAsync Not Transactional
**Impact:** If `SyncStageMappingsAsync` fails after the stage insert, mapping tables are stale for the new stage.
**Mitigation:** Per D-09, the non-transactional `SyncStageMappingsAsync` is the chosen variant. Failure is not data-loss critical — mapping tables can be re-synced. Log the error, don't rethrow (consistent with existing pattern elsewhere in DuplicateAsync's logging block).

### Risk 6: WorkflowId Resolution in ChecklistService.UpdateAsync
**Impact:** A checklist might be used by multiple stages across multiple workflows. All parent workflows should have their audit touched.
**Mitigation:** Use `GetStagesUsingChecklistFastAsync` + lookup stage WorkflowIds. Touch all affected workflows. Each touch is a targeted single-column update — multiple calls are cheap.

---

## Recommended Plan Structure

**Wave 0 (no dependencies):**
- Task 1 — COMP-03: Extend DuplicateAsync stage copy block + call SyncStageMappingsAsync

**Wave 1 (depends on nothing, additive):**
- Task 2 — LOG-05: Add TouchWorkflowAuditAsync to IWorkflowRepository + WorkflowRepository
- Task 3 — LOG-05: Wire TouchWorkflowAuditAsync into StageService.UpdateAsync
- Task 4 — LOG-05: Wire TouchWorkflowAuditAsync into ChecklistService.UpdateAsync (inject IWorkflowRepository)
- Task 5 — LOG-05: Wire TouchWorkflowAuditAsync into QuestionnaireService.UpdateAsync (inject IWorkflowRepository)

**Wave 2 (highest risk, do last):**
- Task 6 — COMP-01: Cascade delete in ChecklistService.DeleteAsync (inject ISqlSugarClient + IDistributedCacheService)
- Task 7 — COMP-02: Cascade delete in QuestionnaireService.DeleteAsync (inject ISqlSugarClient + IDistributedCacheService)

---

## Code Patterns Reference

### Established: Column-targeted update (WorkflowRepository.SetDefaultWorkflowAsync)
```csharp
await db.Updateable<Workflow>()
    .SetColumns(x => x.IsDefault == false)
    .Where(x => x.IsValid == true && x.Id != workflowId)
    .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode)
    .ExecuteCommandAsync();
```

### Established: Transaction pattern (StageService.UpdateAsync)
```csharp
var result = await _db.Ado.UseTranAsync(async () =>
{
    // operations...
    return value;
});
if (!result.IsSuccess)
    throw new CRMException(ErrorCodeEnum.SystemError, result.ErrorMessage ?? "Transaction failed");
```

### Established: Cache invalidation (StageService)
```csharp
// STAGE_CACHE_PREFIX = "ow:stage"
var cacheKey = $"{STAGE_CACHE_PREFIX}:workflow:{workflowId}";
await _cacheService.RemoveAsync(cacheKey);
```

### Established: Audit fields (WorkflowService.ProcessExpiredWorkflowsAsync)
```csharp
workflow.ModifyDate = DateTimeOffset.UtcNow;
workflow.ModifyBy = GetCurrentUserName();  // _operatorContextService.GetOperatorDisplayName()
```

### Established: Transaction-aware mapping sync (ComponentMappingService)
```csharp
// Signature: Task SyncStageMappingsInTransactionAsync(long stageId, ISqlSugarClient transaction)
// Deletes all ChecklistStageMapping + QuestionnaireStageMapping for the stage,
// then rebuilds from stage.ComponentsJson (reads within the transaction client)
await _mappingService.SyncStageMappingsInTransactionAsync(stageId, _db);
```

### New pattern needed: ComponentsJson patch
```csharp
// System.Text.Json — project convention for new code
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
};

var components = JsonSerializer.Deserialize<List<StageComponent>>(
    stage.ComponentsJson ?? "[]", jsonOptions) ?? new List<StageComponent>();

foreach (var comp in components.Where(c => c.Key == "checklist"))
    comp.ChecklistIds?.Remove(deletedChecklistId);

// Remove empty component entries to keep ComponentsJson clean
components.RemoveAll(c => c.Key == "checklist" && (c.ChecklistIds == null || !c.ChecklistIds.Any()));

stage.ComponentsJson = JsonSerializer.Serialize(components, jsonOptions);
stage.ChecklistId = null;
```

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `UseSameTeamForOperate`, `AttachmentManagementNeeded`, `Required` are also missing from DuplicateAsync copy block | COMP-03 | Minor: those fields silently reset to defaults on copy. Not in D-08 scope, but worth including |
| A2 | QuestionnaireService has `IComponentMappingService` injected (inferred from `NotifyQuestionnaireNameChangeAsync` call) | COMP-02 | Low: if not injected, add to constructor — same pattern |
| A3 | `StageComponent.ChecklistIds` is `List<long>` (mutable) rather than `IReadOnlyList<long>` | COMP-01/COMP-02 | If immutable: must reconstruct the component object instead of mutating in place |

---

## Open Questions

1. **Should COMP-01/COMP-02 cascade to `Published` questionnaires?**
   - What we know: QuestionnaireService.DeleteAsync blocks deletion of `Published` questionnaires with a `CRMException`
   - What's unclear: The cascade delete code never executes if deletion is blocked — so `Published` questionnaires can never leave a stale Stage reference via this path. No action needed.
   - Recommendation: Confirm this is acceptable. If a Published questionnaire must also be de-listable, that's a separate requirement.

2. **Should `CoAssignees` be added to the DuplicateAsync copy block?**
   - What we know: `CoAssignees` is a JSONB field on Stage, currently not copied in DuplicateAsync
   - What's unclear: Not mentioned in D-08, but logically should be copied if DefaultAssignee is copied
   - Recommendation: Include in the task definition — low risk, zero cost to add alongside the other fields

## Sources

All findings are from direct codebase inspection (no external sources needed for this phase):

- `packages/flowFlex-backend/Application/Services/OW/ChecklistService.cs` — DeleteAsync, UpdateAsync
- `packages/flowFlex-backend/Application/Services/OW/QuestionnaireService.cs` — DeleteAsync, UpdateAsync
- `packages/flowFlex-backend/Application/Services/OW/StageService.cs` — UpdateAsync, SyncStageMappingsInTransactionAsync call site, cache key pattern
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` — DuplicateAsync, AuditHelper patterns
- `packages/flowFlex-backend/Application/Services/OW/ComponentMappingService.cs` — SyncStageMappingsAsync, SyncStageMappingsInTransactionAsync implementation
- `packages/flowFlex-backend/Application.Contracts/IServices/OW/IComponentMappingService.cs` — full interface contract
- `packages/flowFlex-backend/Domain/Entities/OW/Stage.cs` — all fields
- `packages/flowFlex-backend/Domain/Repository/OW/IWorkflowRepository.cs` — existing methods
- `packages/flowFlex-backend/SqlSugarDB/Repositories/OW/WorkflowRepository.cs` — SetColumns pattern, GetCurrentTenantId helper

**Confidence: HIGH** — all claims verified from source files, no assumed knowledge.
