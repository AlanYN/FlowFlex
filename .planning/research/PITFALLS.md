# Pitfalls Research

**Domain:** Number Field Type Implementation  
**Researched:** 2026-05-25

## Critical Pitfalls

### 1. Incomplete Branch Coverage

**Risk:** 6+ locations switch on question type; missing `number` in any one causes silent incorrect behavior.

**Warning signs:**
- Customer overview shows raw JSON instead of formatted number
- Answer parser falls through to default case
- AI parser generates wrong type name variants

**Prevention:**
- Search all `type === 'short_answer'` and `type === 'linear_scale'` patterns to find every switch point
- Add explicit `number` handling in each location

**Phase:** Implementation — verify all switch points during development

### 2. String vs Numeric Comparison in Rules Engine

**Risk:** Answers stored as strings; `"9" > "10"` is true in string comparison, breaking workflow conditions.

**Warning signs:**
- Conditions with `>` or `<` operators produce wrong results for multi-digit numbers
- Works for single-digit numbers but fails for 10+

**Prevention:**
- Verify `RuleInputDataBuilder.cs` performs numeric coercion when field type is `number`
- Test condition evaluation with values like 9 vs 10, 100 vs 99

**Phase:** Implementation — test during backend validation work

### 3. el-input-number Emits Undefined on Clear

**Risk:** When user clears the input, `el-input-number` emits `undefined`/`null` instead of a number, causing validation failures.

**Warning signs:**
- Required validation passes but backend rejects the value
- Form shows no error but submission fails

**Prevention:**
- Handle `null`/`undefined` in form data collection
- Initialize number fields to `null` (not `0` or empty string)
- Backend validation should allow null for non-required fields

**Phase:** Implementation — handle in dynamicForm.vue

## Moderate Pitfalls

### 4. QuestionnaireAnswerParser Missing Case

`QuestionnaireAnswerParser.cs` switch statement has no `number` case — falls through to default. Add explicit case.

### 5. Customer Overview Display

`customer-overview.vue` has no number-specific display branch — falls to generic text. May need `isNumberType` helper.

### 6. QuestionEditor Type-Switch Reset

`QuestionEditor.vue` resets `min=1, max=5` (linear_scale defaults) on type change. Must not save these to number question structure — clear min/max when switching to number type.

### 7. AI Parser Type Name Variations

AI parser defaults unknown types to `short_answer`. AI may generate `"numeric"` or `"integer"` variants instead of `"number"`. Can defer this fix.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Add type to questionTypes array | Stale min/max from type switch | Clear min/max for number type |
| Frontend input rendering | undefined on clear | Handle null in form data |
| Backend validation | String vs numeric comparison | Verify rules engine coercion |
| Backend answer parsing | Missing switch case | Add explicit case |
| Customer overview display | No number renderer | Add isNumberType helper |

## Open Questions

- Does `RuleInputDataBuilder.cs` already perform numeric coercion when field type is `number`?
- What exact value does `el-input-number` emit when cleared in this Element Plus version?
