# Phase 3: Frontend UX & Data - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Case page layout improvements, Stage Detail collapsible header, file upload metadata display (UploadedBy + UploadDate), and Short Answer Grid validation relaxation. All frontend-focused with one backend change (file upload metadata storage).

Requirements: UX-02, UX-03, DATA-01, DATA-02 (4 items)

</domain>

<decisions>
## Implementation Decisions

### Case Status Tag Position (UX-02)
- **D-01:** Move "In Progress" status tag from below the Case Name to the right side of the Case Name (same line as "C00020 - Test Company")
- **D-02:** Use `flex` layout with `items-center gap-2` to place tag inline with the name

### Stage Detail Collapse (UX-03)
- **D-03:** The Stage header area (red-bordered region in screenshot) is collapsible — the region containing Assigned to, Co-assignees, description, Start Date / Est. Duration / ETA
- **D-04:** Collapsed state shows ONLY: Stage name (left) + "Reassign" + "+ Add Co-assignee" buttons (right)
- **D-05:** Use `el-collapse-transition` pattern (already exists in QuestionnaireDetails.vue and InternalNotes.vue)
- **D-06:** Default state: expanded (current behavior preserved)

### File Upload Metadata (DATA-01)
- **D-07:** Store `uploadedBy` (user display name) and `uploadDate` (UTC ISO string) inside the answer JSONB alongside existing file data
- **D-08:** Frontend displays "Uploaded by {name}, {MM/DD/YYYY HH:mm:ss}" to the right of the filename using `timeZoneConvert` with `projectTenMinutesSsecondsDate`
- **D-09:** Historical records without uploadedBy/uploadDate must display gracefully (no error, just omit the metadata line)

### Short Answer Grid Validation (DATA-02)
- **D-10:** Required validation for Short Answer Grid: the entire Grid is considered "filled" if ANY single cell has a non-empty value
- **D-11:** This applies to both frontend validation (Submit button enable) and backend validation
- **D-12:** Previously: all cells must be filled. New rule: `cells.some(c => c.value?.trim().length > 0)`

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Frontend - Case Detail Layout
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` — Case name + status tag position, Stage header structure

### Frontend - Stage Header
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/detail.vue` — Stage header area with Assigned to, description, dates (lines ~88-225 left scrollbar area)

### Frontend - Questionnaire Form
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` — Grid validation logic, file upload display

### Frontend - Collapse Pattern Reference
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue` — el-collapse-transition pattern with isExpanded toggle (reuse this pattern)

### Backend - File Upload
- `packages/flowFlex-backend/WebApi/Controllers/OW/QuestionnaireController.cs` — File upload endpoint
- `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Questionnaire/QuestionnaireFileUploadResponseDto.cs` — Upload response DTO

### Research
- `.planning/research/SUMMARY.md` — Full research synthesis

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `el-collapse-transition` + `isExpanded` ref pattern in QuestionnaireDetails.vue
- `timeZoneConvert(date, false, projectTenMinutesSsecondsDate)` for seconds-precision date display
- `projectTenMinutesSsecondsDate = 'MM/DD/YYYY HH:mm:ss'` format constant

### Established Patterns
- Vue 3 `<script setup>` with `ref<boolean>` for toggle state
- Tailwind `flex items-center gap-2` for inline layout
- Answer data stored in JSONB via `QuestionnaireAnswer.Answer` (JToken field)

### Integration Points
- Case header in `detail.vue` — status tag currently below name
- Stage header in `detail.vue` — the Assigned to / description / dates block
- File upload response DTO → answer JSONB → frontend display
- Grid validation in `dynamicForm.vue` + backend `QuestionnaireAnswerService`

</code_context>

<specifics>
## Specific Ideas

- JIRA screenshot shows the exact layout: Stage name "Customer Application" (yellow highlight) with Reassign/Add Co-assignee buttons on the right — this is the collapsed view
- File format per JIRA: "Uploaded By, Upload Date (格式 MM/DD/YYYY 14:20:47)" displayed to the right of filename
- Grid validation: "填入一个单元格就算是填写了，不需要全部都填满"

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 3-Frontend UX & Data*
*Context gathered: 2026-06-03*
