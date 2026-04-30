---
name: tech-stack-advisor
description: Recommend a technology stack based on project requirements, team constraints, and non-functional requirements. Use when Dev needs to make and justify technology choices for frontend, backend, database, and infrastructure with documented rationale and trade-offs.
inclusion: manual
---

# Tech Stack Advisor

Recommend a technology stack for a project based on its requirements, team context, and constraints — with explicit rationale and documented alternatives for each choice.

## When to Use

- Dev is starting architecture design and needs to select technologies
- Stakeholders need a justified tech stack decision before implementation
- Evaluating trade-offs between multiple viable options

## Input

```
projectType: "Web SaaS application with mobile support"
teamSize: 5
teamExpertise: "React, Node.js, some Python"
scalingRequirements: "1,000 concurrent users at launch, 10x growth in 12 months"
keyConstraints: "3-month deadline, limited DevOps capacity"
nonFunctionalRequirements:
  - "< 200ms API response time"
  - "99.9% uptime"
  - "GDPR compliance"
```

## Output Format

```markdown
## Tech Stack Recommendation: {Project Name}

### Summary
| Layer | Selected | Rationale Summary |
|-------|----------|-------------------|
| Frontend | React + TypeScript | Team expertise, ecosystem maturity |
| Backend | Node.js (Express/Fastify) | Shared language with frontend, team familiarity |
| Database | PostgreSQL | Relational data model, GDPR compliance features |
| Cache | Redis | Session management, API response caching |
| Infrastructure | Docker + Railway/Render | Low DevOps overhead, fast deployment |
| Auth | Auth.js / Clerk | Reduces implementation time, security best practices |

### Detailed Decisions

#### Frontend: React + TypeScript
- **Rationale**: Team has existing React expertise; TypeScript reduces runtime errors in a 3-month timeline
- **Alternative considered**: Vue 3 — comparable DX but lower team familiarity increases ramp-up risk
- **Trade-off**: Larger bundle size vs. Next.js SSR, acceptable for SaaS dashboard use case

#### Backend: Node.js + Fastify
- **Rationale**: Shared language reduces context switching; Fastify outperforms Express by ~2x for JSON throughput
- **Alternative considered**: Python/FastAPI — better for ML workloads but no current ML requirements
- **Trade-off**: Single-threaded model requires careful async handling for CPU-intensive tasks

#### Database: PostgreSQL
- **Rationale**: ACID compliance for transactional data; row-level security supports GDPR data isolation
- **Alternative considered**: MongoDB — flexible schema but weaker consistency guarantees for financial data
- **Trade-off**: Schema migrations require more planning than document stores

### Scaling Plan
| Phase | Users | Architecture Change |
|-------|-------|---------------------|
| Launch | ~1,000 | Single server, vertical scaling |
| 6 months | ~5,000 | Read replicas, Redis caching layer |
| 12 months | ~10,000 | Horizontal scaling, CDN, queue-based async jobs |
```

## Decision Criteria

| Factor | Weight | Notes |
|--------|--------|-------|
| Team expertise | High | Unfamiliar tech adds 20–40% time overhead |
| Timeline fit | High | Avoid technologies requiring >1 week ramp-up |
| Scaling headroom | Medium | Must support 10x without full rewrite |
| Community/ecosystem | Medium | Active ecosystem = faster problem resolution |
| Operational complexity | Medium | Match DevOps capacity of the team |
| License/cost | Low | Flag any non-OSS or usage-based cost surprises |

## Execution Rules

1. Every technology choice must include: rationale, one alternative considered, and the trade-off accepted
2. Flag any choice that conflicts with stated constraints (deadline, team size, expertise)
3. Include a scaling plan showing when architecture changes are needed
4. Do not recommend technologies the team has zero experience with unless the alternative is clearly worse
5. If requirements are contradictory (e.g. "no DevOps capacity" + "Kubernetes"), flag the conflict explicitly
