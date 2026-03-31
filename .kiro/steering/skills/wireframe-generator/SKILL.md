---
name: wireframe-generator
description: Generate text-based wireframe descriptions and ASCII layout sketches for key pages based on user stories. Use when UX needs to document page structure, key elements, and layout before visual design begins.
inclusion: manual
---

# Wireframe Generator

Produce text-based wireframe descriptions and ASCII layout sketches for key pages, derived from user stories and BA interaction notes.

## When to Use

- UX is designing each key page corresponding to a user story
- Page structure needs to be documented before visual design
- Communicating layout intent to Dev without design tools

## Input

```
userStory: "US-001: Reset Password via Email"
interactionNotes: "Entry point is the login page. User enters email, receives link, sets new password."
pageType: "form"
```

## Output Format

```markdown
### Wireframe: {Page Name} (US-{NNN})

**Purpose**: {One sentence describing what this page does}

**Layout**:
```
+------------------------------------------+
| [Logo]              [Back to Login link]  |
+------------------------------------------+
|                                          |
|   Reset Your Password                    |
|   ─────────────────────────────────      |
|                                          |
|   Email address                          |
|   [________________________]             |
|                                          |
|   [    Send Reset Link    ]  ← primary   |
|                                          |
|   ℹ We'll send a link to your inbox      |
|                                          |
+------------------------------------------+
```

**Key Elements**:
| Element | Type | Behavior |
|---------|------|----------|
| Email input | Text field | Required, validates email format on blur |
| Send Reset Link | Primary button | Disabled until valid email entered |
| Back to Login | Text link | Navigates to login page |
| Info message | Static text | Always visible below button |

**States**:
- Default: empty form, button disabled
- Valid input: button enabled
- Loading: button shows spinner, disabled
- Success: replace form with "Check your inbox" confirmation
- Error: inline error below input field
```

## Layout Conventions

Use ASCII box-drawing for layout sketches:
- `+--+` for container borders
- `[Text]` for buttons
- `[________]` for input fields
- `[ ]` for checkboxes, `( )` for radio buttons
- `▼` for dropdowns
- `←` / `→` annotations for behavior notes

## Page Type Templates

| Page Type | Key Sections |
|-----------|-------------|
| `form` | Header, form fields, submit action, feedback states |
| `list` | Header + filters, list items, empty state, pagination |
| `detail` | Header, content sections, action buttons, related items |
| `dashboard` | Nav, metric cards, charts area, recent activity |
| `modal` | Title, content, confirm/cancel actions |

## Execution Rules

1. Every wireframe must cover: layout, key elements table, and all UI states
2. States to always include: default, loading, success, error, empty (where applicable)
3. Each element in the layout must appear in the key elements table
4. Do not specify colors, fonts, or visual styling — that belongs in design tokens
5. One wireframe per distinct page or major modal

## Failure Handling

If the user story is too vague to determine page structure, list the ambiguities as UXAMB items and request clarification before generating the wireframe.
