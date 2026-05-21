# Requirements: Action Questionnaire Context（问卷答案进入 Action 执行上下文）

## 模块概述

为 WFE 的 Action 执行链路补齐问卷答案上下文能力：允许 Stage Condition 触发的 Action 在执行时读取当前 Stage 的问卷答案，并将这些值安全、稳定地传递到 HTTP Action / 后续执行器中，用于创建 CRM Customer 等外部系统调用。

---

## 用户故事

### US-001: 在 Action 中引用问卷答案

**As a** WFE 管理员 / 集成人员  
**I want to** 在 Action 配置中引用当前 Stage 的问卷答案  
**So that** 我可以将问卷中填写的 Customer 信息自动传给 CRM 创建 Customer 接口

**验收标准（AC）：**
- AC-001-1: 当 Stage Condition 触发 TriggerAction 时，执行上下文中包含当前 Case 下所有已完成 Stage 的可用问卷答案
- AC-001-2: 问卷答案至少提供两种可引用结构：`questionnaireAnswerMap` 和 `questionnaireAnswerByQuestionId`
- AC-001-3: Action 配置中的模板可以引用问卷答案路径，例如 `{{questionnaireAnswerByQuestionId.2001}}`
- AC-001-4: 未配置问卷组件或无答案时，Action 仍可执行，不因缺失问卷数据报错中断

### US-002: 稳定地按题目标识取值

**As a** WFE 管理员 / 集成人员  
**I want to** 使用稳定的题目标识而不是题目文案来引用问卷答案  
**So that** 问卷题目改名、多语言或重排时，不会因为文案变化导致 Action 取值失效

**验收标准（AC）：**
- AC-002-1: v1 以 `questionId` 作为稳定引用底座，不以 `questionText` 作为正式映射 key
- AC-002-2: 上下文中保留 `questionText` 仅用于展示和调试，不作为业务取值主键
- AC-002-3: 设计需预留未来引入 `questionKey` 的扩展空间，但本期不强依赖

### US-003: 在 HTTP Action 中支持深路径模板

**As a** WFE 管理员 / 集成人员  
**I want to** 在 HTTP Action 的 URL、Params、Headers、Body 中使用深路径模板变量  
**So that** 我可以从问卷答案、previousActionResult 等嵌套上下文中直接取值

**验收标准（AC）：**
- AC-003-1: HTTP Action 模板解析支持顶层字段，例如 `{{CaseCode}}`
- AC-003-2: HTTP Action 模板解析支持点路径，例如 `{{previousActionResult.data.customerCode}}`
- AC-003-3: HTTP Action 模板解析支持数值字符串 key，例如 `{{questionnaireAnswerMap.1001.2001}}`
- AC-003-4: 模板变量找不到值时，不导致 Action 执行异常退出；系统需按统一规则处理并记录日志

### US-004: 前端仅展示真实可用变量

**As a** WFE 管理员 / 集成人员  
**I want to** 在 Action 配置 UI 中看到和后端真实执行能力一致的变量提示  
**So that** 我配置出来的模板在运行时确实能生效，不会被误导

**验收标准（AC）：**
- AC-004-1: Variables Panel 和自动补全展示问卷相关变量结构
- AC-004-2: 示例变量名与后端真实上下文字段一致
- AC-004-3: 前端不再主推后端当前并不稳定支持的伪路径示例

### US-005: 兼容现有 Static Field 与链式 Action 能力

**As a** 系统  
**I want to** 在新增问卷上下文能力的同时保持现有 static field 和 previousActionResult 的使用方式不变  
**So that** 现有 Action 配置无需批量回归修改

**验收标准（AC）：**
- AC-005-1: 现有 static field 顶层变量仍可继续使用
- AC-005-2: 现有 previousActionResult / `prev_` 链式传值逻辑保持兼容
- AC-005-3: 无问卷取值需求的 Action 执行结果与现状保持一致

---

## 非功能需求

| 类别 | 要求 |
|------|------|
| 兼容性 | 保持现有 static field、integration token、previousActionResult 的兼容行为 |
| 可扩展性 | 后端上下文结构需预留未来 questionKey、多 Stage 聚合、更多执行器接入能力 |
| 可观测性 | 模板变量解析失败或问卷值缺失需有 warning 级别日志，便于排查 |
| 稳定性 | 无问卷、空答案、题型复杂值场景下，Action 执行不应崩溃 |
| 安全性 | 避免在日志中无差别输出敏感问卷内容；对外只暴露必要执行结果 |

---

## 范围内（In Scope）

- TriggerAction 执行时注入当前 Case 下所有已完成 Stage 的问卷答案上下文
- 问卷答案结构化为可稳定引用的数据结构
- HTTP Action 模板解析支持深路径访问
- 前端变量面板 / 自动补全与后端真实能力对齐
- 单元测试与集成测试补齐

## 范围外（Out of Scope）

- 新增问卷 `questionKey` 编辑能力
- 复杂问卷题型（附件/复杂 grid）到 CRM 特殊格式转换规则的完整产品化
- 字段映射式可视化配置器（纯选择源字段的高级 UI）
- Python Action 参数提取范式重构
- 多 Stage 问卷聚合规则产品化
