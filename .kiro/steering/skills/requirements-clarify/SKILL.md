---
name: requirements-clarify
description: Identify and resolve ambiguous requirements in a PM Spec or user description. Use when BA encounters vague quantities, incomplete conditions, undefined terms, missing boundaries, or unclear subjects. Outputs a structured ambiguity list with suggested definitions for user confirmation.
inclusion: manual
---

# Requirements Clarify

Detect ambiguous requirements and convert them into precise, testable business definitions — with user confirmation before writing them into specs.

## When to Use

- BA encounters vague language in PM Spec or user input
- A requirement cannot be turned into a testable acceptance criterion as-is
- Multiple interpretations of the same requirement are possible

## Ambiguity Types

| Type | Example | Resolution |
|------|---------|------------|
| **Vague quantity** | "support many users" | Ask for a specific number: "support 1,000 concurrent users" |
| **Incomplete condition** | "user can edit the document" | Add precondition: "logged-in users can edit documents they created" |
| **Term ambiguity** | "real-time sync" | Define precisely: "all online users see updates within 500ms" |
| **Undefined boundary** | "support multiple formats" | List formats: "supports .docx, .pdf, .md" |
| **Unclear subject** | "can be shared" | Clarify actor: "document owner can generate a shareable link" |

## Output Format

For each ambiguity found, output one block:

```markdown
### AMB-{NNN}

- **Original**: {exact quote from source}
- **Ambiguity type**: {Vague quantity / Incomplete condition / Term ambiguity / Undefined boundary / Unclear subject}
- **Problem**: {what specifically is unclear}
- **Suggested definition**: {BA's recommended precise definition}
- **Status**: pending confirmation
```

Then present all AMB items together and ask the user to confirm or revise each one before proceeding.

## Execution Rules

1. Scan the full input for all ambiguities before presenting — batch them, don't interrupt one by one
2. Every AMB item must include a suggested definition — never just flag without proposing a resolution
3. Only write confirmed definitions into user stories and flow diagrams
4. If a requirement is clear enough to write a testable AC, do not flag it as ambiguous
5. After user confirms all items, update their status to `confirmed` or `dismissed`

## Failure Handling

If the entire input is too vague to extract any specific requirements, ask the user to provide at least one concrete example of the desired behavior before proceeding.
