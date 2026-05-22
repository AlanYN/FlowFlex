# Guide: Extracting Questionnaire Variables for Action Configuration

## Purpose

When configuring HTTP Actions in FlowFlex, users need to reference questionnaire answers as template variables. This guide explains how to extract question IDs from a questionnaire's JSON structure and map them to usable `{{placeholder}}` syntax.

## Background

FlowFlex's Action system supports template variables in HTTP API configurations (URL, Headers, Params, Body). Questionnaire answers from completed stages are injected into the Action execution context and can be referenced via:

- `{{questionnaireAnswerByQuestionId.<questionId>}}` — get answer by question ID (recommended)
- `{{questionnaireAnswerMap.<questionnaireId>.<questionId>}}` — get answer by questionnaire + question ID

## How It Works

1. When a Stage is completed, its questionnaire answers are saved to the database
2. When an Action is triggered, `ActionContextBuilder` collects answers from ALL completed stages
3. Answers are indexed by `questionId` (the `id` field of each question in the questionnaire JSON)
4. The `TemplateVariableResolver` replaces `{{path}}` placeholders with actual values at execution time

## Questionnaire JSON Structure

A questionnaire's structure JSON looks like:

```json
{
  "sections": [
    {
      "id": "<sectionId>",
      "name": "Section Name",
      "questions": [
        {
          "id": "<questionId>",        // ← THIS is what you use in the placeholder
          "type": "short_answer",       // question type
          "title": "Field Label",       // human-readable name
          "question": "Field Label",    // same as title usually
          "required": true,
          "options": [],                // for checkboxes/multiple_choice
          "rows": [],                   // for grid types
          "columns": []                 // for grid types
        }
      ]
    }
  ]
}
```

## Extraction Rules

Given a questionnaire JSON, for each question:

1. Take the `id` field → this is the `<questionId>`
2. Take the `title` or `question` field → this is the human-readable label
3. Construct the placeholder: `{{questionnaireAnswerByQuestionId.<questionId>}}`

## Question Types and Expected Values

| Type | Example Answer Value |
|------|---------------------|
| `short_answer` | `"ABC Company"` (plain string) |
| `paragraph` | `"Long text..."` (plain string) |
| `multiple_choice` | `"yes"` (single option value) |
| `checkboxes` | `"ftl,ltl"` or `["ftl","ltl"]` (selected values) |
| `date` | `"2026-05-01"` (date string) |
| `short_answer_grid` | Raw JSON object (rows × columns matrix) |

## Output Format

When listing variables for a user, produce a markdown table grouped by section:

```markdown
## Section Name

| Field | Placeholder |
|-------|-------------|
| Human Label | `{{questionnaireAnswerByQuestionId.<questionId>}}` |
```

## Other Available Context Variables

Always remind users these are also available:

| Variable | Description |
|----------|-------------|
| `{{CaseCode}}` | Case code identifier |
| `{{CaseName}}` | Case display name |
| `{{OnboardingId}}` | Onboarding ID |
| `{{WorkflowId}}` | Workflow ID |
| `{{StageId}}` | Current stage ID |
| `{{integrationToken}}` | OAuth2 integration token |
| `{{previousActionResult.data.<field>}}` | Previous action response field |
| `{{prev_<field>}}` | Shorthand for previous action data field |
| `{{<dynamicFieldCamelCase>}}` | Dynamic Field value (e.g. Company Name → companyName) |

## Multi-Stage Override Rule

If the same `questionId` appears in multiple completed stages, the answer from the **latest completed stage** (by `CompletionTime`) wins. The full history is available in `questionnaireAnswers` array if needed.

## Example Prompt for AI

When a user provides a questionnaire JSON and asks to list variables:

> "Here is my questionnaire JSON: [paste]. Please list all available Action placeholders."

The AI should:
1. Parse the `sections[].questions[]` array
2. Extract `id` and `title` from each question
3. Output a grouped table with `{{questionnaireAnswerByQuestionId.<id>}}` for each
4. Note any grid-type questions as "raw value" in v1
5. Include the "Other Available Variables" section at the end
6. Optionally provide a JSON body usage example with the most common fields
