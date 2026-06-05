# FlowFlex Workflow Component Enhancements

## What This Is

FlowFlex 是一个工作流系统（Onboard Wizard），提供 Workflow、Stage、Checklist、Questionnaire 等组件支撑企业入职/业务流程管理。本次 milestone 聚焦 OW-621 中 12 项功能优化和 Bug 修复，覆盖 Change Log、Component 生命周期、Workflow 复制、前端交互、文件上传元数据、校验逻辑、权限等多个模块。

## Core Value

Workflow 组件功能完善且交互流畅，用户操作日志准确、组件生命周期正确维护、权限配置生效。

## Current Milestone: v1.1 OW-621 Workflow Component Enhancements

**Goal:** 完成 OW-621 中 12 个功能优化/Bug 修复，提升 Workflow 组件的功能完整性和用户体验

**Target features:**
- Checklist Done/Cancel 日志文案优化 + 日期到秒
- 已删除 Component 自动从 Stage 中移除
- Workflow Duplicate 时深拷贝 Stage Components
- 问卷 Next 按钮滚动到顶部
- Case 状态 Tag 位置调整 + Stage Detail 区域可收缩
- Stage/Component 更新传播 Workflow UpdatedBy
- 文件上传记录 UploadedBy + UploadDate
- 移除 StageSave 类型 Change Log
- Short Answer Grid 必填校验改为填一格即可
- 创建 Case 时 Workflow 状态与管理页面一致
- User Group 权限排查与修复
- Checklist comment 计数只统计 Notes

## Requirements

### Validated

- ✓ 问卷系统已有 Text/Date/Number 等字段类型 — v1.0
- ✓ 前端问卷渲染和后端验证链路已建立 — v1.0
- ✓ Question Type 枚举机制已存在 — v1.0
- ✓ Number 类型输入约束和后端验证 — v1.0 Phase 1

### Active

- [ ] Checklist Done/Cancel 日志文案改为 "Completed/Cancelled the task"，日期显示到秒
- [ ] 被删除的 Component 自动从 Workflow Stage 中移除
- [ ] 复制 Workflow 时同步复制 Stage 中的 Component 配置（深拷贝）
- [ ] 问卷点击 Next 后自动滚动到页面顶部
- [ ] Case 状态移到 Case Name 右侧 + Stage Detail 区域可收缩
- [ ] 更新 Stage/Component 时同步更新 Workflow 的 UpdatedBy
- [ ] 文件上传记录上传人和上传时间，并在前端展示
- [ ] 移除 StageSave 类型的 Change Log 记录
- [ ] Short Answer Grid 必填校验改为"填一格即算填写"
- [ ] 创建 Case 选择 Workflow 时状态与管理页面一致
- [ ] User Group 权限配置后用户可正常编辑 Case
- [ ] Checklist comment 计数只统计 Notes，不计入 Change Log

### Out of Scope

- 创建 Team 跨租户查重（#10） — IDM 外部系统 Bug，需 IDM 团队修复
- 最小值/最大值范围约束 — 上一 milestone 已排除
- 整数/小数区分 — 上一 milestone 已排除

## Context

- OW-621 是 P0 优先级的综合改进票，由 Lina Gao 创建，分配给 Kai Li
- 当前 Sprint: OW.2026.05/29-06/11
- 需求点 #3（Workflow Duplicate）需产品确认是"深拷贝新建"还是"共享引用"
- 需求点 #7 文件上传元数据存储在 Questionnaire answer JSONB 中
- 需求点 #12 权限排查链路长：PermissionService → IdentityHub IAM → UserGroup 配置
- 子票 OW-636（Action 调用时数字类型字段报错风险）状态 Ready to Test

## Constraints

- **Tech stack**: Vue 3 + .NET 8 + PostgreSQL，不引入新依赖
- **兼容性**: 不能破坏现有 Workflow/Stage/Checklist/Questionnaire 的行为
- **外部依赖**: #10 Team 创建查重需 IDM 团队配合，本次不做
- **时间**: 估算 AI 辅助 ~15.3h（3.2 工作日）

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 仅做输入约束，不做 min/max | 保持简单，满足当前需求 | ✓ Good (v1.0) |
| 前端限制 + 后端验证双重保障 | 防止绕过前端直接提交非数字 | ✓ Good (v1.0) |
| 排除 #10 IDM Team 创建查重 | 外部系统 Bug，FlowFlex 无法独立修复 | — Pending |
| #2 删除 Component 时立即清理 Stage 引用 | 比定时清理更简单直接 | — Pending |
| #7 UploadedBy 信息嵌入 answer JSONB | 与现有 UploadTime 存储方式一致 | — Pending |

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
*Last updated: 2026-06-02 after milestone v1.1 initialization*
