# Requirements Document

## Introduction

本功能允许有权限的内部用户将某个已完成（Completed）的 Stage 重新打开（Roll Back），使其回到可编辑状态（InProgress），以便客户或内部人员修正填写内容后再次 Complete。该功能针对 UT Customer Onboarding 场景中 Accounting 在后续 Stage 发现前置 Stage 内容有误时无法修改的痛点。

> **设计假设说明（针对 BA 尚未明确的问题）：**
>
> **假设 1 — 触发者（Who）：** Roll Back 由有权限的内部用户触发（如 Accounting Team 成员），而非仅限管理员。权限通过 Stage 配置中新增的 `RollBackTeams` 字段控制。这与现有 `OperateTeams` 逻辑一致，工作量更小，也更灵活。
>
> **假设 2 — 后续 Stage 的处理：** Stage 被 Roll Back 后，后续 Stage **不暂停、不修改状态**，但向后续 Stage 的负责人（Assignee）发送通知，告知前置 Stage 已被重新打开，可能影响当前工作。Onboarding 整体状态不变（保持 InProgress）。
>
> **假设 3 — Action 重新触发：** Stage 被 Roll Back 后，先前执行过的 Condition Action（含推数据给 CRM 的 Action）不自动撤销。当该 Stage **重新 Complete 时**，`EvaluateAndExecuteStageConditionAsync` 正常重新触发，与普通完成流程保持一致。

---

## Glossary

- **Stage**：Workflow 中的一个步骤节点，包含表单、Checklist 等组件，由 `ff_stage` 表存储。
- **Onboarding**：客户入驻流程实例（Case），`ff_onboarding` 表存储，`stages_progress_json`（jsonb）记录每个 Stage 的进度。
- **OnboardingStageProgress**：存储在 `ff_onboarding.stages_progress_json` 中的 JSON 对象，记录单个 Stage 在某个 Onboarding 中的状态（Status、IsCompleted 等）。
- **Roll Back**：将某个 Completed Stage 的进度状态从 `Completed` 重置为 `InProgress`，使其可重新编辑。
- **RollBackTeams**：Stage 配置中新增字段，JSONB 数组，记录允许执行 Roll Back 操作的团队列表。若为空，则不允许任何人执行 Roll Back（与 OperateTeams 为空表示所有人可操作的语义不同，此处为安全默认值）。
- **System**：FlowFlex 后端服务（.NET 8 WebApi）。
- **Portal**：FlowFlex 前端 Vue 3 应用。
- **ConditionAction**：Stage 完成时触发的自动化操作，如推数据到 CRM、跳转到指定 Stage 等。
- **StagePermissionService**：现有后端服务，负责校验用户对 Stage 的操作权限。

---

## Requirements

### Requirement 1：Stage Roll Back 核心操作

**User Story：** 作为有权限的内部用户，我希望能将某个已 Completed 的 Stage 重新打开，以便修正其中的填写内容，再重新提交完成。

#### Acceptance Criteria

1. WHEN 用户对某个 Onboarding 中状态为 `Completed` 的 Stage 发起 Roll Back 请求，THE System SHALL 将该 Stage 的 `OnboardingStageProgress.Status` 从 `Completed` 重置为 `InProgress`，并将 `IsCompleted` 设置为 `false`，`CompletionTime` 清空，`CompletedBy` 及 `CompletedById` 清空。

2. WHEN Roll Back 操作成功，THE System SHALL 将该 Stage 设置为当前活动 Stage（`IsCurrent = true`），并更新 Onboarding 的 `CurrentStageId` 为该 Stage 的 ID。

3. IF 目标 Stage 的状态不是 `Completed`，THEN THE System SHALL 返回业务错误，提示"只能对已完成的 Stage 执行 Roll Back 操作"。

4. IF 目标 Stage 不属于该 Onboarding 对应的 Workflow，THEN THE System SHALL 返回业务错误，提示"Stage 不属于当前 Workflow"。

5. IF Onboarding 整体状态为 `Completed`，THEN THE System SHALL 在执行 Roll Back 时将 Onboarding 状态重置为 `InProgress`，并清空 `ActualCompletionDate`。

---

### Requirement 2：Roll Back 权限控制

**User Story：** 作为 Workflow 管理员，我希望为每个 Stage 单独配置哪些团队可以执行 Roll Back，避免未授权的用户随意回退已完成的步骤。

#### Acceptance Criteria

1. THE System SHALL 在 `ff_stage` 表中支持 `roll_back_teams` 字段（jsonb），存储允许执行 Roll Back 的团队 ID 列表。

2. WHEN 用户发起 Roll Back 请求，THE System SHALL 检查执行者所属团队是否包含在目标 Stage 的 `RollBackTeams` 中。

3. IF 目标 Stage 的 `RollBackTeams` 为空或 null，THEN THE System SHALL 拒绝 Roll Back 操作并返回权限错误，提示"该 Stage 未配置 Roll Back 权限"。

4. IF 执行者所属团队不在 `RollBackTeams` 中，THEN THE System SHALL 拒绝 Roll Back 操作并返回权限错误，提示"您没有执行此操作的权限"。

5. WHERE Stage 配置界面启用，THE Portal SHALL 在 Stage 设置页面提供 `RollBackTeams` 的配置入口（与现有 `OperateTeams`、`ViewTeams` 配置方式一致）。

---

### Requirement 3：Roll Back 后通知机制

**User Story：** 作为 Stage 负责人或后续 Stage 的处理人，我希望在某个 Stage 被 Roll Back 后第一时间收到通知，以便知晓流程状态发生变化。

#### Acceptance Criteria

1. WHEN Roll Back 操作成功，THE System SHALL 向被 Roll Back 的 Stage 的当前 Assignee（`OnboardingStageProgress.CustomStageAssignee` 或 `Assignee`）发送通知，内容包含：Onboarding 名称、被 Roll Back 的 Stage 名称、执行者姓名、执行时间。

2. WHEN Roll Back 操作成功，THE System SHALL 向所有后续 Stage（Order 大于被 Roll Back Stage 的所有非 Skipped Stage）的 Assignee 发送通知，告知前置 Stage 已重新打开，当前工作可能受到影响。

3. IF 某个 Stage 的 Assignee 列表为空，THEN THE System SHALL 跳过该 Stage 的通知，不产生错误。

4. WHILE 发送通知失败（网络或邮件服务异常），THE System SHALL 记录错误日志，但不阻断 Roll Back 操作本身的成功响应。

---

### Requirement 4：操作日志记录

**User Story：** 作为审计员，我希望在操作日志中查看 Roll Back 的完整记录，包括谁在什么时候对哪个 Stage 执行了 Roll Back。

#### Acceptance Criteria

1. WHEN Roll Back 操作成功，THE System SHALL 以 `StageReopen`（`OperationTypeEnum = 10`）类型向操作变更日志写入一条记录，包含：操作者 ID、操作者姓名、Onboarding ID、Stage ID、Stage 名称、操作时间。

2. THE System SHALL 在日志记录中包含 Roll Back 原因（由请求者可选填写的 `Reason` 字段提供；若未填写，记录为空）。

3. WHEN 查询某 Onboarding 的操作日志时，THE System SHALL 将 Roll Back 日志条目包含在结果中，供前端日志面板展示。

---

### Requirement 5：Stage 重新 Complete 后 Condition Action 重新触发

**User Story：** 作为系统集成负责人，我希望被 Roll Back 的 Stage 在重新 Complete 后，其配置的 Condition Action（含推送 CRM 数据的 Action）能正常重新触发，以保证下游系统数据一致性。

#### Acceptance Criteria

1. WHEN 被 Roll Back 的 Stage 重新被用户 Complete，THE System SHALL 按照现有 `CompleteCurrentStageAsync` 流程，在标记 Stage 为 Completed 之前调用 `EvaluateAndExecuteStageConditionAsync`，重新触发该 Stage 的所有 Condition Action。

2. THE System SHALL 不为 Roll Back 本身实现任何 "撤销 Condition Action" 的逻辑（如撤销已推送到 CRM 的数据），以降低实现复杂度。

3. IF Condition Action 在重新触发时执行失败，THEN THE System SHALL 按现有错误处理流程阻断 Stage 完成，并向调用方返回业务错误。

---

### Requirement 6：前端 Roll Back 入口

**User Story：** 作为内部处理人员，我希望在 Case 详情页的 Stage 卡片上看到 Roll Back 按钮（当 Stage 已完成且我有权限时），以便快速发起操作。

#### Acceptance Criteria

1. WHEN 某个 Stage 处于 `Completed` 状态，THE Portal SHALL 在该 Stage 卡片或详情区域展示"Roll Back"操作入口（按钮/菜单项）。

2. WHEN 当前用户所属团队不在目标 Stage 的 `RollBackTeams` 中，THE Portal SHALL 隐藏或禁用该 Roll Back 入口（通过后端返回的权限信息判断，避免多余的权限 API 调用）。

3. WHEN 用户点击 Roll Back 按钮，THE Portal SHALL 弹出二次确认对话框，包含可选的"Roll Back 原因"文本输入框，用户确认后发起 API 请求。

4. WHEN Roll Back API 请求成功，THE Portal SHALL 刷新该 Stage 的状态显示，将其展示为 `InProgress` 状态。

5. IF Roll Back API 请求失败，THE Portal SHALL 展示错误提示信息，提示内容来源于 API 返回的错误信息。
