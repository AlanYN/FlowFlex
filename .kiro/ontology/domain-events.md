# 领域事件

> 本文件记录 FlowFlex 项目中所有领域事件的触发条件、数据结构和影响范围。

---

## 事件基础设施

- 事件总线：MediatR（进程内发布/订阅）
- 事件接口：`INotification`（MediatR）
- 事件存储：`ff_events` 表（通用事件存储，支持溯源和重试）
- 事件处理模式：异步 Handler，失败不阻塞其他 Handler

---

## DE-001: OnboardingStageCompletedEvent

### 基本信息

- 事件类：`FlowFlex.Domain.Shared.Events.OnboardingStageCompletedEvent`
- 接口：`MediatR.INotification`
- 触发条件：入职流程中某个阶段完成时发布
- 发布方：OnboardingService（`CompleteCurrentStageAsync` 方法）

### 事件数据

| 字段 | 类型 | 说明 |
|------|------|------|
| `EventId` | string | 唯一事件 ID（GUID） |
| `Timestamp` | DateTimeOffset | 事件时间戳（UTC） |
| `Version` | string | 事件版本（默认 "1.0"） |
| `TenantId` | string | 租户 ID |
| `UserId` / `UserName` | long / string | 触发用户 |
| `OnboardingId` | long | 入职流程 ID |
| `LeadId` | string | 客户/线索 ID |
| `WorkflowId` / `WorkflowName` | long / string | 工作流信息 |
| `CompletedStageId` / `CompletedStageName` | long / string | 已完成的阶段 |
| `StageCategory` | string | 阶段类型/分类 |
| `NextStageId` / `NextStageName` | long? / string | 下一阶段（如有） |
| `CompletionRate` | decimal | 完成率（0-100） |
| `IsFinalStage` | bool | 是否为最后一个阶段 |
| `ResponsibleTeam` | string | 负责团队 |
| `AssigneeId` / `AssigneeName` | long? / string | 负责人 |
| `BusinessContext` | Dictionary | 业务上下文数据 |
| `RoutingTags` | List | 路由标签（用于事件路由） |
| `Priority` | string | 优先级（默认 "Medium"） |
| `Source` | string | 事件来源（默认 "Unis.CRM.Onboarding"） |
| `RelatedEntity` | RelatedEntityInfo | 关联实体信息（类型、ID、名称、状态、扩展属性） |
| `Description` | string | 事件描述 |
| `Tags` | List | 事件标签 |
| `Components` | StageCompletionComponents | 阶段完成时的组件快照 |

### Components 数据结构

`StageCompletionComponents` 包含阶段完成时的完整组件快照：

- `Checklists`：检查清单信息列表（含任务详情、完成率、总任务数）
- `TaskCompletions`：检查项完成记录快照（完成状态、备注、完成人、时间）
- `Questionnaires`：问卷信息列表（含结构、题目数、提交规则）
- `QuestionnaireAnswers`：问卷答案快照（题目文本、答案、状态）
- `RequiredFields`：必填字段值和验证状态

### 订阅方（Handler）

#### 1. OnboardingStageCompletedLogHandler

- 类：`FlowFlex.Application.Notification.OnboardingStageCompletedLogHandler`
- 职责：将事件持久化到 `ff_events` 表
- 处理流程：
  1. 从事件数据中恢复 UserContext（TenantId、UserId、UserName），确保后台任务兼容性
  2. 构建事件元数据（路由标签、优先级、处理信息）
  3. 创建 Event 实体，序列化事件数据和元数据为 JSON
  4. 保存到 `ff_events` 表，状态为 `Published`
- 容错：处理失败时，以 `{EventId}_failed` 为 ID 保存失败记录，`RequiresRetry=true`，`NextRetryAt` = 当前时间 + 5 分钟
- 注意：阶段条件评估已移至 `CompleteCurrentStageAsync` 同步执行，不再在此 Handler 中异步处理

---

## DE-002: ActionTriggerEvent

### 基本信息

- 事件类：`FlowFlex.Domain.Shared.Events.Action.ActionTriggerEvent`
- 接口：`MediatR.INotification`
- 触发条件：任何业务场景需要触发自动化操作时发布
- 发布方：各业务 Service（阶段完成、任务完成、问卷回答等场景）

### 事件数据

| 字段 | 类型 | 说明 |
|------|------|------|
| `TriggerSourceType` | string | 触发源类型（Stage / Task / Question / Workflow / Integration） |
| `TriggerSourceId` | long | 触发源实体 ID |
| `TriggerEventType` | string | 触发事件类型（Completed / Created / Updated / Answered） |
| `ContextData` | object | 操作执行所需的上下文数据 |
| `UserId` | long? | 触发用户 ID |
| `WorkflowId` | long? | 工作流 ID |
| `StageId` | long? | 阶段 ID |
| `TriggeredAt` | DateTime | 事件发生时间（UTC） |

### 订阅方（Handler）

#### 1. ActionTriggerEventHandler

- 类：`Application.Notification.ActionTriggerEventHandler`
- 职责：查找并执行与触发源匹配的所有操作
- 处理流程：
  1. 接收 ActionTriggerEvent
  2. 调用 `IActionTriggerService.ExecuteActionsForTriggerAsync`
  3. 根据 TriggerSourceType + TriggerSourceId + TriggerEventType 查找匹配的 ActionTriggerMapping
  4. 按 ExecutionOrder 顺序执行关联的 ActionDefinition
  5. 为每次执行创建 ActionExecution 记录
- 容错：异常被捕获并记录日志，然后重新抛出

---

## 事件流转总览

```
阶段完成操作
  │
  ├──→ OnboardingStageCompletedEvent
  │     ├──→ OnboardingStageCompletedLogHandler → 保存到 ff_events 表
  │     └──→ （阶段条件评估已移至同步执行）
  │
  └──→ ActionTriggerEvent (TriggerSourceType="Stage")
        └──→ ActionTriggerEventHandler
              └──→ IActionTriggerService.ExecuteActionsForTriggerAsync
                    ├──→ 查找匹配的 ActionTriggerMapping
                    ├──→ 按 ExecutionOrder 执行 ActionDefinition
                    └──→ 创建 ActionExecution 记录

任务完成操作
  └──→ ActionTriggerEvent (TriggerSourceType="Task")
        └──→ ActionTriggerEventHandler → 同上

问卷回答操作
  └──→ ActionTriggerEvent (TriggerSourceType="Question")
        └──→ ActionTriggerEventHandler → 同上
```

---

## 事件存储格式（ff_events 表）

事件持久化到 `ff_events` 表时的关键字段映射：

| Event 实体字段 | 来源 | 示例值 |
|---------------|------|--------|
| `EventId` | 事件的 EventId | `"a1b2c3d4-..."` |
| `EventType` | 固定值 | `"OnboardingStageCompleted"` |
| `EventVersion` | 事件的 Version | `"1.0"` |
| `AggregateId` | 事件的 OnboardingId | `1935628742495965184` |
| `AggregateType` | 固定值 | `"Onboarding"` |
| `EventSource` | 事件的 Source | `"Unis.CRM.Onboarding"` |
| `EventData` | 完整事件序列化 JSON | `{...}` |
| `EventMetadata` | 路由标签+优先级+处理信息 | `{...}` |
| `EventStatus` | 初始状态 | `"Published"` |
| `RelatedEntityId` | 事件的 CompletedStageId | `1935628742495965185` |
| `RelatedEntityType` | 固定值 | `"Stage"` |
| `TenantId` | 事件的 TenantId | `"tenant-001"` |

---

> 更新记录：
> - 2026-03-24：初始化，从代码库扫描提取所有领域事件定义和处理流程
