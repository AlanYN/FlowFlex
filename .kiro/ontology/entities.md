# 实体目录

> 本文件记录 FlowFlex 项目中所有核心业务实体及其关系、生命周期和关键属性。

---

## OW 模块（入职流程核心）

### Workflow（工作流）

- 表名：`ff_workflow`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：Stage (1:N, `WorkflowId`)
- 生命周期状态：`active` / `inactive`
- 关键属性：
  - `Name`：工作流名称
  - `IsDefault`：是否为默认工作流
  - `Version`：版本号
  - `IsAIGenerated`：是否由 AI 生成
  - `ConfigJson`：工作流配置（JSONB）
  - `VisibleInPortal`：是否在 Portal 中可见
  - `PortalPermission`：Portal 权限级别（Viewable / Completable）
- 权限字段：`ViewPermissionMode`、`ViewTeams`、`OperateTeams`、`UseSameTeamForOperate`

### Stage（阶段）

- 表名：`ff_stage`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - Workflow (N:1, `WorkflowId`)
  - Checklist (N:1, `ChecklistId`，可选)
  - Questionnaire (N:1, `QuestionnaireId`，可选)
  - StageCondition (1:N, `StageId`)
- 关键属性：
  - `Name` / `PortalName` / `InternalName`：三种名称（显示名、Portal 名、内部名）
  - `Order`：阶段排序（`order_index` 列）
  - `DefaultAssignee`：默认负责人（JSONB 数组）
  - `CoAssignees`：协同负责人（JSONB 数组）
  - `EstimatedDuration`：预估工期（天，支持小数）
  - `Required`：是否为必需阶段
  - `ComponentsJson`：阶段组件配置（JSONB）
  - `AttachmentManagementNeeded`：是否需要附件管理
  - `VisibleInPortal`：是否在 Portal 中可见
  - `PortalPermission`：Portal 权限级别（Viewable / Completable）
- 权限字段：`ViewPermissionMode`、`ViewTeams`、`OperateTeams`、`UseSameTeamForOperate`

### Onboarding（入职流程实例）

- 表名：`ff_onboarding`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - Workflow (N:1, `WorkflowId`)
  - Stage (N:1, `CurrentStageId`，当前阶段)
  - ChecklistTaskCompletion (1:N, `OnboardingId`)
  - OnboardingFile (1:N, `OnboardingId`)
  - StaticFieldValue (1:N, `OnboardingId`)
  - Message (1:N, `OnboardingId`)
  - InternalNote (1:N, `OnboardingId`)
  - OperationChangeLog (1:N, `OnboardingId`)
- 生命周期状态：`Inactive`(0) → `Active`(1) → `InProgress`(3) → `Completed`(2) / `Paused`(4) / `Aborted`(5) / `Cancelled`(6) / `Rejected`(7) / `Terminated`(8) / `ForceCompleted`(9)
- 终态（不可再转换）：Completed、Aborted、Cancelled、Rejected、Terminated、ForceCompleted
- 允许操作的状态：Active、InProgress
- 关键属性：
  - `CaseName` / `CaseCode`：客户名称和唯一业务编码
  - `LeadEmail` / `LeadPhone` / `ContactPerson` / `ContactEmail`：客户联系信息
  - `CurrentStageId` / `CurrentStageOrder`：当前阶段指针
  - `CompletionRate`：整体完成率（0-100）
  - `StartDate` / `EstimatedCompletionDate` / `ActualCompletionDate`：时间线
  - `CurrentAssigneeId` / `CurrentAssigneeName` / `CurrentTeam`：当前负责人
  - `Ownership` / `OwnershipName` / `OwnershipEmail`：所有者
  - `Priority`：优先级（Low/Medium/High/Critical），Stage 1 完成时设置
  - `StagesProgressJson`：各阶段进度详情（JSONB）
  - `SystemId` / `IntegrationId` / `EntityType` / `EntityId`：外部集成关联
- 权限字段：
  - `ViewPermissionSubjectType` / `OperatePermissionSubjectType`：权限主体类型（Team / User）
  - `ViewPermissionMode`：查看权限模式（Public / VisibleToTeams / InvisibleToTeams / Private）
  - `ViewTeams` / `ViewUsers`：查看权限主体列表（JSONB）
  - `OperateTeams` / `OperateUsers`：操作权限主体列表（JSONB）
  - `UseSameTeamForOperate`：操作权限是否与查看权限同步
- 计算属性（不持久化）：
  - `CurrentStageTimelineDays`：当前阶段已用天数
  - `TotalTimelineDays`：总用时天数
  - `StagesProgress`：阶段进度对象列表

### Checklist（检查清单）

- 表名：`ff_checklist`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - ChecklistTask (1:N, `ChecklistId`)
  - Stage (N:1, 通过 Stage.ChecklistId 关联)
- 关键属性：
  - `Name` / `Description`：名称和描述
  - `Team`：负责团队
  - `Type`：类型（Template / Instance）
  - `IsTemplate`：是否为模板
  - `TemplateId`：来源模板 ID
  - `EstimatedHours`：预估工时

### ChecklistTask（检查项）

- 表名：`ff_checklist_task`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - Checklist (N:1, `ChecklistId`)
  - ChecklistTaskCompletion (1:N, `TaskId`，按 Onboarding 维度)
  - ActionDefinition (N:1, `ActionId`，可选)
  - ActionTriggerMapping (N:1, `ActionMappingId`，可选)
  - ChecklistTask (N:1, `DependsOnTaskId`，前置任务依赖)
- 生命周期状态：`Pending` → `InProgress` → `Completed` / `Blocked` / `Cancelled`
- 关键属性：
  - `Name` / `Description`：任务名称和描述
  - `TaskType`：任务类型（Manual / Automatic / Document / Approval）
  - `IsRequired`：是否必填
  - `AssigneeId` / `AssigneeName` / `AssignedTeam`：负责人
  - `AssigneeJson`：结构化负责人信息（JSONB）
  - `Priority`：优先级（Low / Medium / High / Critical）
  - `Order`：排序（`order_index` 列）
  - `EstimatedHours` / `ActualHours`：预估/实际工时
  - `DueDate` / `CompletedDate`：截止日期和完成日期
  - `DependsOnTaskId`：前置任务依赖
  - `AttachmentsJson`：附件列表（JSONB）
  - `ActionId` / `ActionName` / `ActionMappingId`：关联的自动化操作

### ChecklistTaskCompletion（检查项完成记录）

- 表名：`ff_checklist_task_completion`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - Onboarding (N:1, `OnboardingId`)
  - Checklist (N:1, `ChecklistId`)
  - ChecklistTask (N:1, `TaskId`)
  - Stage (N:1, `StageId`)
- 说明：按 Onboarding 实例维度记录每个检查项的完成状态，实现模板复用
- 关键属性：
  - `IsCompleted`：是否完成
  - `CompletedTime`：完成时间
  - `CompletionNotes`：完成备注
  - `FilesJson`：关联文件（JSON 文本）
  - `Source`：提交来源（默认 `customer_portal`）
  - `IpAddress` / `UserAgent`：提交者环境信息

### Questionnaire（问卷）

- 表名：`ff_questionnaire`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 关联实体：
  - Stage (N:1, 通过 Stage.QuestionnaireId 关联)
  - QuestionnaireAnswer (1:N)
- 生命周期状态：`Draft` → `Published` → `Archived`
- 关键属性：
  - `Name` / `Description`：名称和描述
  - `Structure`：问卷结构定义（JSONB，支持多种题型、表格型、嵌套）
  - `Version`：版本号
  - `Category`：分类
  - `Tags`：标签（JSONB 数组）
  - `TotalQuestions` / `RequiredQuestions`：总题数和必填题数
  - `EstimatedMinutes`：预估填写时间
  - `AllowDraft`：允许保存草稿
  - `AllowMultipleSubmissions`：允许多次提交

### StageCondition（阶段条件）

- 表名：`ff_stage_condition`
- 基类：`OwEntityBase`（含租户隔离）
- 所属模块：OW
- 关联实体：
  - Stage (N:1, `StageId`)
  - Workflow (N:1, `WorkflowId`，冗余字段加速查询)
  - Stage (N:1, `FallbackStageId`，条件不满足时的回退阶段)
- 生命周期状态：`Valid` / `Invalid` / `Draft`
- 关键属性：
  - `Name` / `Description`：条件名称和描述
  - `RulesJson`：Microsoft RulesEngine 格式的规则定义（JSONB）
  - `ActionsJson`：条件满足时执行的操作配置（JSONB）
  - `FallbackStageId`：条件不满足时的回退阶段

### Event（事件）

- 表名：`ff_events`
- 基类：`EntityBaseCreateInfo`
- 所属模块：OW
- 说明：通用事件存储表，支持事件溯源和重试机制
- 关键属性：
  - `EventId`：业务层生成的唯一标识
  - `EventType`：事件类型（如 `OnboardingStageCompleted`）
  - `EventVersion`：事件版本
  - `AggregateId` / `AggregateType`：聚合根 ID 和类型
  - `EventSource`：事件来源（CustomerPortal / AdminPanel / API）
  - `EventData`：事件数据（JSONB）
  - `EventMetadata`：事件元数据（JSONB，含路由标签、优先级）
  - `EventStatus`：事件状态（Published / Failed / Processed）
  - `ProcessCount` / `LastProcessedAt`：处理次数和最后处理时间
  - `RequiresRetry` / `NextRetryAt` / `MaxRetryCount`：重试机制（默认最多 3 次）
  - `ErrorMessage`：错误信息
  - `RelatedEntityId` / `RelatedEntityType`：关联业务实体（快速查询）

---

## Action 模块（操作与自动化）

### ActionDefinition（操作定义）

- 表名：`ff_action_definitions`
- 基类：`EntityBaseCreateInfo`
- 所属模块：Action
- 关联实体：
  - ActionTriggerMapping (1:N, `ActionDefinitionId`)
  - ActionExecution (1:N, `ActionDefinitionId`)
- 关键属性：
  - `ActionCode`：操作编码（唯一标识，最长 20 字符）
  - `ActionName`：操作名称
  - `ActionType`：操作类型（Python / HttpApi / SendEmail / System）
  - `ActionConfig`：操作配置（JSONB，包含执行参数）
  - `IsEnabled`：是否启用
  - `IsTools`：是否为工具类操作
  - `IsAIGenerated`：是否由 AI 生成
  - `TriggerType`：触发类型（Stage / Task / Question / Workflow / Integration）

### ActionTriggerMapping（触发映射）

- 表名：`ff_action_trigger_mappings`
- 基类：`EntityBaseCreateInfo`
- 所属模块：Action
- 关联实体：
  - ActionDefinition (N:1, `ActionDefinitionId`)
  - Workflow (N:1, `WorkFlowId`，可选)
  - Stage (N:1, `StageId`，可选)
- 说明：将操作绑定到触发源，实现操作的复用和灵活配置
- 关键属性：
  - `TriggerType`：触发类型（Stage / Task / Question）
  - `TriggerSourceId`：触发源实体 ID
  - `TriggerEvent`：触发事件（Completed / Created / Updated / Answered）
  - `ExecutionOrder`：执行顺序（数字越小越先执行）
  - `TriggerConditions`：触发条件（JSONB）
  - `MappingConfig`：映射专属参数（JSONB）
  - `IsEnabled`：是否启用

### ActionExecution（操作执行记录）

- 表名：`ff_action_executions`
- 基类：`EntityBaseCreateInfo`
- 所属模块：Action
- 关联实体：
  - ActionDefinition (N:1, `ActionDefinitionId`)
  - ActionTriggerMapping (N:1, `ActionTriggerMappingId`，可选)
- 生命周期状态：`Pending`(1) → `Running`(2) → `Completed`(3) / `Failed`(4) / `Cancelled`(5) / `Timeout`(6) / `Retrying`(7)
- 关键属性：
  - `ExecutionId`：业务层生成的唯一执行标识
  - `ActionName` / `ActionType`：冗余字段，便于查询
  - `TriggerContext`：触发时的上下文数据（JSONB）
  - `ExecutionInput` / `ExecutionOutput`：执行输入和输出（JSONB）
  - `StartedAt` / `CompletedAt` / `DurationMs`：执行时间线
  - `ErrorMessage` / `ErrorStackTrace`：错误信息
  - `ExecutorInfo`：执行器实例信息（JSONB）

---

## Integration 模块（外部集成）

### Integration（集成）

- 表名：`ff_integration`
- 基类：`EntityBaseCreateInfo`
- 所属模块：Integration
- 关联实体：
  - EntityMapping (1:N)
  - IntegrationAction (1:N)
  - QuickLink (1:N)
- 生命周期状态：由 `IntegrationStatus` 枚举定义
- 关键属性：
  - `Type`：集成类型（CRM / ERP / Marketing 等）
  - `Name` / `Description`：名称和描述
  - `SystemName`：外部系统名称
  - `EndpointUrl`：外部系统 API 端点
  - `AuthMethod`：认证方式（`AuthenticationMethod` 枚举）
  - `EncryptedCredentials`：加密的认证凭据
  - `LastSyncDate`：最后同步时间
  - `ErrorMessage`：连接失败时的错误信息
  - `InboundAttachments` / `OutboundAttachments`：入站/出站附件配置（JSON）

### EntityMapping（实体映射）

- 表名：`ff_entity_mapping`
- 所属模块：Integration
- 说明：外部系统实体类型与 FlowFlex 实体类型的映射关系

### InboundFieldMapping（入站字段映射）

- 表名：`ff_inbound_field_mapping`
- 所属模块：Integration
- 说明：外部系统字段与 FlowFlex 字段的映射（入站方向）

### IntegrationAction（集成操作）

- 表名：`ff_integration_action`
- 所属模块：Integration
- 说明：与集成关联的自动化操作

### QuickLink（快捷链接）

- 表名：`ff_quick_link`
- 所属模块：Integration
- 说明：集成相关的外部系统快捷访问链接

---

## DynamicData 模块（动态数据）

### DefineField（字段定义）

- 表名：`ff_define_field`
- 基类：`OwEntityBase`（含租户隔离）
- 所属模块：DynamicData
- 关联实体：
  - BusinessData (通过 `ModuleId` 关联)
  - FieldGroup (通过分组关系)
- 关键属性：
  - `ModuleId`：所属模块 ID
  - `FieldName`：字段名称（标识符和显示名）
  - `DataType`：数据类型（`DataType` 枚举）
  - `SourceType` / `SourceName`：数据来源
  - `IsSystemDefine`：是否系统预定义
  - `IsStatic`：是否为静态字段
  - `IsRequired`：是否必填
  - `IsComputed`：是否为计算字段
  - `IsHidden`：是否隐藏
  - `AllowEdit` / `AllowEditItem`：是否允许编辑
  - `Sort`：排序
  - `AdditionalInfo`：附加信息（JSONB）

### BusinessData（业务数据）

- 表名：`ff_business_data`
- 基类：`OwEntityBase`（含租户隔离）
- 所属模块：DynamicData
- 说明：动态字段的实际数据值存储
- 关键属性：
  - `ModuleId`：所属模块 ID
  - `InternalData`：内部扩展数据（JSONB）

---

## 其他 OW 模块实体（简要）

| 实体 | 表名 | 说明 |
|------|------|------|
| StaticFieldValue | `ff_static_field_values` | 阶段中的静态表单字段值，按 Onboarding + Stage 维度存储 |
| OnboardingFile | `ff_onboarding_file` | 与入职流程关联的文件（文档、图片、证书等） |
| Message | `ff_messages` | 统一消息存储，支持内部消息、邮件、Portal 消息 |
| MessageAttachment | `ff_message_attachment` | 消息附件文件 |
| InternalNote | `ff_internal_notes` | 入职流程中的内部备注，支持回复和 @提及 |
| OperationChangeLog | `ff_operation_change_log` | 业务操作的详细变更记录（前后数据对比） |
| QuestionnaireAnswer | `ff_questionnaire_answer` | 问卷的填写答案记录 |
| QuestionnaireSection | — | 问卷分区（嵌套在 Questionnaire.Structure JSONB 中） |
| QuestionnaireStageMapping | — | 问卷与阶段的映射（已废弃，改用 Stage Components） |
| ChecklistStageMapping | — | 检查清单与阶段的映射（已废弃，改用 Stage Components） |
| User | `ff_users` | 系统内部用户（管理员、操作员） |
| UserInvitation | `ff_user_invitations` | Portal 访问邀请，包含加密令牌 |
| AccessToken | — | 访问令牌管理 |
| EmailBinding | — | 邮箱绑定 |
| AIPromptHistory | — | AI 提示词历史记录 |
| UserAIModelConfig | — | 用户 AI 模型配置 |
| ChecklistTaskNote | — | 检查项备注 |

---

## 实体关系总览（ER 概要）

```
Workflow (1) ──→ (N) Stage
Stage (1) ──→ (0..1) Checklist
Stage (1) ──→ (0..1) Questionnaire
Stage (1) ──→ (N) StageCondition

Checklist (1) ──→ (N) ChecklistTask
ChecklistTask (N) ──→ (1) ChecklistTask [DependsOnTaskId, 自引用]

Onboarding (N) ──→ (1) Workflow
Onboarding (N) ──→ (1) Stage [CurrentStageId]
Onboarding (1) ──→ (N) ChecklistTaskCompletion
Onboarding (1) ──→ (N) OnboardingFile
Onboarding (1) ──→ (N) StaticFieldValue
Onboarding (1) ──→ (N) Message
Onboarding (1) ──→ (N) InternalNote
Onboarding (1) ──→ (N) OperationChangeLog

ActionDefinition (1) ──→ (N) ActionTriggerMapping
ActionDefinition (1) ──→ (N) ActionExecution
ActionTriggerMapping (N) ──→ (1) ActionExecution [可选追溯]

Integration (1) ──→ (N) EntityMapping
Integration (1) ──→ (N) IntegrationAction
Integration (1) ──→ (N) QuickLink

DefineField ──→ BusinessData [通过 ModuleId 关联]
```

---

> 更新记录：
> - 2026-03-24：初始化，从代码库扫描提取所有业务实体、关系和生命周期
