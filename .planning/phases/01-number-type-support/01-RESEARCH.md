# Phase 1: Number Type Support - Research

**Researched:** 2026-05-25
**Domain:** Vue 3 questionnaire form + .NET 8 answer validation
**Confidence:** HIGH

## Summary

The Number type is approximately 70% implemented already. `PreviewContent.vue` (the questionnaire designer preview) fully supports `number` with `el-input-number`, null initialization, and required-field validation. `conditionEnum.ts` maps `number` to `numericOperators` and `'number'` input type. The rules engine backend (`RuleUtils.Compare`) already performs `decimal.TryParse` coercion before comparing, so numeric ordering is correct regardless of whether the stored value is a string.

The missing pieces are narrow and well-defined: (1) `createQuestion.vue` does not include `number` in its `questionTypes` array, so users cannot select it in the editor; (2) `dynamicForm.vue` (the live form-filling view) has no `el-input-number` branch and no null initialization for number fields; (3) `QuestionnaireAnswerService.cs` has no type-specific validation — it stores whatever JSON arrives without checking that number-type answers are actually numeric; (4) `QuestionnaireAnswerParser.cs` has no explicit `number` case in its `FormatAnswerWithConfig` switch, so it falls through to `default` (which returns `answer.ToString()` — harmless but worth fixing for consistency).

**Primary recommendation:** Add `number` to `questionTypes` in `createQuestion.vue`, add an `el-input-number` branch + null initialization in `dynamicForm.vue`, add `double.TryParse` validation in `QuestionnaireAnswerService.cs`, and add an explicit `number` case in `QuestionnaireAnswerParser.cs`. No database migration required.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Question type registration (editor) | Frontend (Vue SPA) | — | `questionTypes` array in `createQuestion.vue` drives the type picker UI |
| Number input rendering (form fill) | Frontend (Vue SPA) | — | `dynamicForm.vue` switches on `question.type` to render the correct Element Plus component |
| Null initialization on clear | Frontend (Vue SPA) | — | `el-input-number` emits `undefined` on clear; form data layer must coerce to `null` |
| Answer serialization to JSON | Frontend (Vue SPA) | — | `transformFormDataForAPI` in `dynamicForm.vue` serializes `formData[question.id]` directly |
| Backend numeric validation | API / Backend | — | `QuestionnaireAnswerService.cs` must validate `type === "number"` answers before persisting |
| Rules engine numeric comparison | API / Backend | — | `RuleUtils.Compare` + `ComponentDataService` answer parsing; already handles numeric coercion |
| Change log display | API / Backend | — | `QuestionnaireAnswerParser.cs` formats answers for audit log; needs explicit `number` case |

## Standard Stack

### Core (already in project — no new installs)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `el-input-number` | Element Plus (project version) | Numeric-only input with clear/null support | Already used 9+ times in codebase; returns JS `number` or `undefined` on clear |
| `double.TryParse` | .NET 8 BCL | Backend numeric validation | Standard .NET pattern; no extra dependency |

No new packages are required for this phase.

## Package Legitimacy Audit

No external packages are being installed in this phase. All changes use libraries already present in the project.

**Packages removed due to slopcheck [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### System Architecture Diagram

```
[Editor: createQuestion.vue]
  questionTypes array
  → user selects "Number"
  → question.type = "number" saved in structure_json JSONB

[Form Fill: dynamicForm.vue]
  loads structure_json
  → switch on question.type
  → "number": render <el-input-number v-model="formData[id]" />
  → initialize formData[id] = null (not 0)
  → on clear: el-input-number emits undefined → coerce to null

[Submit: transformFormDataForAPI()]
  → { questionId, answer: formData[id], type: "number" }
  → POST /api/questionnaire-answer
  → QuestionnaireAnswerService.SaveAnswerAsync()
  → validate: double.TryParse(answer) when type == "number"
  → persist JSONB

[Rules Engine: RulesEngineService]
  → ComponentDataService.GetQuestionnaireDataAsync()
  → answer stored as string in answerDict[questionId]
  → RuleUtils.Compare(left, right)
  → decimal.TryParse on both sides → numeric comparison
  (already works correctly — no change needed)

[Change Log: QuestionnaireAnswerParser]
  → FormatAnswerWithConfig switch
  → add explicit "number" case → return answer.ToString()
```

### Recommended Project Structure

No new files or folders needed. All changes are in-place edits to existing files:

```
packages/
├── flowFlex-common/src/app/views/onboard/
│   ├── questionnaire/
│   │   └── createQuestion.vue          # ADD "number" to questionTypes array
│   └── onboardingList/components/
│       └── dynamicForm.vue             # ADD el-input-number branch + null init + applyAnswers
└── flowFlex-backend/Application/Services/OW/
    ├── QuestionnaireAnswerService.cs   # ADD double.TryParse validation
    └── ChangeLog/
        └── QuestionnaireAnswerParser.cs # ADD explicit "number" case
```

### Pattern 1: Question Type Registration

**What:** Add an entry to the `questionTypes` array in `createQuestion.vue`.
**When to use:** Every new question type follows this pattern.

```typescript
// Source: packages/flowFlex-common/src/app/views/onboard/questionnaire/createQuestion.vue ~line 411
// Insert after the 'short_answer_grid' entry:
{
    id: 'number',
    name: 'Number',
    icon: 'mdi:numeric',
},
```

### Pattern 2: el-input-number in dynamicForm.vue

**What:** Add a `v-else-if` branch for `question.type === 'number'` in the question rendering template. Follow the same pattern as `linear_scale` (which also uses a numeric component).

```html
<!-- Source: pattern from PreviewContent.vue lines 357-365 -->
<!-- Insert after the paragraph block, before the date block -->
<el-input-number
    v-else-if="question.type === 'number'"
    v-model="formData[question.id]"
    :placeholder="'Enter number'"
    :controls="false"
    class="w-full"
    :disabled="questionIsDisabled(question.id)"
    @change="handleInputChange(question.id, $event)"
/>
```

### Pattern 3: Null initialization in dynamicForm.vue

**What:** Add a `number` branch in the form initialization block (around line 1625 where `linear_scale` and `rating` are initialized).

```typescript
// Source: dynamicForm.vue ~line 1625 — follows linear_scale/rating pattern
} else if (question.type === 'number') {
    // Initialize to null, not 0 — avoids showing 0 for unanswered fields
    if (!(question?.id in formData.value)) {
        formData.value[question?.id] = null;
    }
}
```

### Pattern 4: applyAnswers for number type

**What:** Add a `number` branch in `applyAnswers()` (around line 869 where `linear_scale` and `rating` are handled).

```typescript
// Source: dynamicForm.vue ~line 869 — follows linear_scale/rating pattern
} else if (ans.type === 'number') {
    const numValue = Number(ans.answer);
    formData.value[ans.questionId] = isNaN(numValue) ? null : numValue;
}
```

### Pattern 5: Backend numeric validation

**What:** In `QuestionnaireAnswerService.SaveAnswerAsync`, parse the answer JSON and validate number-type responses before persisting.

```csharp
// Source: QuestionnaireAnswerService.cs — add before the existing save logic
// After formattedJson is set, validate number fields:
private static void ValidateNumberAnswers(string answerJson)
{
    if (string.IsNullOrWhiteSpace(answerJson)) return;
    using var doc = System.Text.Json.JsonDocument.Parse(answerJson);
    if (!doc.RootElement.TryGetProperty("responses", out var responses)) return;
    foreach (var response in responses.EnumerateArray())
    {
        if (response.TryGetProperty("type", out var type) &&
            type.GetString() == "number" &&
            response.TryGetProperty("answer", out var answer) &&
            answer.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            var raw = answer.ValueKind == System.Text.Json.JsonValueKind.String
                ? answer.GetString()
                : answer.ToString();
            if (!string.IsNullOrEmpty(raw) && !double.TryParse(raw, out _))
            {
                var qId = response.TryGetProperty("questionId", out var qIdEl)
                    ? qIdEl.GetString() : "unknown";
                throw new ArgumentException(
                    $"Question '{qId}' expects a numeric value but received: '{raw}'");
            }
        }
    }
}
```

### Pattern 6: QuestionnaireAnswerParser explicit number case

**What:** Add `case "number":` before `default:` in `FormatAnswerWithConfig`.

```csharp
// Source: QuestionnaireAnswerParser.cs ~line 311
case "number":
    return answer?.ToString() ?? "No answer";
```

### Anti-Patterns to Avoid

- **`el-input type="number"`:** Returns strings, allows `e`, `+`, `-` characters, does not emit `null` on clear. Use `el-input-number` instead.
- **FluentValidation for answer content:** The answer payload is dynamic JSONB — FluentValidation operates on typed DTOs. The `double.TryParse` approach in the service layer is the correct pattern here.
- **Initializing number fields to `0`:** Shows `0` for unanswered questions, which is misleading. Always initialize to `null`.
- **Skipping the `applyAnswers` branch:** Without it, loading a saved number answer will fall through to the `else` branch which assigns the raw string, causing the `el-input-number` to display `NaN` or ignore the value.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Numeric-only input | Custom input with keydown filtering | `el-input-number` | Already handles IME, paste, arrow keys, clear-to-null |
| Numeric comparison in rules | Custom string-to-number coercion in rule expressions | `RuleUtils.Compare` (already exists) | Already does `decimal.TryParse` on both sides before comparing |
| Number format validation | Regex on the answer string | `double.TryParse` | Handles integers, decimals, scientific notation correctly |

**Key insight:** The rules engine numeric comparison is already solved. `RuleUtils.Compare` calls `decimal.TryParse` on both operands before comparing, so `"9"` vs `"10"` compares as `9 < 10` correctly. No backend rules engine changes are needed.

## Common Pitfalls

### Pitfall 1: el-input-number emits undefined on clear

**What goes wrong:** When the user clears the number field, `el-input-number` emits `undefined` (not `null`, not `0`). If `formData[id]` becomes `undefined`, the `transformFormDataForAPI` serializer sends `undefined` which JSON.stringify drops entirely, causing the answer to disappear from the payload.

**Why it happens:** Element Plus `el-input-number` clears to `undefined` by design when no value is present.

**How to avoid:** The `v-model` binding handles this automatically — Vue's reactivity will store `undefined` in `formData[id]`. The `transformFormDataForAPI` else-branch sends `formData.value[question.id]` directly. Add a null-coalesce: `answer: formData.value[question.id] ?? null` in the answer object construction for number types, or handle it in the `@change` handler.

**Warning signs:** Backend receives a response object with no `answer` field for a number question.

### Pitfall 2: Missing number branch in validateForm

**What goes wrong:** `validateForm` in `dynamicForm.vue` has explicit branches for `rating`, `linear_scale`, `short_answer_grid`, etc. Without a `number` branch, required number fields fall through to the generic `else` branch which checks `!formData.value[question.id]` — this evaluates `0` as falsy, incorrectly marking `0` as invalid.

**Why it happens:** JavaScript's falsy evaluation treats `0` as false.

**How to avoid:** Add an explicit `number` branch: `value !== null && value !== undefined`. This matches the pattern already used in `PreviewContent.vue` line 1447.

**Warning signs:** Submitting `0` for a required number field triggers a validation error.

### Pitfall 3: QuestionEditor type-switch resets min/max

**What goes wrong:** `QuestionEditor.vue` resets `min=1, max=5` (linear_scale defaults) when the question type changes. If a user switches to `number` type, these defaults get saved into the question structure. The `el-input-number` in `dynamicForm.vue` would then enforce `min=1, max=5` unintentionally.

**Why it happens:** The type-change handler applies linear_scale defaults broadly.

**How to avoid:** When switching to `number` type, clear `min` and `max` (set to `undefined`/`null`). Do not pass `:min` and `:max` props to the `el-input-number` in `dynamicForm.vue` unless the question explicitly has them configured (v2 scope).

**Warning signs:** Number field rejects values outside 1–5 range.

### Pitfall 4: QuestionnaireAnswerParser falls through to default

**What goes wrong:** `FormatAnswerWithConfig` switch has no `number` case. It falls through to `default: return answer?.ToString() ?? "No answer"`. This is functionally correct but semantically wrong — the change log shows raw numeric strings without any type-aware formatting.

**Why it happens:** The parser was written before `number` type existed.

**How to avoid:** Add `case "number": return answer?.ToString() ?? "No answer";` explicitly. This is a one-liner and makes the intent clear.

**Warning signs:** Change log entries for number questions look identical to short_answer entries (acceptable but not ideal).

### Pitfall 5: Answer stored as string in rules engine

**What goes wrong:** `ComponentDataService.GetQuestionnaireDataAsync` stores all non-grid answers as strings via `responseObj["answer"]?.ToString()` (line 315). A number answer `42` becomes the string `"42"` in the rules engine input.

**Why it does NOT matter:** `RuleUtils.Compare` calls `decimal.TryParse` on both operands before comparing. So `"9"` vs `"10"` correctly evaluates as `9 < 10`. This is already verified in the codebase.

**Warning signs (if this assumption is wrong):** Conditions like `answer > 9` fail for values >= 10 (multi-digit). Test with 9 vs 10 specifically.

## Code Examples

### Verified: RuleUtils.Compare already does numeric coercion

```csharp
// Source: RuleUtils.cs lines 345-360
public static int Compare(object? left, object? right)
{
    if (left == null && right == null) return 0;
    if (left == null) return -1;
    if (right == null) return 1;

    var leftStr = left.ToString();
    var rightStr = right.ToString();

    // Numeric coercion happens here — "9" and "10" parse to 9 and 10
    if (decimal.TryParse(leftStr, out var leftNum) && decimal.TryParse(rightStr, out var rightNum))
    {
        return leftNum.CompareTo(rightNum);
    }

    return string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase);
}
```

### Verified: PreviewContent.vue already handles number correctly

```html
<!-- Source: PreviewContent.vue lines 357-365 -->
<el-input-number
    v-else-if="item.type === 'number'"
    v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
    :placeholder="item.placeholder || 'Enter number'"
    :min="item.min"
    :max="item.max"
    :step="item.step || 1"
    class="preview-number"
/>
```

```typescript
// Source: PreviewContent.vue ~line 1078
case 'number':
    newPreviewData[key] = null;  // initialized to null, not 0
    break;
```

```typescript
// Source: PreviewContent.vue ~line 1446
case 'number':
    isFieldValid = value !== null && value !== undefined && value !== '';
    errorMessage = 'Please enter a number';
    break;
```

### Verified: conditionEnum.ts already maps number to numericOperators

```typescript
// Source: conditionEnum.ts lines 30-46
export const questionTypeInputMap: Record<string, ValueInputType> = {
    // ...
    number: 'number',
    // ...
};

export const questionTypeOperatorMap: Record<string, OperatorOption[]> = {
    // ...
    number: numericOperators,
    // ...
};
```

### Verified: applyAnswers pattern for numeric types

```typescript
// Source: dynamicForm.vue ~line 869 — existing pattern for linear_scale/rating
} else if (ans.type === 'linear_scale' || ans.type === 'rating') {
    const numValue = Number(ans.answer);
    formData.value[ans.questionId] = isNaN(numValue) ? 0 : numValue;
}
// For 'number', use null instead of 0 as the fallback:
} else if (ans.type === 'number') {
    const numValue = Number(ans.answer);
    formData.value[ans.questionId] = isNaN(numValue) ? null : numValue;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| No number type in editor | Add to questionTypes array | This phase | Users can select Number in question editor |
| Text input for numbers | el-input-number | This phase | Numeric-only input, proper null on clear |
| No backend type validation | double.TryParse check | This phase | Non-numeric values rejected at API layer |

**Already implemented (no change needed):**
- `PreviewContent.vue`: Full number support including `el-input-number`, null init, required validation
- `conditionEnum.ts`: `number` mapped to `numericOperators` and `'number'` input type
- `RuleUtils.Compare`: Numeric coercion via `decimal.TryParse` — string `"9"` vs `"10"` compares correctly as numbers

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `el-input-number` emits `undefined` (not `null`) on clear in the Element Plus version used by this project | Pitfall 1, Pattern 2 | If it emits `null`, the null-coalesce in transformFormDataForAPI is still safe — no functional impact |
| A2 | `QuestionEditor.vue` resets min/max to linear_scale defaults on type change | Pitfall 3 | If it does not reset, no action needed — but worth verifying during implementation |
| A3 | The `mdi:numeric` icon is available in the project's icon set | Pattern 1 | If not available, use any other numeric icon already used in the codebase (e.g., `mdi:numeric-1-box-outline`) |

**If this table is empty:** All claims in this research were verified or cited — no user confirmation needed.

## Open Questions

1. **Does QuestionEditor.vue reset min/max on type change?**
   - What we know: The pitfall research flagged this as a risk; the file was not read in this session.
   - What's unclear: Whether switching to `number` type would inherit `min=1, max=5` from linear_scale.
   - Recommendation: Read `QuestionEditor.vue` during implementation and add a `number` case to the type-change handler that clears min/max if present.

2. **Does customer-overview.vue need a number display branch?**
   - What we know: The pitfall research flagged this as a moderate risk (Pitfall 5 in PITFALLS.md).
   - What's unclear: Whether the customer overview falls through to a generic text display (acceptable) or breaks entirely.
   - Recommendation: Check during implementation. If it falls through to text display, it is acceptable for this phase (v2 scope for formatting).

## Environment Availability

Step 2.6: SKIPPED — this phase is purely code edits to existing files with no external tool, service, or CLI dependencies beyond the project's existing build chain.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | No automated test framework detected in project |
| Config file | none found |
| Quick run command | `dotnet build` (backend compile check) |
| Full suite command | `dotnet build && pnpm type:check` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FREG-01 | "Number" appears in question type dropdown | manual | — | N/A |
| FREG-02 | Number questions render as el-input-number in form fill | manual | — | N/A |
| FREG-03 | Clearing number field results in null (not 0 or undefined error) | manual | — | N/A |
| BVAL-01 | Non-numeric value rejected by backend with validation error | manual (API call) | — | N/A |
| BVAL-02 | QuestionnaireAnswerParser has explicit number case | compile check | `dotnet build` | ❌ Wave 0 |
| BVAL-03 | Numeric comparison: 9 < 10 (not string comparison) | manual (condition rule test) | — | N/A |

### Sampling Rate

- **Per task commit:** `dotnet build` (backend) + `pnpm type:check` (frontend)
- **Per wave merge:** same
- **Phase gate:** All manual checks pass + build green before `/gsd-verify-work`

### Wave 0 Gaps

- No automated test files to create — project has no test framework configured. All validation is manual + compile-time.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | yes | `double.TryParse` in `QuestionnaireAnswerService.cs` |
| V6 Cryptography | no | — |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Submitting non-numeric string as number answer | Tampering | `double.TryParse` validation before persist (BVAL-01) |
| Submitting extremely large number to cause overflow | Tampering | `double` range is ±1.7×10^308 — sufficient; no additional guard needed |

## Sources

### Primary (HIGH confidence — verified by direct code inspection)

- `createQuestion.vue` lines 411–478 — confirmed `number` is absent from `questionTypes` array
- `dynamicForm.vue` lines 89–534 — confirmed no `el-input-number` branch exists
- `dynamicForm.vue` lines 1620–1634 — confirmed `linear_scale` and `rating` init patterns
- `dynamicForm.vue` lines 869–872 — confirmed `applyAnswers` numeric pattern for linear_scale/rating
- `dynamicForm.vue` lines 1361–1373 — confirmed generic `else` branch in `transformFormDataForAPI`
- `QuestionnaireAnswerService.cs` — confirmed no type-specific validation exists
- `QuestionnaireAnswerParser.cs` lines 283–316 — confirmed no `number` case in switch
- `RuleUtils.cs` lines 345–360 — confirmed `decimal.TryParse` numeric coercion in `Compare`
- `ComponentDataService.cs` lines 313–316 — confirmed answers stored as strings via `.ToString()`
- `PreviewContent.vue` lines 357–365, 1078–1080, 1446–1449 — confirmed full number support already present
- `conditionEnum.ts` lines 30–46, 166–182 — confirmed `number` mapped to `numericOperators`

### Secondary (MEDIUM confidence)

- `.planning/research/ARCHITECTURE.md` — prior research identifying 3-file change scope
- `.planning/research/SUMMARY.md` — prior research confirming ~70% implementation
- `.planning/research/PITFALLS.md` — prior research identifying pitfall categories

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — verified by direct code inspection; no new packages
- Architecture: HIGH — all 4 files identified and change locations pinpointed to specific line numbers
- Pitfalls: HIGH — pitfalls verified against actual code (e.g., RuleUtils.Compare confirmed safe; applyAnswers pattern confirmed)

**Research date:** 2026-05-25
**Valid until:** 2026-06-25 (stable codebase, 30-day window)

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FREG-01 | User can select Number type when creating a question in questionnaire editor | Add `{ id: 'number', name: 'Number', icon: 'mdi:numeric' }` to `questionTypes` array in `createQuestion.vue` ~line 477 |
| FREG-02 | Number type renders with `el-input-number` in dynamicForm.vue for form filling | Add `v-else-if="question.type === 'number'"` branch with `<el-input-number>` in `dynamicForm.vue` template |
| FREG-03 | Number field initializes to null (not 0) and handles undefined on clear gracefully | Add `number` init branch (null) in `dynamicForm.vue` ~line 1634; add `number` branch in `applyAnswers` ~line 872; null-coalesce in `transformFormDataForAPI` |
| BVAL-01 | Backend validates Number-type answers are numeric via `double.TryParse` on submission | Add `ValidateNumberAnswers` call in `QuestionnaireAnswerService.SaveAnswerAsync` before persist |
| BVAL-02 | QuestionnaireAnswerParser has explicit `number` case (not falling through to default) | Add `case "number":` in `FormatAnswerWithConfig` switch in `QuestionnaireAnswerParser.cs` ~line 311 |
| BVAL-03 | Rules engine correctly performs numeric comparison (not string comparison) for Number fields | Already implemented — `RuleUtils.Compare` uses `decimal.TryParse`; `ComponentDataService` stores answers as strings but Compare handles coercion. No change needed. |
</phase_requirements>
