# Roadmap: FlowFlex Workflow Component Enhancements

## Overview

Comprehensive enhancements to the FlowFlex workflow system covering audit log accuracy, component lifecycle management, frontend UX improvements, data integrity, and permission fixes across 15 requirements in milestone v1.1 (OW-621).

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Number Type Support** - Register Number type in editor, render numeric input, validate on backend (completed 2026-05-25)
- [ ] **Phase 2: Log & Audit Fixes** - Correct Checklist log messages, timestamp precision, remove StageSave noise, fix comment counts, and sync Workflow status display
- [ ] **Phase 3: Frontend UX & Data** - Case page layout adjustments, Stage collapse, file upload metadata display, Short Answer Grid validation fix
- [ ] **Phase 4: Component Lifecycle & Propagation** - Delete Checklist/Questionnaire cleans Stage refs, Duplicate deep-copies components, Stage/Component updates propagate Workflow UpdatedBy
- [ ] **Phase 5: Permission Fix** - Investigate and fix User Group permission chain so configured users can edit Cases

## Phase Details

### Phase 1: Number Type Support
**Goal**: Users can create Number-type questions and submit numeric answers with proper validation
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: FREG-01, FREG-02, FREG-03, BVAL-01, BVAL-02, BVAL-03
**Success Criteria** (what must be TRUE):
  1. User can select "Number" from the question type dropdown in the questionnaire editor
  2. Number-type questions render as `el-input-number` in the form-filling view (not a text input)
  3. Clearing a number field results in null (not 0 or undefined errors)
  4. Submitting a non-numeric value for a Number field is rejected by the backend with a validation error
  5. Condition rules on Number fields compare numerically (e.g., 9 < 10, not "9" > "10")
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md — Frontend type registration and rendering (createQuestion.vue + dynamicForm.vue)
- [x] 01-02-PLAN.md — Backend validation and parser (QuestionnaireAnswerService.cs + QuestionnaireAnswerParser.cs)

---

### Phase 2: Log & Audit Fixes
**Goal**: Audit logs are accurate and clean — correct action text, second-level timestamps, no StageSave noise, correct comment counts, and Workflow status consistent with management page
**Depends on**: Phase 1
**Requirements**: LOG-01, LOG-02, LOG-03, LOG-04, UX-01, DATA-03
**Success Criteria** (what must be TRUE):
  1. Completing a Checklist task shows "Completed the task" with timestamp in MM/DD/YYYY HH:mm:ss format
  2. Cancelling a Checklist task shows "Cancelled the task" with timestamp in MM/DD/YYYY HH:mm:ss format
  3. No new StageSave entries appear in Change Log after saving a Stage
  4. Checklist task comment count reflects only Notes — Change Log entries are excluded from the count
  5. Clicking Next on a questionnaire scrolls the page to the top before rendering the next question set
  6. The Workflow dropdown when creating a Case shows Active/Inactive status matching the Workflow management page
**Plans**: 2 plans

Plans:
- [ ] 02-01-PLAN.md — Log text fixes + StageSave removal + questionnaire scroll (ChecklistLogService.cs, OnboardingStageProgressService.cs, dynamicForm.vue)
- [ ] 02-02-PLAN.md — Notes count filter + Workflow status sync (ChecklistTaskNoteRepository.cs, WorkflowService.cs)

---

### Phase 3: Frontend UX & Data
**Goal**: Case page layout is cleaner, Stage Detail is collapsible, file uploads display uploader metadata, and Short Answer Grid validation is more lenient
**Depends on**: Phase 2
**Requirements**: UX-02, UX-03, DATA-01, DATA-02
**Success Criteria** (what must be TRUE):
  1. Case page shows the status Tag directly to the right of the Case Name, reducing header height
  2. User can collapse Stage Detail to show only the stage title, and expand it again
  3. After uploading a file, the uploader name and upload date are displayed to the right of the filename
  4. A Short Answer Grid question marked required can be submitted after filling in any single cell (not all cells)
**Plans**: 2 plans

Plans:
- [ ] 03-01-PLAN.md — Case status tag inline + Stage collapse + Grid validation (detail.vue, EditableStageHeader.vue, dynamicForm.vue, QuestionnaireDetails.vue)
- [ ] 03-02-PLAN.md — File upload metadata full-stack (QuestionnaireFileUploadResponseDto.cs, QuestionnaireController.cs, dynamicForm.vue)

---

### Phase 4: Component Lifecycle & Propagation
**Goal**: Deleted components are automatically removed from Stages, Workflow duplication produces a complete independent copy, and Stage/Component updates keep Workflow metadata current
**Depends on**: Phase 2
**Requirements**: COMP-01, COMP-02, COMP-03, LOG-05
**Success Criteria** (what must be TRUE):
  1. Deleting a Checklist removes its ID from any Stage's ChecklistId field and ComponentsJson — no orphan references remain
  2. Deleting a Questionnaire removes its ID from any Stage's QuestionnaireId field and ComponentsJson — no orphan references remain
  3. Duplicating a Workflow produces a new Workflow whose Stages have a fully independent copy of ComponentsJson, ViewPermissionMode, ViewTeams, and OperateTeams
  4. Updating a Stage or Component within a Workflow updates the parent Workflow's UpdatedBy and UpdateDate fields
**Plans**: 2 plans

Plans:
- [x] 04-01-PLAN.md — Workflow duplicate deep copy + TouchWorkflowAuditAsync repo method + StageService wire-up (WorkflowService.cs, IWorkflowRepository.cs, WorkflowRepository.cs, StageService.cs)
- [ ] 04-02-PLAN.md — Cascade delete cleanup for Checklist + Questionnaire + ChecklistService/QuestionnaireService UpdateAsync audit touch (ChecklistService.cs, QuestionnaireService.cs)

---

### Phase 5: Permission Fix
**Goal**: Users assigned Case edit permission via User Group can actually edit Cases
**Depends on**: Phase 4
**Requirements**: PERM-01
**Success Criteria** (what must be TRUE):
  1. A user added to a User Group with Case edit permission can open and submit edits to a Case
  2. The permission check passes through PermissionService → IdentityHub IAM → UserGroup without blocking valid users
**Plans**: TBD

---

## Progress

**Execution Order:**
Phases execute in numeric order. Plans within a phase may run in parallel (parallelization: true).

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Number Type Support | 2/2 | Complete | 2026-05-25 |
| 2. Log & Audit Fixes | 0/2 | Not started | - |
| 3. Frontend UX & Data | 0/2 | Not started | - |
| 4. Component Lifecycle & Propagation | 1/2 | In Progress|  |
| 5. Permission Fix | 0/? | Not started | - |
