# Roadmap: FlowFlex Question Number Type

## Overview

Add Number type to the FlowFlex questionnaire system. The type is already ~70% wired — rendering, conditions, and rules support it. This roadmap covers the remaining registration, form rendering, and backend validation in a single phase.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [ ] **Phase 1: Number Type Support** - Register Number type in editor, render numeric input, validate on backend

## Phase Details

### Phase 1: Number Type Support
**Goal**: Users can create Number-type questions and submit numeric answers with proper validation
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: FREG-01, FREG-02, FREG-03, BVAL-01, BVAL-02, BVAL-03
**Success Criteria** (what must be TRUE):
  1. User can select "Number" from the question type dropdown in the questionnaire editor
  2. Number-type questions render as `el-input-number` in the form-filling view (not a text input)
  3. Clearing a number field results in null (not 0 or undefined errors)
  4. Submitting a non-numeric value for a Number field is rejected by the backend with a validation error
  5. Condition rules on Number fields compare numerically (e.g., 9 < 10, not "9" > "10")
**Plans**: TBD

Plans:
- [ ] 01-01: Frontend type registration and rendering
- [ ] 01-02: Backend validation and parser

## Progress

**Execution Order:**
Phases execute in numeric order.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Number Type Support | 0/2 | Not started | - |
