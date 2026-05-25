# Requirements: FlowFlex Question Number Type

**Defined:** 2026-05-25
**Core Value:** 问卷字段能通过 Number 类型约束用户只输入数字，确保数据质量。

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Frontend - Type Registration

- [ ] **FREG-01**: User can select Number type when creating a question in questionnaire editor
- [ ] **FREG-02**: Number type renders with `el-input-number` in dynamicForm.vue for form filling
- [ ] **FREG-03**: Number field initializes to null (not 0) and handles undefined on clear gracefully

### Backend - Validation

- [x] **BVAL-01**: Backend validates Number-type answers are numeric via `double.TryParse` on submission
- [x] **BVAL-02**: QuestionnaireAnswerParser has explicit `number` case (not falling through to default)
- [x] **BVAL-03**: Rules engine correctly performs numeric comparison (not string comparison) for Number fields

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Number Configuration

- **NCFG-01**: User can set min/max range constraints for Number fields
- **NCFG-02**: User can configure step size for Number fields
- **NCFG-03**: User can toggle between integer-only and decimal input

## Out of Scope

| Feature | Reason |
|---------|--------|
| Min/Max range validation | Keep scope minimal — only type constraint needed now |
| Integer/Decimal distinction | Adds config complexity; allow any number for now |
| Number formatting (locale, thousands separator) | Display concern, not input validation |
| Calculated fields / formulas | Different feature entirely; massive scope |
| Slider/Range input variant | System already has `linear_scale` for that |
| Data export format conversion | Only doing form input validation |
| Number-specific config editor component | No configurable properties in scope |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FREG-01 | Phase 1 | Pending |
| FREG-02 | Phase 1 | Pending |
| FREG-03 | Phase 1 | Pending |
| BVAL-01 | Phase 1 | Complete |
| BVAL-02 | Phase 1 | Complete |
| BVAL-03 | Phase 1 | Complete |

**Coverage:**
- v1 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-05-25*
*Last updated: 2026-05-25 after initial definition*
