# AI Coding Package: Plugin Price List

## 项目概述

为 WFE Customer Onboarding 的 Billing Setup Stage 新增 Price List 插件，提供结构化价目表配置、后端持久化存储和基于 Case 的权限管控。

## 技术栈

- **后端**：C# / ASP.NET Core 8 / SqlSugar ORM / PostgreSQL
- **前端**：Vue 3 + Vite（已有原型，只做 API 对接改造）
- **认证**：JWT Bearer（同域名 cookie/localStorage 自动携带）
- **部署**：前端 dist → WFE 前端 public 目录（Nginx serve）

## 如何使用本规格包

1. 按 `tasks.md` 中的任务顺序执行开发
2. 每个任务包含具体文件路径和操作说明
3. 参考 `design.md` 了解架构决策和 API 设计细节
4. 参考 `requirements.md` 确认验收标准

## 文件阅读顺序

1. `requirements.md` — 完整需求 + 验收标准
2. `design.md` — 架构 + API + 数据库 + 测试策略
3. `tasks.md` — 按顺序执行的开发任务 checklist

## 子规格来源索引

| 本文件章节 | 来源 |
|-----------|------|
| requirements.md — 用户故事 | requirements-analysis/requirements.md |
| requirements.md — 技术需求 | technical-design/requirements.md |
| requirements.md — 测试覆盖 | test-verification/requirements.md |
| design.md — 架构+技术选型 | technical-design/design.md |
| design.md — 测试用例 | test-verification/design.md |
| tasks.md — 开发任务 | technical-design/tasks.md |
| tasks.md — 测试任务 | test-verification/tasks.md |

## 相关 docs 文件

| 文件 | 路径 |
|------|------|
| 数据库表结构 | `docs/plugin-price-list/technical-design/database-schema.md` |
| API 规格 | `docs/plugin-price-list/technical-design/api-spec.md` |
| 文件结构 | `docs/plugin-price-list/technical-design/file-structure.md` |
| 架构决策 | `docs/plugin-price-list/technical-design/architecture-decisions.md` |
| 时序图 | `docs/plugin-price-list/technical-design/flowchart.md` |
