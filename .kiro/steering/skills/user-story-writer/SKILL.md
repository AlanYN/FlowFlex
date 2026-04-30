---
name: user-story-writer
description: Generate structured user stories with acceptance criteria from a feature description or PM Spec scope item. Use when BA needs to convert functional requirements into standard "As a / I want / So that" format with testable GIVEN/WHEN/THEN acceptance criteria.
inclusion: manual
---

# User Story Writer

Convert feature descriptions or PM Spec scope items into structured user stories following the INVEST principle, each with testable acceptance criteria in GIVEN/WHEN/THEN format.

## When to Use

- BA is breaking down each In Scope item from PM Spec into user stories
- A feature needs testable acceptance criteria before handoff to UX or Dev
- Existing user stories lack clear acceptance criteria

## Input

```
featureDescription: "Users can reset their password via email"
userRole: "registered user"
priority: "P0"
relatedGoal: "BG-01"
```

## Output Format

```markdown
### US-{NNN}: {Story Title}

**As a** {user role}
**I want to** {action or capability}
**So that** {value or outcome}

**Priority**: P0 / P1 / P2
**Related goal**: {businessGoal ID}

**Acceptance Criteria**:
- **AC-{NNN}-1**: GIVEN {precondition} WHEN {action} THEN {expected result}
- **AC-{NNN}-2**: GIVEN {precondition} WHEN {action} THEN {expected result}
- **AC-{NNN}-3**: GIVEN {precondition} WHEN {action} THEN {expected result}
```

## INVEST Checklist

Every user story must satisfy:

| Principle | Check |
|-----------|-------|
| **I**ndependent | Story can be developed without depending on another story |
| **N**egotiable | Details can be discussed and adjusted with stakeholders |
| **V**aluable | Delivers clear value to the user or business |
| **E**stimable | Dev can estimate the effort required |
| **S**mall | Can be completed within one iteration |
| **T**estable | Acceptance criteria can be verified |

## Acceptance Criteria Rules

- Every AC must follow GIVEN / WHEN / THEN format
- GIVEN = precondition (system state before the action)
- WHEN = the user action or system event
- THEN = the observable, verifiable outcome
- Each story needs at minimum: 1 happy path AC + 1 error/edge case AC
- No vague language in THEN clauses ("displays correctly" is not acceptable — specify what is displayed)

## Execution Rules

1. One user story per distinct user goal — do not bundle multiple goals into one story
2. If a story is too large to complete in one iteration, split it into smaller stories
3. Inherit priority from the PM Spec MoSCoW classification
4. Every story must trace back to at least one `businessGoal` ID
5. Write ACs only for confirmed requirements — do not invent scope

## Example

```markdown
### US-001: Reset Password via Email

**As a** registered user
**I want to** reset my password using my registered email address
**So that** I can regain access to my account when I forget my password

**Priority**: P0
**Related goal**: BG-01 (user retention)

**Acceptance Criteria**:
- **AC-001-1**: GIVEN I am on the login page WHEN I click "Forgot password" and enter my registered email THEN the system sends a password reset link to that email within 60 seconds
- **AC-001-2**: GIVEN I click a valid reset link WHEN I enter and confirm a new password meeting the policy THEN my password is updated and I am redirected to the login page
- **AC-001-3**: GIVEN I click a reset link that is older than 24 hours WHEN the page loads THEN the system shows "This link has expired" and offers to send a new one
- **AC-001-4**: GIVEN I enter an email address not registered in the system WHEN I submit the form THEN the system shows "If this email is registered, you will receive a reset link" (no user enumeration)
```
