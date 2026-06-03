---
phase: 03-frontend-ux-data
plan: "01"
subsystem: frontend
tags: [ux, validation, vue, element-plus]
dependency_graph:
  requires: []
  provides: [UX-02, UX-03, DATA-02]
  affects:
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/EditableStageHeader.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue
tech_stack:
  added: []
  patterns: [el-collapse-transition, v-show toggle, slot replacement]
key_files:
  modified:
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/EditableStageHeader.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue
decisions:
  - "Used rows.some instead of rows.every for short_answer_grid so any single filled cell satisfies required validation"
  - "Used el-collapse-transition + v-show (not v-if) to preserve DOM state when stage detail is collapsed"
  - "Added @click.stop to Reassign, Add Co-assignee, and edit pencil buttons so they do not bubble to the toggleExpanded handler"
  - "Removed title prop from PageHeader and replaced with #title slot to avoid duplicate rendering alongside GradientTag"
metrics:
  duration: "~15 minutes"
  completed: "2026-06-03"
  tasks_completed: 3
  tasks_total: 3
  files_modified: 4
---

# Phase 03 Plan 01: Case Status Tag Inline, Stage Header Collapse, Grid Validation Relaxation Summary

Three independent frontend-only changes addressing UX-02, UX-03, and DATA-02.

## Tasks Completed

### Task 1 — UX-02: Case status tag inline with Case name

**File:** `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue`

Removed the `title` prop and `#description` slot from `PageHeader`. Added a `#title` slot containing a `span.flex.items-center.gap-2` wrapper with the case code/name text and `GradientTag` side by side. The status tag now renders on the same line as the case name instead of on a separate description row below it.

### Task 2 — UX-03: Collapsible stage detail header

**File:** `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/EditableStageHeader.vue`

- Added `isExpanded = ref(true)` and `toggleExpanded` function.
- Added `ArrowRight` icon import from `@element-plus/icons-vue`.
- Title row div gets `@click="toggleExpanded"` and `cursor-pointer` class.
- Edit pencil, Reassign, and Add Co-assignee buttons get `@click.stop` to prevent collapse toggling.
- Chevron `el-icon.collapse-icon` added to title row with `:class="{ rotated: isExpanded }"`.
- The collapsible body (assignees, co-assignees, description, divider, date grid) is wrapped in `<el-collapse-transition><div v-show="isExpanded">`.
- Scoped CSS adds `transition: transform 0.3s ease` on `.collapse-icon` and `transform: rotate(90deg)` on `.rotated`.
- Default state is expanded, preserving existing behavior.

### Task 3 — DATA-02: Relax short_answer_grid required validation

**Files:**
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue`
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue`

`dynamicForm.vue` `validateForm`: replaced the `allRowsCompleted` / per-row `rowHasValue` loop with a flat `anyCellFilled` loop over all rows and columns. Validation now fails only if every cell is empty.

`QuestionnaireDetails.vue` `isQuestionAnswered`: changed `rows.every` to `rows.some` in the `short_answer_grid` branch. The inner `columns.some` and `findUserAnswer`/`isAnswerValid` calls are unchanged. A grid passes the required check as soon as any single cell has a valid answer.

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `pnpm type:check`: zero errors in all four modified files (pre-existing `.vue.js` TS6504 errors in unrelated files are unchanged)
- `grep "template #title" detail.vue` — match found (line 8)
- `grep "el-collapse-transition" EditableStageHeader.vue` — match found (lines 41, 147)
- `grep "anyCellFilled" dynamicForm.vue` — match found (lines 1165, 1171, 1175)
- `grep "rows\.some" QuestionnaireDetails.vue` — match found (line 268)
- `grep -c "template #description" detail.vue` — returns 0

## Commits

| Hash | Message |
|------|---------|
| 8f0b2239 | feat(03-01): case status tag inline, stage header collapse, grid validation relaxation |

## Self-Check: PASSED

All four modified files exist and the commit `8f0b2239` is present in git log.
