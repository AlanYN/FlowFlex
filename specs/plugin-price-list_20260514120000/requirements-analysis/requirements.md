# Requirements: Plugin Price List

## 模块概述

为 WFE Customer Onboarding 流程的 Billing Setup Stage 新增 Price List 插件，替代手工 Excel 方式，实现结构化价目表配置、持久化存储和权限管控。

---

## 用户故事

### US-001: 加载已保存的 Price List

**As a** WFE Case 的 Stage Assignee / Co-assignee  
**I want to** 从 Billing Setup Stage 的 Quick Link 进入 Price List 页面时，自动加载该 Case 已保存的数据  
**So that** 我可以继续编辑之前的工作，不需要重新填写

**验收标准（AC）：**
- AC-001-1: 页面加载时，根据 URL 中的 caseId 参数调用 GET API 获取数据
- AC-001-2: 如果该 Case 有已保存的数据，自动填充到页面所有字段
- AC-001-3: 如果该 Case 无数据（404），显示空白页面，用户可从零开始配置
- AC-001-4: 加载过程中显示 loading 状态

### US-002: 保存 Price List

**As a** 有写权限的用户  
**I want to** 点击 Save 按钮将当前配置保存到服务器  
**So that** 数据不会因浏览器刷新或切换用户而丢失

**验收标准（AC）：**
- AC-002-1: 点击 Save 时，将完整 Price List JSON 通过 POST API 发送到后端
- AC-002-2: 保存成功后显示成功提示（toast/message）
- AC-002-3: 保存失败时显示错误提示，数据不丢失
- AC-002-4: 同一 Case 重复保存为更新操作（upsert），不创建重复记录

### US-003: 提交 Price List

**As a** 有写权限的用户  
**I want to** 点击 Submit 按钮将 Price List 标记为已提交  
**So that** 下游团队（Billing/IT）知道这份价目表已经确认，可以开始 setup

**验收标准（AC）：**
- AC-003-1: Submit 时调用 POST /submit API，将状态从 draft 改为 submitted
- AC-003-2: 提交成功后，页面切换为只读模式（不可再编辑）
- AC-003-3: 提交失败时显示错误提示，状态不变
- AC-003-4: 数据库 status 字段设计为 VARCHAR(20)，预留 draft → submitted → approved 状态流转空间（本期只实现 draft → submitted）

> **备注**：撤回功能（submitted → draft）本期不实现，但数据模型和 API 设计需预留扩展空间，后续确认后可快速添加。

### US-004: 权限控制 — 只读模式

**As a** 有 Stage 查看权限但非 Assignee 的用户  
**I want to** 进入 Price List 页面时只能查看，不能修改  
**So that** 数据安全不被未授权用户篡改

**验收标准（AC）：**
- AC-004-1: 页面加载时，API 返回当前用户的权限级别（write / read）
- AC-004-2: read 权限：所有 input/select/textarea 设为 disabled
- AC-004-3: read 权限：隐藏 Save、Submit、Add Item、Delete 按钮
- AC-004-4: write 权限：正常显示所有编辑功能

### US-005: 权限控制 — 无权限用户

**As a** 无权限用户  
**I want to** 被阻止访问 Price List 页面  
**So that** 敏感定价信息不被泄露

**验收标准（AC）：**
- AC-005-1: 无权限用户访问 API 时返回 403 Forbidden
- AC-005-2: 前端收到 403 后显示"无权限访问"提示页面

### US-006: 后端数据持久化

**As a** 系统  
**I want to** 将 Price List 数据以 JSONB 格式存储在 PostgreSQL 中  
**So that** 数据结构灵活，支持未来扩展（对接 BNP API、版本管理等）

**验收标准（AC）：**
- AC-006-1: 数据库表 `ff_plugin_price_lists`，一个 Case 对应一条记录
- AC-006-2: 同一租户 + 应用下，case_id 唯一（软删除记录除外）
- AC-006-3: 记录包含完整审计字段（create_by, modify_by, create_date, modify_date）
- AC-006-4: 支持租户隔离（tenant_id + app_code 过滤）

### US-007: WFE 集成 — Quick Link 入口

**As a** WFE 管理员  
**I want to** 在 Billing Setup Stage 的 Quick Links 中配置 Price List 页面链接  
**So that** 用户可以从 Case 详情页一键进入 Price List 配置

**验收标准（AC）：**
- AC-007-1: 前端静态文件部署在 WFE 后端的 `wwwroot/plugins/price-list/` 目录
- AC-007-2: 通过 `/plugins/price-list/index.html?caseId={caseId}&customerCode={code}&customerName={name}` 访问
- AC-007-3: 页面共享 WFE 的 JWT 认证，不需要单独登录

---

## 非功能需求

| 类别 | 要求 |
|------|------|
| 性能 | GET API 响应时间 < 500ms |
| 安全 | 所有 API 需要 JWT 认证 + 租户隔离 |
| 数据完整性 | 同一 Case 不允许重复记录（唯一约束） |
| 兼容性 | 前端支持 Chrome/Edge 最新版本 |
| 可扩展性 | 数据结构支持未来对接 BNP API（本期不实现） |

---

## 范围外（本期不做）

- BNP API 对接（Submit 只改状态，不调外部接口）
- 审批流程（submitted → approved 状态流转）
- 版本管理（保留历史版本）
- 静态数据动态化（billing-data.json / location-mapping.json 暂不从 API 获取）
