# Research Summary

**Project:** FlowFlex Question Number Type  
**Synthesized:** 2026-05-25

## Executive Summary

The Number type is **already ~70% implemented** in the codebase. The rendering (`PreviewContent.vue`), condition engine (`conditionEnum.ts`), and rule utilities (`ruleUtils.ts`) all support `number`. The only missing piece is the type registration in `createQuestion.vue` and backend validation.

This is a very small feature — primarily wiring up what already exists.

## Key Findings

### Stack
- Use `el-input-number` (already used 9 times in codebase, returns proper number type)
- Backend validation: `double.TryParse` in `QuestionnaireAnswerService.cs`
- Do NOT use `el-input type="number"` (returns strings, allows invalid chars)
- Do NOT use FluentValidation (wrong abstraction for dynamic JSON answers)

### Table Stakes (already mostly done)
- Numeric-only input rendering — already done
- Required field validation — already done
- Condition rule support — already done
- **Only missing:** type registration in `questionTypes` array + backend validation

### Architecture
- 3 files need modification: `createQuestion.vue`, `dynamicForm.vue`, `QuestionnaireAnswerService.cs`
- No database migration required
- Backend is type-agnostic — stores JSONB without type-specific logic
- Build order: type registration → form rendering → backend validation (last two can parallel)

### Watch Out For
1. **Incomplete branch coverage** — 6+ locations switch on question type; must add `number` to all
2. **String vs numeric comparison** — rules engine may compare "9" > "10" as strings
3. **el-input-number emits undefined on clear** — handle null in form data collection
4. **QuestionEditor type-switch reset** — clears min/max to linear_scale defaults; don't save these for number
5. **QuestionnaireAnswerParser missing case** — add explicit `number` case

## Implementation Scope

| Area | Files | Effort |
|------|-------|--------|
| Type registration | `createQuestion.vue` | ~5 lines |
| Form rendering | `dynamicForm.vue` | ~20 lines |
| Backend validation | `QuestionnaireAnswerService.cs` | ~10 lines |
| Answer parser | `QuestionnaireAnswerParser.cs` | ~5 lines |
| **Total** | **3-4 files** | **~40 lines** |

## Recommendation

Single phase, 2-3 plans maximum. Frontend and backend work can run in parallel. This is a wiring task, not a design task.
