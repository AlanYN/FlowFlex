---
phase: "01"
plan: "02"
subsystem: backend
tags: [validation, questionnaire, number-type, parser]
dependency_graph:
  requires: []
  provides: [backend-number-validation, parser-number-case]
  affects: [QuestionnaireAnswerService, QuestionnaireAnswerParser]
tech_stack:
  added: []
  patterns: [double.TryParse with InvariantCulture, System.Text.Json JsonDocument]
key_files:
  created: []
  modified:
    - packages/flowFlex-backend/Application/Services/OW/QuestionnaireAnswerService.cs
    - packages/flowFlex-backend/Application/Services/OW/ChangeLog/QuestionnaireAnswerParser.cs
decisions:
  - "Use double.TryParse with NumberStyles.Any and CultureInfo.InvariantCulture for locale-safe numeric validation"
  - "Validate via JsonDocument parsing of the raw JSONB payload rather than FluentValidation (dynamic payload, not typed DTO)"
  - "BVAL-03 required no code change — RuleUtils.Compare already uses decimal.TryParse"
metrics:
  duration: "~10 minutes"
  completed: "2026-05-25"
  tasks_completed: 2
  tasks_total: 2
---

# Phase 01 Plan 02: Backend Number Validation and Parser Summary

Backend numeric validation for Number-type questionnaire answers using JsonDocument + double.TryParse with InvariantCulture, plus an explicit parser case for the change log.

## What Was Built

**Task 1 — ValidateNumberAnswers in QuestionnaireAnswerService.cs (BVAL-01)**

Added a `private static void ValidateNumberAnswers(string answerJson)` helper that:
- Parses the raw JSONB payload with `System.Text.Json.JsonDocument`
- Iterates the `responses` array looking for entries with `type == "number"`
- Calls `double.TryParse` with `NumberStyles.Any` and `CultureInfo.InvariantCulture` on each non-null answer
- Throws `ArgumentException` with the questionId and received value if parsing fails
- Accepts `null` answers (unanswered optional fields) and valid numeric strings

The call site is inserted in `SaveAnswerAsync` immediately after `formattedJson` is set and before the `if (isUpdate)` branch, ensuring validation fires for both create and update paths before any database write.

**Task 2 — Explicit number case in QuestionnaireAnswerParser.cs (BVAL-02, BVAL-03)**

Added `case "number": return answer?.ToString() ?? "No answer";` before the `default:` case in the `FormatAnswerWithConfig` switch statement. Behavior is identical to the default fallthrough, but the explicit case documents that `number` is a known type and prevents future default-case changes from accidentally affecting number formatting.

BVAL-03 confirmed via grep: `RuleUtils.cs` already contains `decimal.TryParse` — numeric comparison in the rules engine works correctly without any code change.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 | 478aa9aa | feat(01-02): add ValidateNumberAnswers to QuestionnaireAnswerService |
| Task 2 | 6113d7fd | feat(01-02): add explicit number case in QuestionnaireAnswerParser |

## Acceptance Criteria Verification

- BVAL-01: `ValidateNumberAnswers` appears 2 times in QuestionnaireAnswerService.cs (call site + definition) — confirmed
- BVAL-02: `case "number":` appears 1 time in QuestionnaireAnswerParser.cs before `default:` — confirmed
- BVAL-03: `decimal.TryParse` found in RuleUtils.cs — no code change needed, confirmed
- `dotnet build` exits 0 with no new errors — confirmed (3529 pre-existing warnings, 0 errors)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

No new security surface introduced. All changes use .NET 8 BCL (System.Text.Json, double.TryParse). Threat mitigations T-01-BE-01 and T-01-BE-03 from the plan's threat model are fully implemented.

## Self-Check: PASSED

- packages/flowFlex-backend/Application/Services/OW/QuestionnaireAnswerService.cs — FOUND
- packages/flowFlex-backend/Application/Services/OW/ChangeLog/QuestionnaireAnswerParser.cs — FOUND
- Commit 478aa9aa — FOUND
- Commit 6113d7fd — FOUND
