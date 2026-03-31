# 术语表

> 本文件定义 FlowFlex 项目中所有业务术语与技术术语的映射关系。所有 specs、docs 和对话中应使用一致的术语。

## 核心业务术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 入职流程 | Onboarding | `ff_onboarding` 表 | 客户/线索从创建到完成所有阶段的完整业务流程实例 |
| 工作流 | Workflow | `ff_workflow` 表 | 入职流程的模板定义，包含阶段顺序和配置 |
| 阶段 | Stage | `ff_stage` 表 | 工作流中的一个步骤，包含检查清单、问卷、静态字段等组件 |
| 阶段进度 | Stage Progress | `OnboardingStageProgress`（JSONB） | 每个入职实例中各阶段的完成状态，存储在 Onboarding 的 JSONB 字段中 |
| 阶段条件 | Stage Condition | `ff_stage_condition` 表 | 阶段完成后的条件规则，基于 Microsoft RulesEngine 评估 |
| 检查清单 | Checklist | `ff_checklist` 表 | 任务列表容器，可作为模板或实例使用 |
| 检查项 | Checklist Task | `ff_checklist_task` 表 | 检查清单中的单个任务项，支持手动/自动/文档/审批类型 |
| 检查项完成记录 | Task Completion | `ff_checklist_task_completion` 表 | 每个入职实例中检查项的完成状态（按 Onboarding 维度） |
| 问卷 | Questionnaire | `ff_questionnaire` 表 | 动态问卷，支持多种题型，结构以 JSONB 存储 |
| 问卷答案 | Questionnaire Answer | `ff_questionnaire_answer` 表 | 问卷的填写答案记录 |
| 静态字段值 | Static Field Value | `ff_static_field_values` 表 | 阶段中的静态表单字段值，按 Onboarding + Stage 维度存储 |
| 案例编码 | Case Code | `Onboarding.CaseCode` | 入职流程的唯一业务标识，从客户名称自动生成 |

## 操作与自动化术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 操作定义 | Action Definition | `ff_action_definitions` 表 | 可执行操作的配置（Python 脚本、HTTP API、邮件发送、系统操作） |
| 操作执行 | Action Execution | `ff_action_executions` 表 | 操作的执行历史和结果记录 |
| 触发映射 | Trigger Mapping | `ff_action_trigger_mappings` 表 | 操作与触发源（阶段/任务/问题/工作流/集成）的绑定关系 |
| 触发源 | Trigger Source | `TriggerTypeEnum` | 触发操作执行的来源类型：Stage、Task、Question、Workflow、Integration |

## 集成术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 集成 | Integration | `ff_integration` 表 | 与外部系统（CRM、ERP 等）的连接配置 |
| 实体映射 | Entity Mapping | `ff_entity_mapping` 表 | 外部系统实体类型与 WFE 实体类型的映射 |
| 字段映射 | Field Mapping | `ff_inbound_field_mapping` 表 | 外部系统字段与 WFE 字段的映射（入站方向） |
| 集成操作 | Integration Action | `ff_integration_action` 表 | 与集成关联的自动化操作 |
| 快捷链接 | Quick Link | `ff_quick_link` 表 | 集成相关的外部系统快捷访问链接 |

## 用户与权限术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 用户 | User | `ff_users` 表 | 系统内部用户（管理员、操作员） |
| 用户邀请 | User Invitation | `ff_user_invitations` 表 | Portal 访问邀请，包含加密令牌 |
| 租户 | Tenant | `TenantId` 字段 | 多租户隔离标识，从 HTTP Header `X-Tenant-Id` 获取 |
| 应用编码 | App Code | `AppCode` 字段 | 多应用隔离标识，从 HTTP Header `X-App-Code` 获取，默认值为 `"default"`（小写） |
| Portal 令牌 | Portal Token | `PortalAccess` 属性 | 客户门户的访问令牌，允许外部用户有限访问 |

## 消息与协作术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 消息 | Message | `ff_messages` 表 | 统一消息存储，支持内部消息、邮件、Portal 消息三种类型 |
| 内部备注 | Internal Note | `ff_internal_notes` 表 | 入职流程中的内部备注，支持回复和 @提及 |
| 操作变更日志 | Change Log | `ff_operation_change_log` 表 | 业务操作的详细变更记录（前后数据对比） |
| 事件 | Event | `ff_events` 表 | 通用事件存储，支持事件溯源和重试机制 |

## 动态数据术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 字段定义 | Define Field | `ff_define_field` 表 | 动态字段的元数据定义（类型、验证、显示规则） |
| 业务数据 | Business Data | `ff_business_data` 表 | 动态字段的实际数据值，以 JSONB 存储 |
| 字段组 | Field Group | `ff_field_group` 表 | 字段的分组容器 |

## 文件术语

| 业务术语 | 英文标识 | 技术对应 | 定义 |
|---------|---------|---------|------|
| 入职文件 | Onboarding File | `ff_onboarding_file` 表 | 与入职流程关联的文件（文档、图片、证书等） |
| 消息附件 | Message Attachment | `ff_message_attachment` 表 | 消息的附件文件 |

---

> 更新记录：
> - 2026-03-24：初始化，从代码库扫描提取所有业务实体和术语
