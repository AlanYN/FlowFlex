# Research: Phase 3 — Frontend UX & Data

**Researched:** 2026-06-03
**Domain:** Vue 3 frontend layout, Element Plus collapse transition, JSONB answer storage, form validation
**Confidence:** HIGH — all findings verified directly from source files

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Move status tag from below Case Name to the right side of the Case Name (same line)
- D-02: Use `flex` layout with `items-center gap-2` to place tag inline
- D-03: Stage header area (Assigned to, Co-assignees, description, dates) is the collapsible region
- D-04: Collapsed state shows ONLY: Stage name (left) + "Reassign" + "+ Add Co-assignee" buttons (right)
- D-05: Use `el-collapse-transition` pattern (already in QuestionnaireDetails.vue and InternalNotes.vue)
- D-06: Default state: expanded
- D-07: Store `uploadedBy` (user display name) and `uploadDate` (UTC ISO string) in answer JSONB alongside existing file data
- D-08: Frontend displays "Uploaded by {name}, {MM/DD/YYYY HH:mm:ss}" using `timeZoneConvert` with `projectTenMinutesSsecondsDate`
- D-09: Historical records without uploadedBy/uploadDate display gracefully (omit metadata line, no error)
- D-10: Short Answer Grid required: entire grid is "filled" if ANY single cell has a non-empty value
- D-11: Applies to both frontend validation (Submit button) and backend validation
- D-12: Previously all cells must be filled. New rule: `cells.some(c => c.value?.trim().length > 0)`

### Claude's Discretion
None — all decisions locked

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| UX-02 | Case 状态 Tag 移到 Case Name 右侧 | Status tag is in `#description` slot of PageHeader in detail.vue; move to title area using flex inline |
| UX-03 | Stage Detail 区域支持展开/收缩 | EditableStageHeader.vue holds the full collapsible region; el-collapse-transition pattern confirmed in QuestionnaireDetails.vue |
| DATA-01 | 文件上传记录 UploadedBy + UploadDate，前端展示 | File answer stored in JSONB `responses[]`; upload response DTO has `UploadTime` but no `UploadedBy`; needs frontend injection at upload time |
| DATA-02 | Short Answer Grid 必填改为"任一单元格非空即通过" | Frontend validateForm already checks per-row with `rowHasValue` logic; backend has no grid-specific validation — only number-type validation exists |
</phase_requirements>

---

## Summary

Phase 3 is four targeted changes across two frontend files and one backend DTO concern. No new dependencies are required. The existing `el-collapse-transition` pattern is confirmed in `QuestionnaireDetails.vue` and can be applied directly to `EditableStageHeader.vue`. The Case status tag is currently rendered inside the `#description` slot of `PageHeader` — moving it inline with the title requires restructuring to use the `#title` slot or an adjacent flex wrapper.

For DATA-01, the file upload response DTO (`QuestionnaireFileUploadResponseDto`) already contains `UploadTime` (DateTime UTC) but has no `UploadedBy` field. The frontend must inject `uploadedBy` from the current user store at the point the file upload response is processed, and store both fields inside the answer JSONB alongside the existing file reference data. The backend save path (`QuestionnaireAnswerService.SaveAnswerAsync`) accepts the JSONB blob verbatim — no backend code change is needed for storage.

For DATA-02, the frontend `validateForm` in `dynamicForm.vue` already has a per-row `rowHasValue` check for `short_answer_grid` (lines 1163–1183). The current logic correctly marks a row as incomplete if no cell in that row has a value. Per D-10/D-12, the new rule is that the entire grid passes if ANY single cell across all rows/columns is non-empty — meaning the loop logic needs to change from row-by-row to a single `cells.some(...)` across the entire grid. The backend has no `short_answer_grid`-specific required validation; it only validates `number` type answers. No backend change is needed for DATA-02.

**Primary recommendation:** All four changes are surgical edits to existing components. No new files, no new packages, no backend migrations required.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Case status tag position | Frontend (Vue SPA) | — | Pure layout/CSS change in detail.vue |
| Stage detail collapse | Frontend (Vue SPA) | — | UI state only; EditableStageHeader.vue owns the region |
| File upload metadata storage | Frontend (Vue SPA) | Backend (API) | Frontend injects uploadedBy at upload-response time; backend stores JSONB verbatim |
| File upload metadata display | Frontend (Vue SPA) | — | Read from existing JSONB answer; render conditionally |
| Grid validation (frontend) | Frontend (Vue SPA) | — | validateForm in dynamicForm.vue |
| Grid validation (backend) | Backend (Application) | — | QuestionnaireAnswerService — currently no grid validation, D-11 requires adding it |

---

## UX-02: Case Status Tag Position

### Current DOM Structure

In `detail.vue`, the `PageHeader` component is used with two named slots:

```html
<PageHeader
    :title="`${onboardingData?.caseCode || ''} - ${onboardingData?.caseName || ''}`"
    ...
>
    <template #description>
        <!-- Status tag is here — BELOW the title -->
        <div class="flex items-center" v-if="onboardingData?.status">
            <GradientTag ... />
        </div>
    </template>
    <template #actions>
        <!-- Save / Complete / Overview / Share buttons -->
    </template>
</PageHeader>
```

The `PageHeader` component (confirmed in `src/app/components/global/PageHeader/index.vue`) renders:
- `<h1>` for the `#title` slot / `title` prop
- `<p>` for the `#description` slot — this is a `<p>` tag with `opacity-85`, on its own line below h1

### Required Change

The `GradientTag` must move from the `#description` slot to be inline with the title text. PageHeader does not have a dedicated inline-with-title slot, so the approach is:

**Option A (recommended):** Override the `#title` slot with a flex wrapper:
```html
<template #title>
    <span>{{ onboardingData?.caseCode || '' }} - {{ onboardingData?.caseName || '' }}</span>
    <GradientTag
        v-if="onboardingData?.status"
        :type="statusTagType"
        :text="statusDisplayText"
        :pulse="statusShouldPulse"
        size="small"
        class="ml-2 align-middle"
    />
</template>
```
The `h1` in PageHeader already uses `flex-1` container — adding `flex items-center gap-2` to the wrapping `div` inside PageHeader is not needed since the slot content flows inline inside the `h1`.

**Option B:** Wrap the `h1` slot content in a `flex items-center gap-2` span. Same effect.

The `#description` slot should then be removed or left empty (the `<p>` only renders if `description || $slots.description` is truthy — removing the slot prevents the blank `<p>` from rendering).

### Key constraint

The `h1` in PageHeader has `text-2xl font-semibold text-white` — GradientTag `size="small"` is already used and renders correctly at that scale (confirmed pattern in the codebase).

---

## UX-03: Stage Detail Collapse

### Current Structure in EditableStageHeader.vue

The component has two top-level states: `isEditing` (edit form) and display state. The collapsible region is the entire display-state body — specifically everything inside the `v-if="!isEditing"` div below the title row.

Current display-state structure (simplified):
```html
<div v-if="!isEditing">
    <!-- ALWAYS VISIBLE: title + action buttons -->
    <div class="flex items-center justify-between">
        <h2 class="font-bold text-xl">{{ displayTitle }}</h2>
        <div class="flex items-center gap-2">
            <el-button>Reassign</el-button>
            <el-button>Add Co-assignee</el-button>
            <el-button link @click="handleEdit" />   <!-- edit pencil -->
        </div>
    </div>

    <!-- COLLAPSIBLE: everything below the title row -->
    <div class="my-2 space-y-2">          <!-- Assigned to + Co-assignees -->
    <div>{{ stageDescription }}</div>      <!-- Description -->
    <el-divider />
    <div class="grid ...">                 <!-- Start Date / Est Duration / ETA -->
</div>
```

### Collapse Pattern (from QuestionnaireDetails.vue)

```typescript
// State
const isExpanded = ref(true); // default expanded per D-06

// Toggle
const toggleExpanded = () => {
    isExpanded.value = !isExpanded.value;
};
```

```html
<!-- Header row — always visible, click to toggle -->
<div class="flex items-center justify-between" @click="toggleExpanded">
    <h2>...</h2>
    <div class="flex items-center gap-2">
        <!-- Reassign + Add Co-assignee buttons -->
        <!-- Chevron icon to indicate expand state -->
        <el-icon :class="{ rotated: isExpanded }"><ArrowRight /></el-icon>
    </div>
</div>

<!-- Collapsible body -->
<el-collapse-transition>
    <div v-show="isExpanded">
        <!-- assignees, description, divider, date grid -->
    </div>
</el-collapse-transition>
```

### Important: Edit pencil button interaction

The edit button (`handleEdit`) is currently inside the title row. Clicking it must NOT trigger the collapse toggle. Use `@click.stop` on the edit button, or move it outside the clickable header area.

### CSS class for chevron rotation (from QuestionnaireDetails.vue)

```scss
.case-component-expand-icon {
    transition: transform 0.3s ease;
}
.rotated {
    transform: rotate(90deg);
}
```

Apply same pattern inside EditableStageHeader's scoped styles.

---

## DATA-01: File Upload Metadata

### Current File Upload Flow

1. User drags/selects file in `dynamicForm.vue` → `handleFileChange(questionId, file, fileList)` stores raw `fileList` in `formData[questionId]`
2. `transformFormDataForAPI()` serializes file-type questions via the generic "else" branch:
   ```typescript
   answer: formData.value[question.id],   // this is the fileList array
   responseText: Array.isArray(...) ? fileList.join(', ') : ...
   ```
   The file list objects from `el-upload` contain `{ name, url, raw, status, uid }` — no uploadedBy or uploadDate.
3. `QuestionnaireFileUploadResponseDto` fields: `Id`, `Success`, `AccessUrl`, `OriginalFileName`, `FileName`, `FilePath`, `FileSize`, `ContentType`, `Category`, `FileHash`, `UploadTime` (DateTime UTC), `Gateway`, `FullAccessUrl`.
   **Missing:** No `UploadedBy` field on the DTO.

### Where the API call happens

Search required — the file upload question uses `el-upload` with `:auto-upload="false"`, meaning the actual HTTP upload to `POST /ow/questionnaires/v1/questions/upload-file` happens separately. Need to locate the upload API call in the frontend API layer.
<br>
Likely in `src/app/apis/ow/questionnaire.ts` or similar — the `transformFormDataForAPI` path does not upload files; it serializes already-uploaded file references.

### Required Changes

**Backend** — Add `UploadedBy` to `QuestionnaireFileUploadResponseDto`:
```csharp
/// <summary>Uploader display name</summary>
public string UploadedBy { get; set; }
```
And populate it in `QuestionnaireController.UploadQuestionFileAsync`:
```csharp
UploadedBy = _userContext.UserName,
```

**Frontend** — When processing the upload response, inject `uploadedBy` and `uploadDate` into the file object stored in `formData`:
```typescript
// After successful upload response:
const fileWithMeta = {
    ...uploadResponse,
    uploadedBy: uploadResponse.uploadedBy,
    uploadDate: uploadResponse.uploadTime,  // UTC ISO string from backend
};
```

**Display** — In `dynamicForm.vue`, when rendering already-answered file questions (from `applyAnswers`), show metadata if present:
```html
<div v-if="file.uploadedBy || file.uploadDate" class="text-xs text-gray-500 ml-2">
    Uploaded by {{ file.uploadedBy }},
    {{ timeZoneConvert(file.uploadDate, false, projectTenMinutesSsecondsDate) }}
</div>
```
Per D-09: the `v-if` guard ensures historical records without metadata render without error.

### JSONB storage path

`QuestionnaireAnswerService.SaveAnswerAsync` stores `input.AnswerJson` verbatim into `QuestionnaireAnswer.Answer` (JToken). No change needed in the service — whatever the frontend sends in the file answer is stored as-is.

### Key: locate the actual upload call

The frontend questionnaire API module must be checked to confirm where the upload response is processed. The `el-upload` `:auto-upload="false"` setup means there is a manual upload trigger somewhere outside `handleFileChange`. This is the injection point for `uploadedBy` and `uploadDate`.

---

## DATA-02: Short Answer Grid Validation

### Current Frontend Logic (dynamicForm.vue, lines 1163–1183)

```typescript
} else if (question.type === 'short_answer_grid') {
    if (question.rows && question.columns && question.columns.length > 0) {
        let allRowsCompleted = true;
        question.rows.forEach((row: any, rowIndex: number) => {
            // Current: each row must have at least one cell with content
            let rowHasValue = false;
            question.columns.forEach((column: any, columnIndex: number) => {
                const gridKey = `${question.id}_${column.id}_${row.id}`;
                const gridValue = formData.value[gridKey];
                if (gridValue && gridValue.trim() !== '') {
                    rowHasValue = true;
                }
            });
            if (!rowHasValue) {
                allRowsCompleted = false;
            }
        });
        if (!allRowsCompleted) {
            isValid = false;
            errors.push(errorMsg);
        }
    }
}
```

**Current behavior:** Every row must have at least one non-empty cell. A 3-row grid fails if any row is completely empty.

**Required behavior (D-10/D-12):** The entire grid passes if ANY single cell across ALL rows and columns is non-empty.

**New logic:**
```typescript
} else if (question.type === 'short_answer_grid') {
    if (question.rows && question.columns && question.columns.length > 0) {
        let anyCellFilled = false;
        question.rows.forEach((row: any) => {
            question.columns.forEach((column: any) => {
                const gridKey = `${question.id}_${column.id}_${row.id}`;
                const gridValue = formData.value[gridKey];
                if (gridValue && gridValue.trim() !== '') {
                    anyCellFilled = true;
                }
            });
        });
        if (!anyCellFilled) {
            isValid = false;
            errors.push(`${sIndex + 1} - ${qIdx + 1}`);
        }
    }
}
```

### QuestionnaireDetails.vue — isQuestionAnswered (lines 257–276)

There is a SECOND location that evaluates grid completion — the `isQuestionAnswered` function in `QuestionnaireDetails.vue`, used to compute `completionStats` and `canSubmitQuestionnaire`:

```typescript
if (question.type === 'short_answer_grid') {
    // Current: checks every row has at least one column with content
    return question.rows.every((row: any) => {
        return question.columns.some((column: any) => {
            const gridKey = `${questionId}_${column.id}_${row.id}`;
            const userAnswer = findUserAnswer(answers, gridKey);
            return isAnswerValid(userAnswer);
        });
    });
}
```

**This must also change** to match D-10:
```typescript
if (question.type === 'short_answer_grid') {
    // New: any single cell filled = entire grid answered
    return question.rows.some((row: any) => {
        return question.columns.some((column: any) => {
            const gridKey = `${questionId}_${column.id}_${row.id}`;
            const userAnswer = findUserAnswer(answers, gridKey);
            return isAnswerValid(userAnswer);
        });
    });
}
```

### Backend Validation

`QuestionnaireAnswerService` has NO `short_answer_grid` required validation. It only validates `number` type answers via `ValidateNumberAnswers`. The backend accepts the JSONB blob as-is. D-11 says "applies to both frontend and backend" — but since the backend currently has no grid-required enforcement, D-11 is effectively a frontend-only change. No backend code is added unless a future explicit backend guard is desired. The planner should confirm this interpretation with the user.

---

## Implementation Risks

### Risk 1: UX-02 — PageHeader h1 slot flex alignment

The `h1` tag in PageHeader does not have `flex` by default. Putting a `GradientTag` inside the `#title` slot places it inline with the text node, but vertical alignment depends on `h1`'s `leading-tight` class. The tag may sit slightly above or below baseline text.

**Mitigation:** Wrap the title slot content in `<span class="flex items-center gap-2">`. The `h1` renders its children inline — a flex span inside works correctly.

### Risk 2: UX-03 — Edit button inside clickable header

The edit pencil button is currently inside the title row div. If the entire title row becomes `@click="toggleExpanded"`, clicking the pencil will both open the edit form AND toggle collapse state.

**Mitigation:** Add `@click.stop` to the edit button (already a pattern used elsewhere in the codebase for nested clickable elements).

### Risk 3: UX-03 — Collapse does not affect edit state

When `isEditing` is true, the collapse wrapper is irrelevant (edit form replaces display). The `isExpanded` state should not be reset when editing begins. When the user cancels edit, the collapsed/expanded state should be preserved from before editing.

**Mitigation:** `isEditing` and `isExpanded` are independent refs — no interference by default.

### Risk 4: DATA-01 — Upload call location unknown without further lookup

The actual file upload API call location has not been confirmed in the frontend. `el-upload` is set to `:auto-upload="false"` in dynamicForm.vue, meaning there is a separate manual trigger somewhere. The `handleFileChange` only updates `formData`. The injection point for `uploadedBy`/`uploadDate` depends on finding this trigger.

**Mitigation:** Planner should add a task to locate `POST /ow/questionnaires/v1/questions/upload-file` call in the frontend API layer before implementing DATA-01.

### Risk 5: DATA-02 — Two validation locations must stay in sync

`validateForm` in `dynamicForm.vue` and `isQuestionAnswered` in `QuestionnaireDetails.vue` both evaluate grid completion independently. If only one is updated, the Submit button state (driven by `canSubmitQuestionnaire`) will disagree with the validation error shown on form submission.

**Mitigation:** Both must be updated in the same task/commit.

---

## Recommended Plan Structure

The four requirements are independent — each can be a separate task with no cross-dependency. Recommended order:

**Task 1 — UX-02:** Edit `detail.vue` — move GradientTag from `#description` slot into `#title` slot with flex wrapper. Remove empty `#description` slot. (~15 lines changed)

**Task 2 — UX-03:** Edit `EditableStageHeader.vue` — add `isExpanded` ref, wrap collapsible body in `el-collapse-transition` + `v-show`, make title row clickable, add `@click.stop` to edit button, add chevron icon with rotation CSS. (~40 lines changed)

**Task 3 — DATA-01:** 
- Step A: Add `UploadedBy` to `QuestionnaireFileUploadResponseDto.cs` and populate in controller.
- Step B: Locate upload API call in frontend, inject `uploadedBy` + `uploadDate` into stored file object.
- Step C: Add conditional display in `dynamicForm.vue` file question render block.

**Task 4 — DATA-02:** Edit two locations:
- `dynamicForm.vue` `validateForm` — change `every row` to `any cell` for `short_answer_grid`
- `QuestionnaireDetails.vue` `isQuestionAnswered` — change `rows.every` to `rows.some` for `short_answer_grid`

---

## Code Examples

### el-collapse-transition pattern (from QuestionnaireDetails.vue)

```typescript
// VERIFIED from QuestionnaireDetails.vue
const isExpanded = ref(true);

const toggleExpanded = () => {
    isExpanded.value = !isExpanded.value;
};
```

```html
<!-- Header — always visible -->
<div class="flex items-center justify-between" @click="toggleExpanded">
    <h2>{{ displayTitle }}</h2>
    <div class="flex items-center gap-2">
        <el-button @click.stop="openReassignDialog">Reassign</el-button>
        <el-button @click.stop="openAddCoassigneeDialog">Add Co-assignee</el-button>
        <el-button link @click.stop="handleEdit" :disabled="disabled || !currentStage?.startTime" />
        <el-icon class="collapse-icon" :class="{ rotated: isExpanded }"><ArrowRight /></el-icon>
    </div>
</div>

<!-- Collapsible body -->
<el-collapse-transition>
    <div v-show="isExpanded">
        <!-- my-2 space-y-2: Assigned to + Co-assignees -->
        <!-- stageDescription div -->
        <!-- el-divider -->
        <!-- grid cols Start Date / Est Duration / ETA -->
    </div>
</el-collapse-transition>
```

### timeZoneConvert for file metadata display

```typescript
// VERIFIED from EditableStageHeader.vue — import pattern
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinutesSsecondsDate } from '@/settings/projectSetting';

// Usage
timeZoneConvert(file.uploadDate, false, projectTenMinutesSsecondsDate)
// produces "MM/DD/YYYY HH:mm:ss" in user's local timezone
```

### Status tag inline with title

```html
<!-- In detail.vue — replace current #description slot usage -->
<PageHeader
    :show-back-button="true"
    @go-back="handleBack"
>
    <template #title>
        <span class="flex items-center gap-2">
            <span>{{ onboardingData?.caseCode || '' }} - {{ onboardingData?.caseName || '' }}</span>
            <GradientTag
                v-if="onboardingData?.status"
                :type="statusTagType"
                :text="statusDisplayText"
                :pulse="statusShouldPulse"
                size="small"
            />
        </span>
    </template>
    <template #actions>
        <!-- unchanged -->
    </template>
</PageHeader>
```

Note: The `title` prop on PageHeader is replaced by the `#title` slot. The slot renders inside the `h1` — removing the `title` prop avoids duplicate rendering.

---

## Open Questions

1. **DATA-01: Where is the frontend file upload API call?**
   - What we know: `el-upload :auto-upload="false"` in dynamicForm.vue; upload endpoint is `POST /ow/questionnaires/v1/questions/upload-file`
   - What's unclear: Which frontend function calls this endpoint, and where the response is processed
   - Recommendation: Grep for `upload-file` or `uploadFile` in `src/app/apis/ow/` before implementing DATA-01

2. **DATA-02: Backend enforcement for D-11**
   - What we know: Backend currently has no `short_answer_grid` required validation
   - What's unclear: Does D-11 ("applies to both frontend and backend") require adding backend validation, or is the frontend-only change sufficient?
   - Recommendation: Confirm with user. If backend enforcement is needed, add validation in `QuestionnaireAnswerService.SaveAnswerAsync` that checks `short_answer_grid` responses in the JSON — but this is low risk to skip since the frontend gate is the primary enforcement point.

---

## Sources

### Primary (HIGH confidence — verified from source files)
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` — Status tag in `#description` slot, PageHeader usage
- `packages/flowFlex-common/src/app/components/global/PageHeader/index.vue` — Slot structure confirmed
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/EditableStageHeader.vue` — Full display-state structure, existing refs and methods
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue` — el-collapse-transition pattern, isExpanded toggle, isQuestionAnswered for short_answer_grid
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` — validateForm short_answer_grid logic (lines 1163–1183), handleFileChange, transformFormDataForAPI, file type initialization
- `packages/flowFlex-backend/WebApi/Controllers/OW/QuestionnaireController.cs` — UploadQuestionFileAsync, _userContext.UserName available
- `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Questionnaire/QuestionnaireFileUploadResponseDto.cs` — Fields confirmed, no UploadedBy field
- `packages/flowFlex-backend/Application/Services/OW/QuestionnaireAnswerService.cs` — SaveAnswerAsync stores JSONB verbatim, ValidateNumberAnswers only validation

---

## Metadata

**Confidence breakdown:**
- UX-02 (status tag): HIGH — DOM structure verified, PageHeader slots confirmed
- UX-03 (collapse): HIGH — EditableStageHeader.vue structure fully read, collapse pattern confirmed from QuestionnaireDetails.vue
- DATA-01 (file metadata): MEDIUM — backend change clear; frontend injection point not yet located
- DATA-02 (grid validation): HIGH — both validation locations confirmed, logic change is minimal

**Research date:** 2026-06-03
**Valid until:** 2026-07-03 (stable frontend framework)
