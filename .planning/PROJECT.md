# FlowFlex Question Number Type

## What This Is

为 FlowFlex 问卷系统的 Question 配置增加 Number 类型选项。当前 Question Type 缺少数字类型，导致无法约束用户输入为数字。本次改动在前后端同时支持 Number 类型，前端限制只能输入数字，后端验证格式。

## Core Value

问卷字段能通过 Number 类型约束用户只输入数字，确保数据质量。

## Requirements

### Validated

- ✓ 问卷系统已有 Text/Date 等字段类型 — existing
- ✓ 前端问卷渲染和后端验证链路已建立 — existing
- ✓ Question Type 枚举机制已存在 — existing

### Active

- [ ] Question Type 增加 Number 选项
- [ ] 前端输入框对 Number 类型仅允许数字输入
- [ ] 后端对 Number 类型字段值进行格式验证

### Out of Scope

- 最小值/最大值范围约束 — 当前只需类型约束，不需要范围校验
- 整数/小数区分 — 不需要控制小数位数
- 数据导出格式转换 — 仅做表单输入验证

## Context

- FlowFlex 是一个工作流系统，问卷（Questionnaire）是 Stage 的组件之一
- Question 有 Type 字段控制输入类型（如 Text、Date 等）
- 前端使用 Vue 3 + Element Plus，后端使用 .NET 8 + SqlSugar
- 数据库为 PostgreSQL，Question 配置存储在 JSONB 列中

## Constraints

- **Tech stack**: 必须使用现有的 Vue 3 + .NET 8 技术栈
- **兼容性**: 不能破坏现有 Question Type 的行为
- **数据库**: 无需 migration，Number 类型作为枚举值添加即可

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 仅做输入约束，不做 min/max | 保持简单，满足当前需求 | — Pending |
| 前端限制 + 后端验证双重保障 | 防止绕过前端直接提交非数字 | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-25 after initialization*
