# Feature Landscape — OW-621 Workflow Component Enhancements

**Domain:** Workflow/BPM system (onboarding case management)
**Researched:** 2026-06-02
**Milestone:** v1.1 — 12 targeted fixes and enhancements

---

## Table Stakes

Features users expect in any workflow/BPM system. Missing = product feels broken or untrustworthy.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Human-readable audit log text | Users read logs to understand what happened; raw enum strings like "ChecklistTaskUncomplete" feel unfinished | Low | Backend: update `BuildTaskCompletionDescription` and `BuildTaskUncompletionDescription` in `ChecklistLogService` to produce "Completed/Cancelled the task '{name}'" |
| Timestamps to the second in audit logs | Multiple events can happen in the same minute; minute-precision timestamps make ordering ambiguous | Low | Frontend: `projectTenMinutesSsecondsDate` format is already used in `historyTable.vue` and `InternalNotes.vue` — apply same format to checklist Done/Cancel log display |
| Scroll-to-top on multi-page form navigation | Users expect to read each new page from the top; staying mid-scroll is a jarring UX failure | Low | Frontend only: call `window.scrollTo(0,0)` or reset the `el-scrollbar` ref in the questionnaire Next button handler |
| Status consistency across views | Selecting a Workflow in Create Case must show the same status label and tag as the Workflow management list | Low | Frontend: align the status display component/format used in the Case creation workflow selector with the Workflow list view |
| Component orphan cleanup on delete | Deleting a Checklist or Questionnaire still referenced by a Stage leaves the Stage pointing at a ghost component | Medium | Backend: on Component delete, remove the corresponding Stage-component link or call `_componentMappingService.SyncWorkflowMappingsAsync` for all affected workflows |
| Uploader identity on file records | In any document context, users expect to see who uploaded a file and when — "uploaded by System" is not acceptable | Low | Backend: write `UploadedBy` (operator name) into the questionnaire answer JSONB alongside existing `UploadTime`. Frontend: render name + date in the file list row |
| Accurate comment/note count badge | A badge that inflates with system change-log entries misleads users about real human activity | Low | Frontend: filter the count computation to only include user-created Notes records, excluding ChangeLog-typed entries |

---

## Differentiators

Features beyond baseline expectations that improve quality of life in this system.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Workflow deep copy on duplicate | A fully independent clone — no surprise shared-state mutations when editing the copy's stages | Medium | Backend: `WorkflowService.DuplicateAsync` currently copies Stages but does NOT copy component mappings. After each duplicated stage is inserted, call the component mapping copy. Needs product confirmation: copy the Stage-to-Component reference only (recommended), or also clone the Component definition itself |
| UpdatedBy propagation from child to parent | When a Stage or Component is edited, the parent Workflow's `ModifyBy`/`ModifyDate` updates too — admins can trust the "last modified" field on the Workflow list | Medium | Backend: in `StageService.UpdateAsync` and component update paths, additionally update the parent Workflow's audit fields. Wide surface area — all update paths must be covered |
| Collapsible Stage detail sections | Detail pages with many components become unwieldy; collapse/expand lets users focus on what matters | Low | Frontend: pattern already exists in `InternalNotes.vue` using `el-collapse-transition` + `isOpen` toggle. Apply same pattern to Stage detail area |
| StageSave log suppression | Auto-save events produce log noise that drowns out meaningful user actions | Low | Backend: skip inserting `OperationTypeEnum.StageSave` (value = 47) in the log writer. Existing records are unaffected |
| Partial grid validation (fill-one-cell rule) | Strict all-cells-required for a Short Answer Grid is too rigid; real forms often accept partial grid responses | Medium | Backend: change required validation in `QuestionnaireAnswerService` — for Grid questions marked required, pass if at least one cell has a non-empty value. Frontend: update error state to row-level rather than grid-level indicator |

---

## Anti-Features

Things to explicitly NOT build in this scope.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Deep recursive component clone on Workflow duplicate (cloning Questionnaire/Checklist content) | OW-621 asks for Stage component references to be copied, not the component definitions. Cloning component content is a separate feature with its own migration and naming complexity | Copy the Stage-to-Component mapping link only. Leave component definitions shared unless product explicitly confirms full clone |
| Retroactive deletion of existing StageSave log records | Destructive data operation, outside milestone scope, irreversible | Only suppress future StageSave writes; leave existing records as-is |
| Real-time IdentityHub UserGroup sync listener | Infrastructure scope, not a bug fix | Fix the permission check logic to correctly read existing IDM data on each request |
| Min/max numeric validation on grid answers | Explicitly excluded in v1.0 per PROJECT.md | Not in this milestone |
| Cross-tenant team deduplication | Blocked on IDM team (external bug), explicitly out of scope per PROJECT.md | |

---

## Feature Dependencies

```
#8 Remove StageSave log
    (no dependencies — safe to do first, cleans noise for #1)

#1 Log text + timestamp
    (no dependencies beyond #8 being done first for visibility)

#4 Scroll to top on Next
    (no dependencies — pure frontend, one line)

#10 Status consistency
    (no dependencies — frontend alignment)

#7 File upload UploadedBy
    (no dependencies — backend JSONB write + frontend display)

#12 Comment count = Notes only
    (no dependencies — frontend filter on count query or computed)

#5 Case status tag position + collapsible Stage detail
    (no dependencies — frontend layout; collapse pattern from InternalNotes.vue)

#2 Deleted component orphan cleanup
    (depends on understanding _componentMappingService.SyncWorkflowMappingsAsync behavior)

#3 Workflow duplicate deep copy
    (depends on product confirming scope: reference-copy vs full component clone)
    (depends on #2 being stable so mapping sync is trustworthy)

#6 UpdatedBy propagation
    (depends on identifying all Stage + Component update entry points)
    (wide surface area: StageService, ChecklistService, QuestionnaireService update paths)

#9 Grid partial validation
    (depends on QuestionnaireAnswerService validation chain)
    (frontend error indicator update depends on backend rule being finalized)

#11 UserGroup permission fix
    (depends on full chain trace: PermissionService → CasePermissionService →
     StagePermissionService → IdentityHub IAM → UserGroup config)
    (highest risk — investigate before any code change)
```

---

## Implementation Order (Recommended)

Ordered by risk ascending and dependency satisfaction:

1. **#8** StageSave log removal — zero risk, no deps, unblocks log readability work
2. **#1** Log text + timestamp — low risk, backend string constants only
3. **#4** Scroll to top on Next — one-line frontend fix
4. **#10** Workflow status consistency — frontend display alignment
5. **#7** File upload UploadedBy — small JSONB addition + frontend column
6. **#12** Comment count = Notes only — frontend filter
7. **#5** Status tag position + collapsible Stage detail — frontend layout only
8. **#2** Deleted component orphan cleanup — backend service change, needs integration test
9. **#3** Workflow duplicate deep copy — confirm product scope first, then implement
10. **#6** UpdatedBy propagation — cross-cutting backend change, test all update paths
11. **#9** Grid partial validation — backend + frontend, needs edge case test coverage
12. **#11** UserGroup permission — highest risk, requires full chain investigation before coding

---

## Complexity Matrix

| # | Feature | Backend | Frontend | Risk |
|---|---------|---------|---------|------|
| 1 | Log text + timestamp | Low — two description builder methods | Low — format already used elsewhere | Low |
| 2 | Component orphan cleanup | Medium — hook into delete, call mapping sync | None | Medium — must not break valid stage-component links |
| 3 | Workflow deep copy | Medium — extend DuplicateAsync mapping loop | None | Medium — product scope unclear until confirmed |
| 4 | Scroll to top on Next | None | Low — one scroll call in Next handler | Low |
| 5 | Status tag position + collapsible | None | Low — CSS reposition + el-collapse-transition | Low |
| 6 | UpdatedBy propagation | Medium — all Stage/Component update paths | None | Medium — wide surface area |
| 7 | File upload metadata | Low — add UploadedBy to JSONB write | Low — add display column | Low |
| 8 | Remove StageSave log | Low — skip insert for StageSave type | None | Low |
| 9 | Grid partial validation | Medium — change required rule in answer validator | Low — update error indicator | Medium — mixed required/optional row edge cases |
| 10 | Status consistency | None | Low — align status display source | Low |
| 11 | UserGroup permission | High — full chain investigation required | Low — may need guard update | High |
| 12 | Comment count = Notes only | None | Low — filter in count computation | Low |

---

## Sources

- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` — DuplicateAsync confirmed: stages copied, component mappings not copied
- `packages/flowFlex-backend/Domain.Shared/Enums/OW/OperationTypeEnum.cs` — StageSave = 47 confirmed
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/ChecklistLogService.cs` — log description builder pattern, current text strings
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/InternalNotes.vue` — existing el-collapse-transition pattern
- `packages/flowFlex-common/src/app/components/changeHistory/historyTable.vue` — datetime format usage, log rendering
- `.planning/PROJECT.md` — requirements list, constraints, key decisions, out-of-scope items
