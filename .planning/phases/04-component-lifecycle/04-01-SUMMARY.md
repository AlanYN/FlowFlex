---
phase: "04-component-lifecycle"
plan: 01
subsystem: "OW / Workflow / Stage"
tags: [duplicate, audit-touch, stage-copy, component-mapping]
dependency_graph:
  requires: []
  provides: [COMP-03, LOG-05-partial]
  affects: [WorkflowService, StageService, WorkflowRepository]
tech_stack:
  added: []
  patterns: [SetColumns targeted-update, try/catch fire-and-forget, SyncStageMappingsAsync post-insert]
key_files:
  created: []
  modified:
    - packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs
    - packages/flowFlex-backend/Domain/Repository/OW/IWorkflowRepository.cs
    - packages/flowFlex-backend/SqlSugarDB/Repositories/OW/WorkflowRepository.cs
    - packages/flowFlex-backend/Application/Services/OW/StageService.cs
decisions:
  - "modifyBy in TouchWorkflowAuditAsync sourced from _userContext (FirstName+LastName > UserName > Email > SYSTEM), matching InitUpdateInfo pattern — StageService has no _operatorContextService"
  - "SyncStageMappingsAsync used (non-transactional) in DuplicateAsync per D-09; SyncStageMappingsInTransactionAsync is reserved for transactional UpdateAsync paths"
  - "TouchWorkflowAuditAsync omits explicit tenant/appCode Where clauses — consistent with SetDefaultWorkflowAsync; SqlSugar global filter handles tenant isolation for Updateable operations"
metrics:
  duration: "~8 minutes"
  completed: "2026-06-04"
  tasks_completed: 3
  files_modified: 4
---

# Phase 04 Plan 01: Workflow Duplicate Deep Copy + TouchWorkflowAuditAsync Summary

**One-liner:** Extended DuplicateAsync to copy 10 missing Stage fields + component mappings, and added a lightweight SetColumns audit-touch on the parent Workflow when a Stage is saved.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Extend DuplicateAsync stage copy block (COMP-03) | 5a47928d | WorkflowService.cs |
| 2 | Add TouchWorkflowAuditAsync to IWorkflowRepository + WorkflowRepository (LOG-05) | 5a47928d | IWorkflowRepository.cs, WorkflowRepository.cs |
| 3 | Wire TouchWorkflowAuditAsync into StageService.UpdateAsync (LOG-05) | 5a47928d | StageService.cs |

## What Was Done

### Task 1 — COMP-03: DuplicateAsync deep copy

The `new Stage { ... }` initializer in `WorkflowService.DuplicateAsync` previously copied only 12 fields, leaving 10 fields at their default values. Added the missing assignments immediately after `IsActive`:

- `ComponentsJson`, `ViewPermissionMode`, `ViewTeams`, `OperateTeams`, `PortalPermission`, `VisibleInPortal`, `UseSameTeamForOperate`, `AttachmentManagementNeeded`, `Required`, `CoAssignees`

After `_stageRepository.InsertAsync(duplicatedStage)`, added a try/catch call to `_componentMappingService.SyncStageMappingsAsync(duplicatedStage.Id)` so mapping tables are populated for each duplicated stage. Failure logs a warning and does not abort the duplicate operation.

### Task 2 — LOG-05 infrastructure: TouchWorkflowAuditAsync

Added interface declaration to `IWorkflowRepository`:

```csharp
Task<bool> TouchWorkflowAuditAsync(long workflowId, string modifyBy);
```

Implemented in `WorkflowRepository` using the SetColumns targeted-update pattern (consistent with `SetDefaultWorkflowAsync`). Only `ModifyBy` and `ModifyDate` columns are written — no full entity load, no AutoMapper, no change log call. Returns `true` when at least one row was updated.

### Task 3 — LOG-05 wire-up: StageService.UpdateAsync

After the logging try/catch block in `UpdateAsync` (outside the transaction), added a try/catch call to `_workflowRepository.TouchWorkflowAuditAsync(stage.WorkflowId, modifyBy)`. The `modifyBy` value is computed inline using the same priority logic as `InitUpdateInfo` (`FirstName + LastName > UserName > Email > "SYSTEM"`). On failure, only a warning is logged — no exception propagates and the Stage save is unaffected.

## Requirements Addressed

- **COMP-03**: Duplicating a workflow now produces stages with all fields copied, including ComponentsJson and all permission/team fields. Component mapping tables are populated via SyncStageMappingsAsync.
- **LOG-05** (partial): TouchWorkflowAuditAsync infrastructure is in place and wired into StageService.UpdateAsync. Remaining LOG-05 call sites (Plan 02) can reuse the same pattern.

## Deviations from Plan

None — plan executed exactly as written. One implementation detail resolved: StageService has no `_operatorContextService`, so the `modifyBy` string is computed inline from `_userContext` using the same logic as `InitUpdateInfo`. This is consistent with the service's existing audit pattern.

## Self-Check: PASSED

- WorkflowService.cs contains `ComponentsJson = stage.ComponentsJson` (line 903)
- WorkflowService.cs contains `SyncStageMappingsAsync(duplicatedStage.Id)` (line 923)
- IWorkflowRepository.cs declares `TouchWorkflowAuditAsync` (line 90)
- WorkflowRepository.cs implements `.SetColumns(x => x.ModifyBy == modifyBy)` (line 519)
- StageService.cs calls `TouchWorkflowAuditAsync` (line 629)
- Application/Application.csproj: 0 errors
- SqlSugarDB/SqlSugarDB.csproj: 0 errors, 0 warnings
- Commit 5a47928d exists: 4 files changed, 54 insertions(+), 1 deletion(-)
