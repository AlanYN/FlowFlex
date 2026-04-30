---
name: task-breakdown
description: Decompose user stories and technical requirements into structured, independently deliverable development tasks. Use when Dev needs to convert a confirmed Dev Spec into an actionable task list with priority, complexity, and dependency mapping.
inclusion: manual
---

# Task Breakdown

Decompose user stories and architecture decisions into structured development tasks — each independently deliverable, sized at M or smaller, with explicit priority and dependencies.

## When to Use

- Dev Spec architecture and tech stack are confirmed
- User stories from BA Spec are confirmed and ambiguity-free
- Need to produce an actionable task list for sprint planning or implementation

## Input

```
userStories:
  - "US-001: User can reset password via email (P0)"
  - "US-002: User can update profile information (P1)"
architecture: "React + Node.js/Fastify + PostgreSQL + Redis"
techStack: "TypeScript, Prisma ORM, JWT auth, SendGrid for email"
```

## Output Format

```markdown
## Task List: {Project Name}

### TASK-001: Set up password reset token schema
- **Description**: Add `password_reset_tokens` table via Prisma migration. Fields: id, user_id (FK), token (hashed), expires_at, used_at. Index on token for lookup performance.
- **Priority**: P0
- **Complexity**: S (< 1 day)
- **Related requirements**: US-001, AC-001-1
- **Dependencies**: none

### TASK-002: Implement POST /auth/forgot-password endpoint
- **Description**: Accept email, look up user, generate secure random token, hash and store in DB, send reset email via SendGrid. Return generic 200 regardless of whether email exists (prevent user enumeration).
- **Priority**: P0
- **Complexity**: M (2–3 days)
- **Related requirements**: US-001, AC-001-1, AC-001-4
- **Dependencies**: TASK-001

### TASK-003: Implement POST /auth/reset-password endpoint
- **Description**: Accept token + new password. Validate token exists, not expired (24h), not used. Hash new password, update user record, mark token as used. Return 200 on success, 400 on invalid/expired token.
- **Priority**: P0
- **Complexity**: M (2–3 days)
- **Related requirements**: US-001, AC-001-2, AC-001-3
- **Dependencies**: TASK-001

### TASK-004: Build reset password UI flow (request + confirmation pages)
- **Description**: Two pages: (1) email input form with loading/success/error states per wireframe WF-003; (2) new password form with confirmation field and policy validation. Use design tokens from UX Spec.
- **Priority**: P0
- **Complexity**: M (2–3 days)
- **Related requirements**: US-001, AC-001-1, AC-001-2
- **Dependencies**: TASK-002, TASK-003
```

## Complexity Scale

| Size | Time Estimate | Rule |
|------|--------------|------|
| XS | < 4 hours | Config change, copy update, trivial fix |
| S | < 1 day | Single endpoint, simple component, DB migration |
| M | 2–3 days | Feature slice with frontend + backend + tests |
| L | 1 week | Complex feature, significant integration work |
| XL | > 1 week | Must be split — do not accept XL tasks |

## Priority Levels

| Level | Meaning |
|-------|---------|
| P0 | Blocks launch — must be done first |
| P1 | Important — do after all P0s are complete |
| P2 | Nice to have — do if time permits |
| P3 | Deferred — schedule for next iteration |

## Decomposition Rules

1. Every task must map to at least one user story AC — no orphan tasks
2. Maximum complexity is M — split any L/XL task before adding to the list
3. P0 tasks must be listed first and form a clear critical path
4. Every task with a dependency must explicitly list it
5. Each task description must include enough detail for a developer to start without asking questions
6. Infrastructure/setup tasks (DB schema, auth middleware, CI pipeline) are valid tasks — list them first as they unblock feature tasks
7. Do not create tasks for work already covered by a library or framework out of the box
