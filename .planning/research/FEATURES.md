# Feature Landscape

**Domain:** Questionnaire/Form Number Field Type  
**Researched:** 2026-05-25

## Current State in Codebase

The `number` type is **partially implemented** already:
- Preview rendering: `PreviewContent.vue` uses `el-input-number` with `min`, `max`, `step`, `placeholder`
- Condition rules: `conditionEnum.ts` maps `number` to `numericOperators` (==, !=, >, <, >=, <=)
- Rule utils: `ruleUtils.ts` maps `number` to input type `'number'`
- Validation: `PreviewContent.vue` checks `value !== null && value !== undefined && value !== ''`

**Missing pieces:**
- Not in `questionTypes` array in `createQuestion.vue` (users cannot create Number questions)
- No backend type-specific validation (answers stored as raw JSON)
- No Number-specific configuration editor component

## Table Stakes

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Add `number` to questionTypes array | Users need to select it when creating questions | Low | Just add entry to the array in `createQuestion.vue` |
| Numeric-only input rendering | Core purpose of the type; prevents text entry | Low | Already done in `PreviewContent.vue` via `el-input-number` |
| Required field validation | All other types support `required` toggle | Low | Already done in `PreviewContent.vue` validation switch |
| Placeholder text support | Consistent with short_answer/paragraph UX | Low | Already supported via `item.placeholder` in preview |
| Backend accepts number answers | Answers must persist correctly | Low | Already works; JSONB stores any value |
| Condition rule support | Workflow conditions need to evaluate number answers | Low | Already mapped in `conditionEnum.ts` with numericOperators |

## Differentiators

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Min/Max range validation | Constrain input to valid business ranges | Low | `el-input-number` already accepts `:min` and `:max` props |
| Step size configuration | Control increment granularity | Low | `el-input-number` already accepts `:step` |
| Decimal/Integer toggle | Some fields need whole numbers only | Medium | Need `precision` prop + config UI + backend validation |
| Prefix/Suffix display | Show units like "$", "%", "kg" | Medium | Need wrapper UI around `el-input-number` |
| Backend numeric validation | Reject non-numeric submissions server-side | Low | Add type check in answer submission flow |

## Anti-Features

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Min/Max range constraints | PROJECT.md explicitly excludes; keep scope minimal | Accept any numeric value; add later |
| Integer/Decimal distinction | PROJECT.md explicitly excludes; adds config complexity | Allow any number |
| Number formatting (thousands separator, locale) | Display concern, not input validation | Use browser-native formatting |
| Calculated fields / formulas | Different feature entirely; massive scope | Keep Number as pure input |
| Slider/Range input variant | Different UX paradigm; system already has `linear_scale` | Number type = precise numeric input |
| Data export format conversion | PROJECT.md explicitly excludes | Store as-is in JSONB |

## Feature Dependencies

```
Add to questionTypes array (required first)
    --> Numeric input rendering (already done)
    --> Required validation (already done)
    --> Condition rule support (already done)

Backend numeric validation (independent, can be done in parallel)
    --> No dependencies on frontend work
```

## MVP Recommendation

1. Add `number` to `questionTypes` array in `createQuestion.vue`
2. Frontend: restrict input to numeric only — already done via `el-input-number`
3. Backend: validate that Number-type answers are numeric — add type check in answer submission path
