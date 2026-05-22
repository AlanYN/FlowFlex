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
| `checkboxes` | `"ftl,ltl"` (comma-separated values, auto-expanded from array) |
| `dropdown` | `"net_30"` (single option value) |
| `date` | `"2026-05-01"` (date string) |
| `file_upload` | File reference string |
| `short_answer_grid` | Nested object — access individual cells via `.<rowId>.<columnId>` |
| `multiple_choice_grid` | Nested object — access individual cells via `.<rowId>.<columnId>` |
| `checkbox_grid` | Nested object — access individual cells via `.<rowId>.<columnId>` |

## Output Format

When listing variables for a user, produce a markdown table grouped by section:

```markdown
## Section Name

| Field | Placeholder |
|-------|-------------|
| Human Label | `{{questionnaireAnswerByQuestionId.<questionId>}}` |
```

For grid-type questions, list each cell individually:

```markdown
| Human Label (Row → Column) | `{{questionnaireAnswerByQuestionId.<questionId>.<rowId>.<columnId>}}` |
```

## Grid Question Variable Extraction

Grid-type questions (`short_answer_grid`, `multiple_choice_grid`, `checkbox_grid`) have `rows` and `columns` arrays in their JSON definition. Each cell is accessed via a three-level path:

```
{{questionnaireAnswerByQuestionId.<questionId>.<rowId>.<columnId>}}
```

### How to Extract Grid Variables

Given a grid question JSON:

```json
{
  "id": "2050329392391000074",
  "type": "short_answer_grid",
  "title": "Person of Contact",
  "rows": [
    {"id": "2050329392391000069", "label": "Primary Contact"},
    {"id": "2050329392391000070", "label": "AP Contact"}
  ],
  "columns": [
    {"id": "2050329392391000071", "label": "Contact Name"},
    {"id": "2050329392391000072", "label": "Email Address"},
    {"id": "2050329392391000073", "label": "Phone Number"}
  ]
}
```

Extract variables by combining each row × column:

| Field | Placeholder |
|-------|-------------|
| Primary Contact → Contact Name | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000069.2050329392391000071}}` |
| Primary Contact → Email Address | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000069.2050329392391000072}}` |
| Primary Contact → Phone Number | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000069.2050329392391000073}}` |
| AP Contact → Contact Name | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000070.2050329392391000071}}` |
| AP Contact → Email Address | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000070.2050329392391000072}}` |
| AP Contact → Phone Number | `{{questionnaireAnswerByQuestionId.2050329392391000074.2050329392391000070.2050329392391000073}}` |

### Grid Extraction Rules for AI

1. Take the question's `id` → `<questionId>`
2. For each item in `rows[]`, take its `id` → `<rowId>` and `label` → row display name
3. For each item in `columns[]`, take its `id` → `<columnId>` and `label` → column display name
4. Construct: `{{questionnaireAnswerByQuestionId.<questionId>.<rowId>.<columnId>}}`
5. Display as: `Row Label → Column Label`

### Storage Format (Backend)

Grid answers are stored in `responseText` with keys in format `questionId_rowId_columnId`:

```json
{
  "2050329392391000074_2050329392391000069_2050329392391000071": "John Smith",
  "2050329392391000074_2050329392391000069_2050329392391000072": "john@example.com",
  "2050329392391000074_2050329392391000069_2050329392391000073": "123-456-7890"
}
```

`ComponentDataService` parses this into a nested object `{rowId: {columnId: value}}`, and `TemplateVariableResolver` resolves the dot-path to the final cell value.

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
4. For grid-type questions (`short_answer_grid`, `multiple_choice_grid`, `checkbox_grid`), expand rows × columns and list each cell as `{{questionnaireAnswerByQuestionId.<questionId>.<rowId>.<columnId>}}`
5. Include the "Other Available Variables" section at the end
6. Optionally provide a JSON body usage example with the most common fields

## Template Resolution Behavior

### Array Values (checkboxes, multi-select)
When a question answer is stored as an array (e.g. `["wise"]` or `["ftl","ltl"]`), the resolver automatically expands it to a comma-separated string:
- `["wise"]` → `wise`
- `["ftl","ltl","small_parcel"]` → `ftl,ltl,small_parcel`

### Flat Key Priority
If the context contains both a flat key like `"questionnaireAnswerByQuestionId.123"` (from AI lookup) and a nested path `questionnaireAnswerByQuestionId` → `123` (from raw questionnaire answers), the flat key takes priority. This ensures AI-matched/transformed values are used when available.
