---
phase: 03-frontend-ux-data
plan: 02
subsystem: questionnaire-file-upload
tags: [file-upload, metadata, uploader, timestamp, questionnaire]
requirements: [DATA-01]
dependency_graph:
  requires: []
  provides: [uploadedBy-in-upload-response, file-metadata-display]
  affects: [QuestionnaireFileUploadResponseDto, QuestionnaireController, dynamicForm.vue]
tech_stack:
  added: []
  patterns: [server-assigned-metadata, conditional-display, immediate-upload-on-change]
key_files:
  created: []
  modified:
    - packages/flowFlex-backend/Application.Contracts/Dtos/OW/Questionnaire/QuestionnaireFileUploadResponseDto.cs
    - packages/flowFlex-backend/WebApi/Controllers/OW/QuestionnaireController.cs
    - packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue
decisions:
  - "Upload metadata (uploadedBy, uploadDate) injected at file-change time via immediate API call in handleFileChange — not at submit time — so metadata is available in the file list UI immediately"
  - "el-upload show-file-list renders its own list; metadata is shown in a separate div below the upload widget, one row per file, guarded by v-if on uploadedBy || uploadDate"
  - "handleFileChange converted to async to await the uploadQuestionFile call; non-blocking on error so form usability is preserved"
metrics:
  completed: 2026-06-03
---

# Phase 3 Plan 2: File Upload Metadata (UploadedBy + UploadDate) Summary

Full-stack addition of uploader name and upload timestamp to the questionnaire file upload flow, from backend DTO through controller assignment to frontend display.

## What Was Done

### Task 1 — Backend: UploadedBy field (DATA-01, D-07)

Added `public string UploadedBy { get; set; }` to `QuestionnaireFileUploadResponseDto.cs`, placed after `UploadTime` for logical grouping.

In `QuestionnaireController.UploadQuestionFileAsync`, assigned `UploadedBy = _userContext.UserName` in the response DTO object initializer. The value comes from the JWT-authenticated user context — it is server-assigned and not controllable by the client (T-03-02-01 mitigated).

dotnet build: 0 errors.

### Task 2 — Frontend: metadata injection and display (DATA-01, D-08, D-09)

Three changes to `dynamicForm.vue`:

**Imports added:**
- `projectTenMinutesSsecondsDate` from `@/settings/projectSetting`
- `timeZoneConvert` from `@/hooks/time`
- `uploadQuestionFile` from `@/apis/ow/questionnaire`

**handleFileChange converted to async upload-on-change:**
The existing `handleFileChange` was a synchronous no-op (just stored the Element Plus file list). It is now `async` and immediately calls `uploadQuestionFile` for any new file (`file.raw` present and `uploadedBy` not yet set). On a 200 response the file entry in `formData[questionId]` is enriched with:
- `uploadedBy` ← `response.data.data.uploadedBy` (new field from Task 1)
- `uploadDate` ← `response.data.data.uploadTime` (existing UTC timestamp)
- `accessUrl`, `fullAccessUrl`, `fileId` (bonus: stored for downstream use)

Upload errors are swallowed silently so the file still appears in the list without metadata.

**Metadata display block added in template:**
Below the `el-upload` widget, inside the same container `div`, a block iterates over `formData[question.id]` and for each file renders:

```
<span v-if="file.uploadedBy || file.uploadDate" class="text-xs text-gray-500 ml-2">
  Uploaded by {{ file.uploadedBy }}, {{ timeZoneConvert(file.uploadDate, false, projectTenMinutesSsecondsDate) }}
</span>
```

The `v-if="file.uploadedBy || file.uploadDate"` guard satisfies D-09: historical file answers that have no metadata fields render without error and without a blank line.

## Files Modified

| File | Change |
|------|--------|
| `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Questionnaire/QuestionnaireFileUploadResponseDto.cs` | Added `UploadedBy string` property after `UploadTime` |
| `packages/flowFlex-backend/WebApi/Controllers/OW/QuestionnaireController.cs` | Assigned `UploadedBy = _userContext.UserName` in `UploadQuestionFileAsync` response initializer |
| `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` | Added 3 imports; converted `handleFileChange` to async with immediate upload + metadata injection; added conditional metadata display span below file list |

## Deviations from Plan

### Auto-added: bonus fields on upload response

**Found during:** Task 2 — upload response handler

**Issue:** While wiring `uploadedBy` and `uploadDate`, `accessUrl`, `fullAccessUrl`, and `fileId` were also available from the same response object at no extra cost.

**Fix:** Added them to the enriched file object so downstream logic (e.g. answer serialization) can use the server-assigned URL.

**Files modified:** `dynamicForm.vue`

No other deviations — plan executed as written.

## Requirements Addressed

- DATA-01: File upload entries in questionnaires now display "Uploaded by {name}, MM/DD/YYYY HH:mm:ss" — uploader name is server-assigned from JWT context, timestamp is server UTC time formatted via `timeZoneConvert`.

## Self-Check

- [x] `QuestionnaireFileUploadResponseDto.cs` contains `UploadedBy` — grep confirmed line 66
- [x] `QuestionnaireController.cs` assigns `UploadedBy` in `UploadQuestionFileAsync` — grep confirmed line 351
- [x] `dynamicForm.vue` contains `uploadedBy` >= 2 matches — grep count: 4
- [x] `dynamicForm.vue` contains `uploadDate` >= 2 matches — grep count: 3
- [x] `dynamicForm.vue` contains `v-if="file.uploadedBy || file.uploadDate"` — grep confirmed line 367
- [x] `dynamicForm.vue` contains `timeZoneConvert` in metadata display span — grep confirmed line 369
- [x] dotnet build: 0 errors (2072 pre-existing warnings, TreatWarningsAsErrors=false)

## Self-Check: PASSED
