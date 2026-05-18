---
name: test-case-generator
description: Generate structured test cases from BA acceptance criteria and Dev risk register. Use when QA needs to convert every AC into at least one verifiable test case with preconditions, steps, and expected results. Covers happy path, error cases, and edge cases for high-risk items.
inclusion: manual
---

# Test Case Generator

Convert BA acceptance criteria and Dev risk items into structured, verifiable test cases — covering happy path, error handling, and edge cases.

## When to Use

- QA is converting each BA acceptance criterion into executable test cases
- High-risk items from Dev Spec need additional boundary and edge case coverage
- Building the test suite before or during implementation

## Input

```
acceptanceCriteria:
  - "AC-001-1: GIVEN I am on the login page WHEN I click Forgot password and enter my registered email THEN the system sends a reset link within 60 seconds"
  - "AC-001-3: GIVEN I click a reset link older than 24 hours WHEN the page loads THEN the system shows 'This link has expired'"
risks:
  - "RISK-001: Token not invalidated after use — user could reuse a reset link"
```

## Output Format

```markdown
### TC-001: Password reset email sent successfully

- **Related requirement**: AC-001-1
- **Preconditions**: User account exists with email user@example.com; user is on the login page
- **Steps**:
  1. Click "Forgot password" link
  2. Enter "user@example.com" in the email field
  3. Click "Send Reset Link"
- **Expected result**: Success message displayed; reset email arrives in inbox within 60 seconds; email contains a valid reset link
- **Status**: pending

---

### TC-002: Expired reset link shows error

- **Related requirement**: AC-001-3
- **Preconditions**: A password reset link was generated more than 24 hours ago and has not been used
- **Steps**:
  1. Open the expired reset link in a browser
- **Expected result**: Page displays "This link has expired" message with an option to request a new reset link; no password change is possible
- **Status**: pending

---

### TC-003: Used reset link cannot be reused (security)

- **Related requirement**: AC-001-2 | **Risk**: RISK-001
- **Preconditions**: User has successfully reset their password using a valid reset link
- **Steps**:
  1. Copy the reset link URL before using it
  2. Complete the password reset successfully
  3. Navigate back to the copied reset link URL
- **Expected result**: System shows "This link has already been used" or equivalent error; password cannot be changed again with the same link
- **Status**: pending
```

## Coverage Requirements

| Source | Minimum Coverage |
|--------|-----------------|
| Each AC | At least 1 test case (happy path) |
| Each AC with error condition | At least 1 additional error test case |
| Each 🔴 Critical risk | At least 2 test cases (trigger + boundary) |
| Each 🟡 Medium risk | At least 1 test case |

## Test Case Types

| Type | When to Write |
|------|--------------|
| **Happy path** | Every AC — the normal successful flow |
| **Error handling** | ACs with explicit error conditions (expired, invalid, missing) |
| **Boundary** | Numeric limits, time thresholds, max lengths |
| **Security** | Auth bypass, token reuse, privilege escalation, enumeration |
| **Edge case** | Empty states, concurrent actions, network interruption |

## Execution Rules

1. Every AC must have at least one test case — no untested acceptance criteria
2. Expected results must be specific and verifiable — "displays correctly" is not acceptable
3. Preconditions must fully describe the system state before step 1
4. Steps must be atomic — one action per step
5. Security-related risks always generate test cases regardless of probability score
6. Test case IDs are sequential: TC-001, TC-002, etc. — never reuse an ID
