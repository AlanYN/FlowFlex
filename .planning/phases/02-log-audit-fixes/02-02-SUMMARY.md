---
phase: "02"
plan: "02"
subsystem: "OW / Workflow / ChecklistTask"
tags: [bug-fix, notes-count, workflow-status, data-integrity]
requirements: [LOG-04, DATA-03]
key-files:
  modified:
    - packages/flowFlex-backend/Domain/Repository/OW/IChecklistTaskNoteRepository.cs
    - packages/flowFlex-backend/SqlSugarDB/Repositories/OW/ChecklistTaskNoteRepository.cs
    - packages/flowFlex-backend/Application/Services/OW/ChecklistTaskService.cs
    - packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs
decisions:
  - "Filter notes count by NoteType='General' to exclude system change-log entries from displayed note counts"
  - "Sync IsActive from Status in WorkflowService.UpdateAsync using OrdinalIgnoreCase comparison"
metrics:
  duration: "~5 minutes"
  completed: "2026-06-03"
  tasks_completed: 2
  files_modified: 4
---

# Phase 02 Plan 02: Log Audit Fixes (Tasks 2 & 3) Summary

Filter checklist task note counts to user-created notes only (NoteType="General"), and sync Workflow.IsActive from Status on every UpdateAsync call.

## DB Investigation Results (Task 1 — completed prior)

- Table: `ff_checklist_task_note`
- NoteType discriminator values confirmed in staging data:
  - `"General"` — 27 rows, user-created notes
  - `"System"` — 22 rows, system change-log entries
- IsActive/Status sync gap: not observed in current staging data (all rows consistent), but defensive fix applied in UpdateAsync

## What Was Done

### Task 2 — Filter Notes Count by NoteType (LOG-04)

The `CountByTaskIdAsync` query was returning counts that included `NoteType="System"` entries (auto-generated change-log records), inflating the displayed note count shown to users.

Three-file change:

1. `IChecklistTaskNoteRepository.cs` — added optional `string noteType = null` parameter to `CountByTaskIdAsync`
2. `ChecklistTaskNoteRepository.cs` — added `.WhereIF(!string.IsNullOrEmpty(noteType), (ctn, ob) => ctn.NoteType == noteType)` to the query; existing `!ctn.IsDeleted` and `ob.IsValid` filters preserved
3. `ChecklistTaskService.cs` — `GetNotesCountByTaskIdsAsync` now passes `"General"` so only user-created notes are counted

### Task 3 — Sync IsActive from Status in WorkflowService.UpdateAsync (DATA-03)

After `_mapper.Map(input, entity)` and the `entity.IsAIGenerated = originalIsAIGenerated` guard, added:

```csharp
entity.IsActive = string.Equals(entity.Status, "active", StringComparison.OrdinalIgnoreCase);
```

This keeps `IsActive` in sync whenever a workflow is updated via the standard update path. `ActivateAsync` and `DeactivateAsync` were not touched.

## Verification

- `dotnet build` — 0 errors, 4 warnings (pre-existing AutoMapper CVE advisory, unrelated)
- Build output: `已成功生成` (succeeded)

## Commit

- `58cf0ad8` — fix(02-02): filter notes count by NoteType, sync Workflow IsActive from Status

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Modified files staged and committed: confirmed (4 files, 7 insertions, 3 deletions)
- Commit hash 58cf0ad8 present in git log: confirmed
- Build passes: 0 errors confirmed
