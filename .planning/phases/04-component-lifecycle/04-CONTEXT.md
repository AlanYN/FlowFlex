# Phase 4: Component Lifecycle & Propagation - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Deleted Checklist/Questionnaire components are automatically removed from Stage references, Workflow duplication produces a complete independent copy of Stage component config, and Stage/Component updates propagate UpdatedBy to the parent Workflow.

Requirements: COMP-01, COMP-02, COMP-03, LOG-05 (4 items)

</domain>

<decisions>
## Implementation Decisions

### Cascade Delete Cleanup (COMP-01, COMP-02)
- **D-01:** When a Checklist or Questionnaire is soft-deleted (IsValid=false), immediately clean all Stage references in the same transaction
- **D-02:** Cleanup must cover: Stage.ChecklistId/QuestionnaireId FK → set null, Stage.ComponentsJson → remove the deleted component entry, ChecklistStageMapping/QuestionnaireStageMapping → remove rows
- **D-03:** Use `SyncStageMappingsInTransactionAsync` (accepts transaction client) for mapping table sync — NOT the fire-and-forget version
- **D-04:** All operations wrapped in one SqlSugar transaction (soft-delete + JSONB update + mapping cleanup + cache invalidation)
- **D-05:** Invalidate stage cache key `ow:stage:workflow:{workflowId}` after mutation
- **D-06:** Use mapping tables (`ChecklistStageMapping`/`QuestionnaireStageMapping`) to find affected Stages — do NOT full-table scan ComponentsJson
- **D-07:** Stay tenant-scoped — never use `.Filter(null, true)` during cascade delete

### Workflow Duplicate Deep Copy (COMP-03)
- **D-08:** Extend `DuplicateAsync` stage copy block to include: `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, `PortalPermission`, `VisibleInPortal`
- **D-09:** Call `SyncStageMappingsAsync(newStageId)` per duplicated stage after copy
- **D-10:** Shared references are the intended behavior — new Workflow's Stages point to the same Checklist/Questionnaire entities as the original
- **D-11:** No user prompt or warning after Duplicate — shared references are accepted as-is
- **D-12:** Editing a Component in the copy WILL affect the original (accepted trade-off, not a bug)

### UpdatedBy Propagation (LOG-05)
- **D-13:** Add a lightweight `TouchWorkflowAuditAsync(workflowId)` repo method that only updates `ModifyBy`/`ModifyDate` columns — do NOT call `WorkflowService.UpdateAsync` (that triggers IDM validation and change logging)
- **D-14:** Call `TouchWorkflowAuditAsync` from: `StageService.UpdateAsync`, `ChecklistService.UpdateAsync`, `QuestionnaireService.UpdateAsync`
- **D-15:** Wrap in try/catch — on failure, log warning and continue (do NOT roll back the Stage/Component save)
- **D-16:** Only user-initiated saves propagate — background/system-triggered updates do not

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Backend - Checklist Delete
- `packages/flowFlex-backend/Application/Services/OW/ChecklistService.cs` — DeleteAsync method

### Backend - Questionnaire Delete
- `packages/flowFlex-backend/Application/Services/OW/QuestionnaireService.cs` — DeleteAsync method

### Backend - Stage Service
- `packages/flowFlex-backend/Application/Services/OW/StageService.cs` — ComponentsJson management, UpdateAsync, SyncStageMappingsInTransactionAsync

### Backend - Workflow Duplicate
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` — DuplicateAsync (lines ~885-906, confirmed missing fields)

### Backend - Workflow Repository
- `packages/flowFlex-backend/Domain/Repository/OW/IWorkflowRepository.cs` — Add TouchWorkflowAuditAsync here

### Backend - Component Mapping Service
- `Application/Services/OW/ComponentMappingService.cs` — SyncStageMappingsInTransactionAsync, SyncWorkflowMappingsAsync

### Backend - Stage Entity
- `packages/flowFlex-backend/Domain/Entities/OW/Stage.cs` — ComponentsJson, ChecklistId, QuestionnaireId fields

### Research
- `.planning/research/SUMMARY.md` — Critical risks and pitfalls for cascade delete and duplicate

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SyncStageMappingsInTransactionAsync` — accepts transaction client, handles mapping table sync
- `ComponentMappingService` — existing service for mapping table CRUD
- SqlSugar `BeginTranAsync()` / `CommitTranAsync()` / `RollbackTranAsync()` transaction pattern

### Established Patterns
- Soft delete via `IsValid = false` with SqlSugar global filter
- `ComponentsJson` is `System.Text.Json` serialized JSONB — deserialize → modify → re-serialize
- Cache invalidation pattern: `_cache.RemoveAsync($"ow:stage:workflow:{workflowId}")`
- `WhereIF` for conditional query filters

### Integration Points
- `ChecklistService.DeleteAsync` → trigger cascade cleanup
- `QuestionnaireService.DeleteAsync` → trigger cascade cleanup
- `WorkflowService.DuplicateAsync` → extend stage copy block
- `StageService.UpdateAsync` → call TouchWorkflowAuditAsync
- `ChecklistService.UpdateAsync` → call TouchWorkflowAuditAsync
- `QuestionnaireService.UpdateAsync` → call TouchWorkflowAuditAsync

</code_context>

<specifics>
## Specific Ideas

- Research confirmed `DuplicateAsync` lines 885-906 copy stages but skip 8 fields including ComponentsJson
- `OperationTypeEnum.StageSave = 47` kept in enum (we only removed the call site in Phase 2)
- `ChecklistStageMapping` / `QuestionnaireStageMapping` are the fastest reverse lookup path
- Research identified `GetComponentsAsync` line 1571 in StageService already has a name-count mismatch detection — this is a live symptom of the stale ComponentsJson problem

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 4-Component Lifecycle & Propagation*
*Context gathered: 2026-06-04*
