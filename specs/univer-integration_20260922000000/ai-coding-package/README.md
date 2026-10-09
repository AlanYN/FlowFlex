# AI Coding 规格包 — Univer 在线文档管理

> 模块：univer-integration  
> 生成时间：2026-09-22  
> 技术栈：Vue 3.5 + .NET 8 + Univer 1.0.0-rc.0 + PostgreSQL

---

## 一、项目概述

在 FlowFlex 平台集成 Univer 开源文档引擎，新增独立的「文档管理」菜单（路径 `/documents`），支持：
- **电子表格（Sheet）**：类 Excel，全功能（公式/条件格式/筛选/批注等）
- **富文本文档（Doc）**：类 Word，全功能（富文本/超链接/绘图/批注等）
- **演示文稿（Slide）**：类 PPT，P1 功能
- **多人实时协同**：SignalR + 乐观锁
- **操作日志**：每次编辑操作异步记录

---

## 二、技术栈

| 层 | 技术 |
|----|------|
| 前端 | Vue 3.5 + Vite 5.4 + Element Plus 2.9 + Pinia 2.2 |
| 文档引擎 | Univer 1.0.0-rc.0（@univerjs/presets 系列） |
| 协同传输 | @univerjs/network + ASP.NET Core SignalR |
| 后端 | ASP.NET Core 8.0 + SqlSugar + AutoMapper |
| 数据库 | PostgreSQL（JSONB 存文档快照） |

---

## 三、文件阅读顺序

1. `requirements.md` → 完整产品需求和验收标准
2. `design.md` → UI 设计 + 架构方案 + 测试策略
3. `tasks.md` → **按顺序执行的开发任务清单**（从此文件开始开发）

---

## 四、关键约束（必须遵守）

| 约束 | 说明 |
|------|------|
| `workbookData` 禁止放 `ref()` | 大 JSON 对象放响应式系统会严重影响性能 |
| 编辑器容器必须有明确高度 | `height: calc(100vh - 56px)`，禁止 `auto` |
| Tailwind 隔离 | `tailwind.config.ts` 添加 `important: '#app'` |
| 操作日志 fire-and-forget | 失败静默，不影响编辑主流程 |

---

## 五、文件来源索引

| 文件 | 来源阶段 |
|------|---------|
| `requirements.md` | requirements-analysis + interaction-design + technical-design + test-verification |
| `design.md` | interaction-design + technical-design + test-verification |
| `tasks.md` | technical-design（开发任务）+ test-verification（测试任务）|

---

## 六、重要参考文档

开发过程中可随时查阅：

| 文档 | 路径 | 说明 |
|------|------|------|
| 集成技术指南 | `docs/INTEGRATION_GUIDE.md` | Univer 详细用法、数据格式、协同代码示例 |
| 组件规格 | `docs/univer-integration/technical-design/component-tree.md` | Props/Emits/模板结构 |
| API 规范 | `docs/univer-integration/technical-design/api-spec.md` | 所有接口定义 |
| 数据库表结构 | `docs/univer-integration/technical-design/database-schema.md` | DDL + ER 图 |
| 文件结构 | `docs/univer-integration/technical-design/file-structure.md` | 所有新增/修改文件列表 |
| 架构决策 | `docs/univer-integration/technical-design/architecture-decisions.md` | DD-01~DD-06 |
| 可交互原型 | `docs/univer-integration/interaction-design/demo.html` | 直接浏览器打开预览 |
