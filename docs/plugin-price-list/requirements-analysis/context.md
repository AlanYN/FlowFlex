# Context: Plugin Price List — 需求分析阶段

> 本文件为下游阶段（交互设计、技术方案、测试验证）提供输入摘要。

---

## 业务目标

为 WFE Customer Onboarding 的 Billing Setup Stage 提供结构化 Price List 配置工具，替代手工 Excel，确保 Sales 只能选择 BNP 系统支持的 Billing Item，减少 billing setup 错误。

---

## 用户故事摘要

| ID | 角色 | 核心行为 | 关键约束 |
|----|------|---------|---------|
| US-001 | Stage Assignee | 进入页面自动加载已保存数据 | 按 caseId 查询，404 时显示空白 |
| US-002 | 有写权限用户 | 保存 Price List 到服务器 | Upsert 语义，同 Case 不重复 |
| US-003 | 有写权限用户 | 提交 Price List | 状态 draft→submitted，提交后只读 |
| US-004 | 只读用户 | 查看但不能编辑 | 所有输入 disabled，隐藏操作按钮 |
| US-005 | 无权限用户 | 被拦截 | 返回 403 |
| US-006 | 系统 | JSONB 持久化 | 租户隔离，审计字段，唯一约束 |
| US-007 | WFE 管理员 | Quick Link 配置 | 静态文件部署，同域名 JWT |

---

## 核心数据实体

### PluginPriceList

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | long (雪花ID) | 主键 |
| CaseId | string(50) | WFE Case ID，同租户下唯一 |
| CustomerCode | string(50) | 客户代码 |
| CustomerName | string(200) | 客户名称 |
| PriceListType | string(50) | 类型（默认 "Customer Specific"） |
| StartDate | string(20) | 生效日期 |
| EndDate | string(20) | 结束日期 |
| Data | JSONB | 完整 sections 数据 |
| Status | string(20) | draft / submitted / approved |
| TenantId | string(32) | 租户隔离 |
| AppCode | string(32) | 应用隔离 |

---

## 权限模型

- **写权限**：Case 所在 Stage 的 Assignee / Co-assignee
- **读权限**：有 Stage 查看权限的用户
- **无权限**：其他用户 → 403

权限判断依赖 WFE 现有的 `IPermissionService`。

---

## 技术约束

- 后端：C# + ASP.NET Core，遵循 FlowFlex 分层架构
- 数据库：PostgreSQL，表名 `ff_plugin_price_lists`
- 前端：Vue 3（Amanda 已完成原型），只做 API 对接改造
- 认证：JWT，同域名 cookie 自动携带
- 部署：静态文件放 `wwwroot/plugins/price-list/`

---

## 范围边界

**本期实现**：GET/POST/Submit API + 权限 + 前端 API 对接 + 只读模式 + 部署  
**本期不做**：BNP 对接、审批流程、版本管理、撤回功能、静态数据动态化
