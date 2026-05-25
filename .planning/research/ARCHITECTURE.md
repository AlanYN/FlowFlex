# Architecture Research

**Domain:** Number Field Type Integration  
**Researched:** 2026-05-25

## Components That Need Changes

| Component | File | Change |
|-----------|------|--------|
| Question Type Registry | `packages/flowFlex-common/src/app/views/onboard/questionnaire/createQuestion.vue` | Add `{ id: 'number', name: 'Number', icon: 'mdi:numeric' }` to `questionTypes` array (~line 411) |
| Dynamic Form Rendering | `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/dynamicForm.vue` | Add `el-input-number` template block for `question.type === 'number'` |
| Form Initialization | Same `dynamicForm.vue` | Initialize number fields to `null` (not empty string) to avoid showing `0` |
| Answer Application | Same `dynamicForm.vue` | Parse stored answer as `Number()` in `applyAnswers()` |
| Preview Renderer | `packages/flowFlex-common/src/app/views/onboard/questionnaire/components/PreviewContent.vue` | Already implemented — no change needed |
| Backend Validation | `packages/flowFlex-backend/Application/Services/OW/QuestionnaireAnswerService.cs` | Add `double.TryParse` check when `response.Type == "number"` |

## Data Flow

```
Design: QuestionTypesPanel -> QuestionEditor -> section.questions[] -> structure_json JSONB
Fill:   dynamicForm loads structure -> renders el-input-number -> formData[id] = number
Save:   collectFormData() -> { questionId, answer: value, type: 'number' } -> POST API -> JSONB
Load:   GET API -> applyAnswers() -> Number(ans.answer) -> formData[id]
```

## Build Order (Critical Path)

1. Frontend type registration (createQuestion.vue) — no dependencies
2. Frontend form rendering (dynamicForm.vue) — depends on step 1 for testing
3. Backend validation (QuestionnaireAnswerService.cs) — can parallel with step 2
4. End-to-end verification

## Key Architecture Insights

- Backend is type-agnostic for questionnaire answers — stores/retrieves JSONB without type-specific logic
- `PreviewContent.vue` already supports number rendering — partial implementation exists
- Only 3 files need modification for the full feature
- No database migration required — types are string values inside JSONB columns
- The condition engine already handles number comparisons via `conditionEnum.ts`
