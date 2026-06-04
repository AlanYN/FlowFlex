---
phase: "04-component-lifecycle"
plan: 02
subsystem: "backend/services/OW"
tags: [cascade-delete, checklist, questionnaire, transaction, cache-invalidation, audit]
dependency_graph:
  requires: ["04-01"]
  provides: ["COMP-01", "COMP-02", "LOG-05-checklist-update", "LOG-05-questionnaire-update"]
  affects: ["ChecklistService", "QuestionnaireService", "StageService cache", "WorkflowRepository audit"]
tech_stack:
  added: []
  patterns:
    - "UseTranAsync for atomic cascade delete + FK nulling + ComponentsJson patch + mapping sync"
    - "Pre-transaction reverse lookup via GetStagesUsingChecklistFastAsync / GetStagesUsingQuestionnaireFastAsync"
    - "Post-transaction cache invalidation via IDistributedCacheService.RemoveAsync"
    - "Fire-and-forget TouchWorkflowAuditAsync in try/catch after UpdateAsync"
key_files:
  modified:
    - packages/flowFlex-backend/Application/Services/OW/ChecklistService.cs
    - packages/flowFlex-backend/Application/Services/OW/QuestionnaireService.cs
decisions:
  - "Cascade cleanup runs BEFORE hard delete in QuestionnaireService.DeleteAsync to prevent mapping table orphans (Risk 2)"
  - "Stage reverse lookup performed outside transaction to avoid holding DB connection during query"
  - "Cache invalidation runs outside transaction per D-05 (invalidate after commit, not during)"
  - "TouchWorkflowAuditAsync wrapped in per-workflow try/catch so one failure does not abort the update"
  - "StageJsonOptions static field reused in QuestionnaireService for ComponentsJson deserialization (no new JsonSerializerOptions)"
metrics:
  duration: "~20 minutes"
  completed: "2026-06-04T07:19:28Z"
  tasks_completed: 2
  files_modified: 2
---

# Phase 04 Plan 02: Cascade Delete Cleanup for Checklist and Questionnaire Summary

Implemented atomic cascade delete cleanup for Checklist (COMP-01) and Questionnaire (COMP-02), and wired TouchWorkflowAuditAsync call sites for both UpdateAsync methods (LOG-05).

## What Was Done

### Task 1: ChecklistService — COMP-01 + LOG-05

**New constructor injections:**
- `ISqlSugarClient _db` — for `UseTranAsync`
- `IDistributedCacheService _cacheService` — for cache invalidation
- `IWorkflowRepository _workflowRepository` — for `TouchWorkflowAuditAsync`

**DeleteAsync rewrite:**
1. Pre-transaction: `GetStagesUsingChecklistFastAsync(id)` to find affected stage IDs; load stages to collect distinct `WorkflowId`s.
2. `_db.Ado.UseTranAsync`:
   - Soft-delete the checklist (`IsValid = false`, `ModifyDate = UtcNow`, `UpdateAsync`)
   - For each affected stage: `ChecklistId = null`, deserialize `ComponentsJson`, remove the deleted ID from `ChecklistIds`, remove empty checklist components, re-serialize, `UpdateAsync`, `SyncStageMappingsInTransactionAsync(stageId, _db)`
   - Return `affectedWorkflowIds` from lambda
3. On `!result.IsSuccess`: throw `CRMException(ErrorCodeEnum.SystemError, ...)`
4. Post-transaction: `_cacheService.RemoveAsync($"ow:stage:workflow:{workflowId}")` for each affected workflow
5. Background-queue log call unchanged

**UpdateAsync amendment (LOG-05):**
After save, calls `GetStagesUsingChecklistFastAsync` → load stages → collect distinct WorkflowIds → `TouchWorkflowAuditAsync` per workflow in individual try/catch. Does not rethrow.

### Task 2: QuestionnaireService — COMP-02 + LOG-05

**New constructor injections:** same three as Task 1.

**DeleteAsync rewrite (critical ordering: cleanup BEFORE hard delete):**
1. Pre-transaction: `GetStagesUsingQuestionnaireFastAsync(id)` + workflow ID collection
2. `_db.Ado.UseTranAsync`:
   - For each affected stage: `QuestionnaireId = null`, deserialize `ComponentsJson` using `StageJsonOptions`, remove deleted ID from `QuestionnaireIds`, remove empty questionnaire components, re-serialize using `StageJsonOptions`, `UpdateAsync`, `SyncStageMappingsInTransactionAsync`
   - **Then** `_questionnaireRepository.DeleteAsync(entity)` — hard delete is last
   - Return `affectedWorkflowIds`
3. On `!result.IsSuccess`: throw `CRMException(ErrorCodeEnum.SystemError, ...)`
4. Post-transaction: cache invalidation per workflow
5. Background-queue log call unchanged

**UpdateAsync amendment (LOG-05):** same pattern as ChecklistService.

## Requirements Addressed

| Requirement | Status |
|-------------|--------|
| COMP-01 | Done — ChecklistService.DeleteAsync atomic cascade with transaction |
| COMP-02 | Done — QuestionnaireService.DeleteAsync cascade before hard delete |
| LOG-05 | Done — TouchWorkflowAuditAsync in both UpdateAsync methods |

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

### Files exist:
- `packages/flowFlex-backend/Application/Services/OW/ChecklistService.cs` — modified
- `packages/flowFlex-backend/Application/Services/OW/QuestionnaireService.cs` — modified

### Commit exists:
- `54c749c7` — feat(04-02): cascade delete cleanup for Checklist and Questionnaire

### Build: PASSED (0 errors, dotnet build Application/Application.csproj)

## Self-Check: PASSED
