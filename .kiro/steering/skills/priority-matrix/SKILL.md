---
name: priority-matrix
description: Classify a list of requirements using the MoSCoW method (Must/Should/Could/Won't). Use when PM needs to determine delivery scope and make trade-off decisions, especially when resources or time are constrained.
inclusion: manual
---

# Priority Matrix (MoSCoW)

Classify each requirement in a list into Must Have, Should Have, Could Have, or Won't Have — with a rationale for every decision.

## When to Use

- Requirements list is ready and PM needs to define delivery scope
- Resources or timeline are constrained and trade-offs must be made
- Aligning stakeholders on what's in vs. out of scope

## Input

```
requirementList:
  - User registration and login
  - WeChat one-click login
  - Product search
  - Product detail page
  - Shopping cart
  - Order management
  - Coupon system
  - Recommendation algorithm
  - Multi-language support
  - Dark mode

context: "MVP phase, 3-month deadline, team of 5"
```

## Output Format

```markdown
## MoSCoW Priority Matrix

### Must Have (P0) — Product cannot function without these
| Requirement | Rationale |
|-------------|-----------|
| User registration and login | Prerequisite for all features |
| Product search | Core user journey — can't transact without finding items |

### Should Have (P1) — Important but a workaround exists
| Requirement | Rationale |
|-------------|-----------|
| Shopping cart | Improves purchase flow; single-item direct buy is a temporary substitute |
| WeChat one-click login | Reduces friction; standard login is a viable fallback |

### Could Have (P2) — Nice to have, low impact if absent
| Requirement | Rationale |
|-------------|-----------|
| Coupon system | Marketing tool; manual discounts can substitute in MVP |
| Recommendation algorithm | Needs data volume not yet available in MVP |

### Won't Have (this release) — Explicitly deferred
| Requirement | Rationale |
|-------------|-----------|
| Multi-language support | Target market is domestic; internationalization not in scope |
| Dark mode | Visual preference only; no business value impact |
```

## Classification Criteria

| Category | Rule |
|----------|------|
| **Must Have** | Without it, the product cannot launch or operate. No workaround exists. |
| **Should Have** | Important and user-expected, but a temporary substitute exists. Omitting hurts UX but won't cause failure. |
| **Could Have** | Enhances experience. Low cost to omit. Include only if time permits. |
| **Won't Have** | Explicitly out of scope for this release. Not "never" — add to roadmap. |

## Common Mistakes to Avoid

- Must Have ≠ "everything we want" — it's the minimum viable set
- If >60% of items are Must Have, the criteria are too loose — re-evaluate
- Won't Have is not a rejection; document it in the roadmap

## Execution Rules

1. Classify every item in `requirementList` — no omissions
2. Every item must have a rationale — no classification without reasoning
3. Clarify any vague requirement before classifying it
4. Apply `context` (phase, team size, deadline) to calibrate Must vs. Should
5. Output all four categories even if one is empty
