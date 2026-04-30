# 业务规则

> 本文件记录 FlowFlex 项目中所有业务规则、不变量、约束和计算逻辑。

---

## 入职流程状态管理

### BR-001: Onboarding 状态转换规则

- 状态枚举值：Inactive(0)、Active(1)、Completed(2)、InProgress(3)、Paused(4)、Aborted(5)、Cancelled(6)、Rejected(7)、Terminated(8)、ForceCompleted(9)
- 合法转换路径：
  - `Inactive` → `Active`（StartOnboarding 操作）
  - `Active` → `InProgress`（Proceed 操作，进入详情页）
  - `InProgress` → `Completed`（所有阶段完成，正常流程）
  - `InProgress` → `Paused`（Pause 操作，暂停流程）
  - `InProgress` → `Aborted`（Abort 操作，终止流程）
  - `Paused` → `InProgress`（Resume 操作，恢复流程）
  - `Aborted` → `InProgress`（Reactivate 操作，重新激活）
  - 任意非终态 → `ForceCompleted`（管理员强制完成）
  - 任意非终态 → `Cancelled`（用户取消）
- 终态（不可再转换）：Completed、Aborted、Cancelled、Rejected、Terminated、ForceCompleted
- 允许业务操作的状态：仅 Active 和 InProgress

### BR-002: Onboarding 操作权限

- `StartOnboarding`：将 Inactive 状态激活为 Active
- `Proceed`：进入入职详情页
- `Pause`：暂停进行中的流程
- `Resume`：恢复已暂停的流程
- `Abort`：终止流程
- `Reactivate`：重新激活已终止的流程
- `Edit`：编辑入职详情
- `View`：只读查看
- `Complete`：完成当前阶段

---

## 阶段完成与流转

### BR-003: 阶段完成条件

- 阶段完成需要满足以下所有条件：
  1. 该阶段所有必填（`IsRequired=true`）的 ChecklistTask 对应的 ChecklistTaskCompletion 记录 `IsCompleted=true`
  2. 该阶段关联的问卷（如有）已提交
  3. 该阶段的必填静态字段（如有）已填写
- 触发条件：用户执行 Complete 操作
- 影响范围：Onboarding.CurrentStageId、Onboarding.CurrentStageOrder、Onboarding.CompletionRate、Onboarding.StagesProgressJson

### BR-004: 阶段条件评估（StageCondition）

- 阶段完成后，系统使用 Microsoft RulesEngine 评估 `StageCondition.RulesJson` 中定义的规则
- 评估输入数据来源：阶段完成事件中的 Components 数据（检查项完成记录、问卷答案、必填字段值等）
- 条件满足时：执行 `ActionsJson` 中定义的操作
- 条件不满足时：跳转到 `FallbackStageId` 指定的回退阶段
- 注意：阶段条件评估现在在 `CompleteCurrentStageAsync` 中同步执行，而非异步事件处理

### BR-005: 阶段流转顺序

- 默认按 `Stage.Order`（`order_index`）升序流转
- 当存在 StageCondition 且条件不满足时，可跳转到 FallbackStageId 指定的阶段
- 最后一个阶段完成时，Onboarding 状态自动变为 Completed

### BR-006: 完成率计算

- `Onboarding.CompletionRate` = 已完成阶段数 / 总阶段数 × 100
- 阶段进度详情存储在 `StagesProgressJson`（JSONB 格式）

---

## 优先级管理

### BR-007: 优先级设置规则

- `Onboarding.Priority` 默认值为 `"Medium"`
- 当 Stage 1 完成时，系统检查 `IsPrioritySet` 标志
- 如果 `IsPrioritySet=false`，提示用户设置优先级
- 可选值：Low / Medium / High / Critical

---

## 检查项管理

### BR-008: ChecklistTask 状态转换

- 状态值：Pending → InProgress → Completed / Blocked / Cancelled
- 任务类型影响完成方式：
  - `Manual`：用户手动标记完成
  - `Automatic`：关联 Action 执行成功后自动完成
  - `Document`：上传文档后标记完成
  - `Approval`：审批通过后标记完成

### BR-009: 任务依赖关系

- `ChecklistTask.DependsOnTaskId` 定义前置任务依赖
- 前置任务未完成时，当前任务状态应为 Blocked
- 依赖关系为单层（不支持链式依赖的自动传递）

### BR-010: ChecklistTaskCompletion 维度隔离

- 完成记录按 Onboarding 实例维度存储
- 同一个 ChecklistTask 模板可被多个 Onboarding 实例复用
- 每个 Onboarding 实例有独立的完成状态记录
- 记录提交来源（`Source`）和环境信息（`IpAddress`、`UserAgent`）

---

## 操作与自动化

### BR-011: Action 触发规则

- 触发源类型（TriggerTypeEnum）：Stage(1)、Task(2)、Question(3)、Workflow(4)、Integration(5)
- 触发事件类型：Completed、Created、Updated、Answered
- 触发流程：
  1. 业务事件发生（如阶段完成）
  2. 发布 `ActionTriggerEvent`（MediatR INotification）
  3. `ActionTriggerEventHandler` 接收事件
  4. 调用 `IActionTriggerService.ExecuteActionsForTriggerAsync` 查找匹配的 ActionTriggerMapping
  5. 按 `ExecutionOrder` 顺序执行关联的 ActionDefinition
  6. 创建 ActionExecution 记录

### BR-012: Action 执行状态管理

- 执行状态：Pending(1) → Running(2) → Completed(3) / Failed(4) / Cancelled(5) / Timeout(6) / Retrying(7)
- 执行记录包含：输入参数、输出结果、执行时长、错误信息和堆栈
- 支持重试机制（Retrying 状态）

### BR-013: Action 类型与执行方式

- `Python`(1)：通过 Judge0 执行 Python 脚本
- `HttpApi`(2)：发起 HTTP API 调用
- `SendEmail`(3)：发送邮件
- `System`(4)：系统预定义操作

### BR-014: ActionTriggerMapping 条件过滤

- `TriggerConditions`（JSONB）定义触发条件，只有满足条件时才执行
- `MappingConfig`（JSONB）提供映射专属的自定义参数
- `IsEnabled` 控制映射是否生效
- 同一触发源可绑定多个操作，按 `ExecutionOrder` 排序执行

---

## 租户隔离

### BR-015: 多租户数据隔离

- 所有业务数据必须包含 `TenantId` 字段
- `TenantId` 从 HTTP Header `X-Tenant-Id` 获取
- 所有查询必须包含 `TenantId` 过滤条件
- 不同租户的数据完全隔离，不可跨租户访问

### BR-016: 多应用隔离

- `AppCode` 字段用于同一租户下的多应用隔离
- `AppCode` 从 HTTP Header `X-App-Code` 获取
- 默认值必须为小写 `"default"`（严禁使用大写 `"DEFAULT"`）

---

## 权限控制

### BR-017: 查看权限模式

- `Public`(0)：所有用户可查看
- `VisibleToTeams`(1)：仅列表中的团队/用户可查看
- `InvisibleToTeams`(2)：列表中的团队/用户以外的所有人可查看
- `Private`(3)：仅创建者/所有者可查看

### BR-018: 权限主体类型

- `Team`(1)：基于团队名称的权限控制（默认）
- `User`(2)：基于用户 ID 的权限控制
- `ViewPermissionSubjectType` 和 `OperatePermissionSubjectType` 可独立设置

### BR-019: 操作权限同步

- 当 `UseSameTeamForOperate=true` 时，操作权限主体自动与查看权限主体同步
- 适用于 Workflow、Stage、Onboarding 三个层级

### BR-020: 权限继承

- Onboarding 的权限可从 Workflow 继承
- Stage 的权限可从 Workflow 继承
- 继承关系：Workflow → Stage → Onboarding（逐级可覆盖）

---

## 问卷管理

### BR-021: 问卷生命周期

- 状态：Draft → Published → Archived
- 仅 Published 状态的问卷可被阶段引用
- 问卷结构以 JSONB 存储，支持多种题型和嵌套结构

### BR-022: 问卷提交规则

- `AllowDraft=true` 时允许保存草稿
- `AllowMultipleSubmissions=true` 时允许多次提交
- 必填题（`IsRequired=true`）未回答时不允许提交

---

## 集成管理

### BR-023: 集成认证凭据安全

- 认证凭据以加密形式存储（`EncryptedCredentials`）
- 凭据不可通过 API 明文返回

### BR-024: 实体映射规则

- 外部系统实体类型通过 EntityMapping 映射到 FlowFlex 实体
- 字段级映射通过 InboundFieldMapping 定义（入站方向）
- 映射支持数据转换和格式化

---

## 事件存储与重试

### BR-025: 事件重试机制

- 事件处理失败时，`RequiresRetry` 设为 `true`
- `NextRetryAt` 设为当前时间 + 5 分钟
- `MaxRetryCount` 默认为 3 次
- 每次处理递增 `ProcessCount`
- 超过最大重试次数后不再重试

### BR-026: 事件处理容错

- 事件处理器（Handler）中的异常不应影响其他 Handler 的执行
- 失败的事件以 `_failed` 后缀的 EventId 单独存储
- 事件状态：Published → Processed / Failed

---

## ID 与时间规范

### BR-027: 主键生成规则

- 所有实体使用雪花 ID（Snowflake ID）作为主键
- 类型为 `long`
- JSON 序列化时转为字符串（避免 JavaScript 精度丢失）

### BR-028: 时间字段规范

- 所有时间字段使用 `DateTimeOffset` 类型
- 存储和比较使用 UTC 时间：`DateTimeOffset.UtcNow`
- 严禁使用 `DateTime.Now` 或 `DateTime.UtcNow`

---

## 阶段组件管理

### BR-029: Stage Components 机制

- 阶段通过 `ComponentsJson`（JSONB）配置其包含的组件
- 组件类型包括：Checklist、Questionnaire、StaticField 等
- 旧的 ChecklistStageMapping 和 QuestionnaireStageMapping 已废弃
- 新的组件关联统一通过 Stage Components 管理

---

> 更新记录：
> - 2026-03-24：初始化，从代码库扫描提取所有业务规则和约束
