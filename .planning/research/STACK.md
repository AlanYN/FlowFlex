# Stack Research

**Domain:** Questionnaire Number Field Type Implementation  
**Researched:** 2026-05-25

## Current Stack Context

- **Frontend:** Vue 3 + Element Plus + TypeScript
- **Backend:** .NET 8 + SqlSugar ORM
- **Database:** PostgreSQL with JSONB columns
- **Existing pattern:** `el-input-number` already used 9 times in codebase

## Prescriptive Recommendations

### Frontend

| Recommendation | Confidence | Rationale |
|----------------|------------|-----------|
| Use `el-input-number` (not `el-input type="number"`) | HIGH | Returns number type, blocks non-numeric chars, consistent with codebase |
| Place Number after Paragraph in type list | HIGH | Standard form builder UX grouping |
| Icon: `mdi:numeric` | MEDIUM | Matches existing Iconify prefix patterns |
| Do NOT create wrapper component | HIGH | Out of scope; `el-input-number` handles everything needed |
| Do NOT add precision/step config UI | HIGH | Explicitly out of scope per PROJECT.md |

### Backend

| Recommendation | Confidence | Rationale |
|----------------|------------|-----------|
| Validate with `double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)` | HIGH | Culture-invariant, handles integers and decimals |
| Throw `CRMException(ErrorCodeEnum.ParamInvalid, ...)` | HIGH | Matches existing error pattern |
| Do NOT use FluentValidation | HIGH | Wrong abstraction — answers are dynamic JSON, not typed DTOs |
| Do NOT add min/max validation | HIGH | Explicitly out of scope |

### What NOT to Do

| Anti-Pattern | Why |
|--------------|-----|
| `<el-input type="number">` | Returns strings, allows `e`/`+`/`-`, inconsistent |
| FluentValidation on `AnswerJson` | Can't validate individual fields in a JSON blob at DTO level |
| Strongly-typed answer models | Schema is dynamic JSONB, don't fight it |
| Keystroke-level validation watchers | `el-input-number` handles this natively |

## Key Technical Facts

- `PreviewContent.vue` already renders `el-input-number` for `type === 'number'` (line 357)
- `conditionEnum.ts` already maps `number` to numeric operators
- `JsonNumberHandling.AllowReadingFromString` is already configured
- Backend is type-agnostic for answers — stores/retrieves JSONB without type-specific logic
- Only `createQuestion.vue` questionTypes array is missing the `number` entry

## Implementation Size

This is a very small feature — primarily wiring up what already exists:
- 1 file for type registration (frontend)
- 1 file for form rendering (dynamicForm.vue)
- 1 file for backend validation (QuestionnaireAnswerService.cs)
