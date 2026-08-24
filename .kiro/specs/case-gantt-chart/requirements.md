# Requirements Document

## Introduction

为 FlowFlex（WFE）系统的 Case 模块新增甘特图时间线视图。每个 Case 由多个按序推进的 Stage 组成，当前系统仅能看到当前 Stage 的进度条，无法直观评估整体健康度、时间偏差和风险分布。

本功能在不改动任何现有流程的前提下，增量添加甘特图视图，提供三个访问入口（列表页悬停预览、列表页弹出完整图、详情页 Gantt 按钮），使管理者能通过统一的时间线视角快速判断 Case 整体进度与风险。

---

## Glossary

- **Case**：一个客户/Lead 的 Onboarding 流程实例，对应 `ff_onboarding` 表中的记录
- **Stage**：Workflow 中的一个步骤节点，对应 `ff_stage` 表中的记录
- **StageProgress**：某个 Case 中某个 Stage 的执行进度记录，存储于 `ff_onboarding.stages_progress_json` JSONB 列
- **Planned Time**：原始计划时间，Case 启动时计算一次，此后不再变更
- **Projected Time**：当前预测时间，每次 Stage 推进后动态重算
- **Actual Time**：实际时间，Stage 真正开始或完成时记录（映射自现有 `StartTime` / `CompletionTime`）
- **GanttChart**：甘特图组件，以时间轴形式展示所有 Stage 的 Planned / Projected / Actual 三组时间线
- **GanttSummaryHeader**：甘特图顶部 Case 汇总区域
- **StagePopover**：点击 Stage 条时弹出的详情浮层
- **GanttThumbnail**：列表页悬停时显示的只读缩略甘特图（约 400×180px）
- **Component**：附加在 Stage 上的可配置项，类型包括 Checklist、Questionnaire、Fields、Files、Quick Link
- **ComponentWeight**：Stage 中每个 Component 在完成度计算中的权重占比，配置存储于 `ff_stage.component_weights`
- **CompletionPercentage**：Stage 完成度百分比（0-100），按 ComponentWeight 加权计算，查询时实时计算，不持久化
- **GanttStageStatus**：甘特图专属的 Stage 状态枚举，包含 NotStarted / Delayed / InProgress / Overdue / Completed 五种值；与现有系统 Stage 状态（Pending/InProgress/Completed/Skipped 等）独立，不覆盖或替换现有状态
- **Blocker**：Stage 被手动标记的阻塞状态，通过 `isBlocked` 字段表达，叠加在 GanttStageStatus 上，不是独立枚举值
- **BlockerHistory**：一个 Stage 的所有历史阻塞记录的数组
- **InheritedDelayDays**：继承延迟天数，= Actual Start - Planned Start
- **OwnVarianceDays**：自身执行偏差，= Actual Duration - Planned Duration
- **TotalVarianceDays**：总偏差天数，= Actual End - Planned End
- **SLA_Days**：Stage 级别的 SLA 要求天数，超出则触发 Overdue
- **OnboardingStartedEvent**：Case 调用 `/start` 接口后通过 MediatR 发布的领域事件，触发 Planned 时间初始化
- **OnboardingStageCompletedEvent**：现有事件，Stage 完成时发布，甘特图 Handler 复用此事件触发 Projected 重算
- **OnboardingStageMovedEvent**：新建事件，Stage move-to-stage 时发布，与 OnboardingStageCompletedEvent 共用同一个 GanttProjectedTimeRecalcHandler
- **GanttProjectedTimeRecalcHandler**：新建 MediatR Handler，监听 OnboardingStageCompletedEvent 和 OnboardingStageMovedEvent，负责重算所有 Stage 的 Projected 时间并持久化
- **Legend**：甘特图底部始终可见的颜色图例区域
- **GanttTour**：用户首次打开完整甘特图时显示的 5 步引导，可跳过，按 userId 记录是否已看过

---

## Requirements

### Requirement 1：甘特图数据模型扩展

**User Story：** 作为系统，我需要在 StageProgress 和 Stage 表中扩展字段，以支持 Planned / Projected / Actual 三组时间和阻塞管理数据。

#### Acceptance Criteria

1. THE System SHALL 在 `ff_onboarding.stages_progress_json` 中为每个 StageProgress 记录新增以下字段：`plannedStartDate`、`plannedEndDate`、`projectedStartDate`、`projectedEndDate`、`inheritedDelayDays`、`ownVarianceDays`、`totalVarianceDays`、`isBlocked`、`blockerHistory`，其中 `blockerHistory` 为数组，每条记录包含 `blockerReason`（必填）、`blockerStartDate`、`expectedResolutionDate`（可选）、`blockerResolvedDate`（可选）、`resolutionNotes`（可选）、`blockedDays` 字段

2. THE System SHALL 在 `ff_stage` 表中新增 `sla_days`（Integer）和 `component_weights`（JSONB）两列，并通过 Migration 变更数据库结构

3. THE System SHALL 将 `component_weights` 存储为数组格式：`[{ "type": "checklist|questionnaire|fields|files|quickLink", "id": "...", "name": "...", "weight": 40 }]`，且单个 Stage 所有 Component 的 `weight` 之和在写入时由 API 层验证等于 100

4. IF 某 Stage 的 `component_weights` 为空或未配置，THEN THE System SHALL 在计算 CompletionPercentage 时对该 Stage 的所有已配置 Component 平均分配权重

5. THE System SHALL 在 `ff_onboarding` 表中新增 `total_variance_days`（Integer）列，通过 Migration 变更数据库结构

6. THE System SHALL 复用现有 `Stage.EstimatedDuration`（decimal）字段作为 `estimated_duration_days` 的数据来源，使用 `Math.Round` 取整，不新增数据库列

---

### Requirement 2：Planned 时间初始化

**User Story：** 作为系统，我需要在 Case 启动时为所有 Stage 计算并写入 Planned 时间，且 Planned 时间此后不再变更，以便后续对比分析。

#### Acceptance Criteria

1. WHEN Case 调用 `/start` 接口且状态由 Inactive 变为 Active，THE System SHALL 通过 MediatR 发布 `OnboardingStartedEvent`，并由专用 Handler 在同一事务上下文中为该 Case 所有 Stage 写入 `plannedStartDate` 和 `plannedEndDate`

2. THE System SHALL 按以下规则计算 Planned 时间：Stage 1 的 `plannedStartDate` = `Case.StartDate`；Stage 1 的 `plannedEndDate` = `plannedStartDate` + `EstimatedDuration`（天数取整）；Stage N（N > 1）的 `plannedStartDate` = Stage(N-1) 的 `plannedEndDate` + 1 天；Stage N 的 `plannedEndDate` = `plannedStartDate` + `EstimatedDuration`（天数取整）- 1 天

3. IF 某 Stage 的 `EstimatedDuration` 为 null 或 0，THEN THE System SHALL 采用 fallback 策略：优先按 Case ETA - StartDate 均分到所有 Stage 天数；IF Case 亦无 ETA，THEN 默认每个 Stage 7 天

4. WHILE Planned 时间已写入，THE System SHALL 拒绝任何来源对 `plannedStartDate` 和 `plannedEndDate` 的修改，并返回 400 错误

5. IF Case 已存在且 `plannedStartDate` 为 null（旧 Case），THEN THE GanttDataService SHALL 在 GET `/ow/onboardings/v1/{id}/gantt` 查询时动态推算 Planned 时间，推算结果不持久化到数据库

---

### Requirement 3：Projected 时间动态重算

**User Story：** 作为系统，我需要在每次 Stage 推进后重算所有后续 Stage 的 Projected 时间，使管理者能看到当前最新的预测完成时间。

#### Acceptance Criteria

1. WHEN `OnboardingStageCompletedEvent` 或 `OnboardingStageMovedEvent` 被发布，THE `GanttProjectedTimeRecalcHandler` SHALL 重算该 Case 所有 Stage（包括已完成 Stage 之后的所有 Stage）的 `projectedStartDate` 和 `projectedEndDate`，并将结果持久化回 `stages_progress_json`

2. THE System SHALL 按以下逻辑推算每个 Stage 的 Projected 时间：IF Stage(N-1) 已完成，THEN Stage N 的 `projectedStartDate` = Stage(N-1) 的 `actualEndDate` + 1 天；IF Stage(N-1) InProgress，THEN Stage N 的 `projectedStartDate` = 今天 + 基于 Stage(N-1).completionPercentage 估算的剩余天数（= EstimatedDuration × (1 - completionPercentage / 100)，取整）；IF Stage(N-1) 未开始，THEN Stage N 的 `projectedStartDate` = Stage(N-1).`projectedEndDate` + 1 天；所有情况下 Stage N 的 `projectedEndDate` = `projectedStartDate` + EstimatedDuration（取整）- 1 天

3. WHILE 某 Stage 的 `isBlocked` 为 true，THE System SHALL 将该 Stage 及其所有后续 Stage 的 `projectedStartDate` 和 `projectedEndDate` 设为 null

4. WHEN 某 Stage 的 Blocked 状态被解除，THE `GanttProjectedTimeRecalcHandler` SHALL 重新触发 Projected 重算，恢复 null 值为有效预测时间

---

### Requirement 4：CompletionPercentage 实时计算

**User Story：** 作为系统，我需要在甘特图 API 查询时实时计算每个 Stage 的加权完成百分比，使甘特图能展示真实的 Stage 完成度。

#### Acceptance Criteria

1. WHEN `GET /ow/onboardings/v1/{id}/gantt` 被调用，THE GanttDataService SHALL 为每个 Stage 实时计算 `completionPercentage`，计算结果不持久化到数据库

2. THE System SHALL 按公式 `completionPercentage = Σ(componentWeight × componentCompletion)` 计算，其中各 Component 类型的 `componentCompletion` 按以下规则计算：Checklist = 已完成 Tasks / 总 Tasks；Questionnaire = 已填写 Questions / 总 Required Questions；Fields = 已填写 Required Fields / 总 Required Fields；Files = 已上传文件数 / 最小要求数（无最小要求则不计入）；Quick Link = 默认权重 0，不计入完成度

3. IF 某 Stage 没有任何 Component 被配置，THEN THE System SHALL 返回该 Stage 的 `completionPercentage` = 0

4. IF Questionnaire 中 Required Questions 总数为 0，THEN THE System SHALL 视该 Questionnaire 的 componentCompletion 为 100%

---

### Requirement 5：GanttStageStatus 状态派生

**User Story：** 作为系统，我需要在甘特图 API 响应中为每个 Stage 计算并返回 GanttStageStatus，使前端能正确渲染颜色和状态 Badge。

#### Acceptance Criteria

1. WHEN `GET /ow/onboardings/v1/{id}/gantt` 被调用，THE GanttDataService SHALL 为每个 StageProgress 记录派生 `ganttStatus` 字段，值为以下五种之一：`NotStarted`、`Delayed`、`InProgress`、`Overdue`、`Completed`

2. THE System SHALL 按以下规则派生 `ganttStatus`：IF Stage 已完成（StageProgress.IsCompleted = true），THEN `Completed`；IF Stage 已实际开始（actualStartDate 不为 null）且 `今日 > plannedEndDate`，THEN `Overdue`；IF Stage 已实际开始且 `今日 ≤ plannedEndDate`，THEN `InProgress`；IF Stage 未开始且 `今日 > plannedStartDate`，THEN `Delayed`；否则 `NotStarted`

3. THE System SHALL 在 GanttStageItem 响应中同时返回 `isBlocked` 字段，前端据此决定是否叠加 Blocked 视觉样式，`isBlocked` 与 `ganttStatus` 正交，不互相替换

4. THE System SHALL 在 GanttStageItem 响应中为 `Completed` 状态的 Stage 返回 `inheritedDelayDays`、`ownVarianceDays`、`totalVarianceDays` 三个偏差分析字段，计算公式：`inheritedDelayDays` = actualStartDate - plannedStartDate（天数）；`ownVarianceDays` = actualDuration - estimatedDuration（天数）；`totalVarianceDays` = actualEndDate - plannedEndDate（天数）

---

### Requirement 6：甘特图数据查询 API

**User Story：** 作为前端，我需要一个单一接口获取某个 Case 的完整甘特图数据，包括 Case 汇总信息和所有 Stage 详情。

#### Acceptance Criteria

1. THE System SHALL 提供 `GET /ow/onboardings/v1/{id}/gantt` 接口，返回 `GanttDataResponse` 对象，包含 `summary`（GanttCaseSummary）和 `stages`（GanttStageItem[]）两个字段

2. THE `GanttCaseSummary` SHALL 包含以下字段：`onboardingId`、`caseName`、`caseCode`、`workflowName`、`status`、`priority`、`plannedStartDate`、`plannedEndDate`、`projectedEndDate`、`actualStartDate`、`actualEndDate`、`overallCompletionPercentage`（所有 Stage 的加权平均）、`totalStages`、`completedStages`、`overdueStages`、`delayedStages`、`blockedStages`、`currentStageName`、`currentStageOrder`

3. THE `GanttStageItem` SHALL 包含以下字段：`stageId`、`stageName`、`stageOrder`、`color`、`isRequired`、`ganttStatus`、`isBlocked`、`assignee`（用户名数组）、`coAssignees`（用户名数组）、`plannedStartDate`、`plannedEndDate`、`projectedStartDate`（可为 null）、`projectedEndDate`（可为 null）、`actualStartDate`（可为 null）、`actualEndDate`（可为 null）、`estimatedDurationDays`、`completionPercentage`、`inheritedDelayDays`（可为 null）、`ownVarianceDays`（可为 null）、`totalVarianceDays`（可为 null）、`isBlocked`、`blockedDays`、`blockReason`（当前 Blocker 原因，可为 null）、`expectedResolutionDate`（可为 null）、`components`（Component 完成统计）、`lastSavedBy`、`lastSavedAt`

4. THE `GanttStageItem.components` SHALL 包含以下子字段：`checklistsTotal`、`checklistsCompleted`、`questionnairesTotal`、`questionnairesSubmitted`、`fieldsTotal`、`fieldsFilled`、`filesUploaded`

5. IF 请求的 Case ID 不存在或已被软删除，THEN THE System SHALL 返回 404 错误

6. THE System SHALL 对 `GET /ow/onboardings/v1/{id}/gantt` 接口应用现有的 Case 查看权限校验，无查看权限的用户收到 403 错误

7. THE System SHALL 在 API 响应中以 ISO 8601 格式返回所有日期字段，由前端转换为 MM/DD/YYYY 显示格式

---

### Requirement 7：Blocked 状态管理 API

**User Story：** 作为 Assignee 或管理者，我需要手动标记 Stage 为 Blocked 并填写阻塞原因，以及手动解除 Blocked 状态，使其他人能了解当前阻塞情况。

#### Acceptance Criteria

1. THE System SHALL 提供 `POST /ow/onboardings/v1/{id}/block-stage` 接口，必填参数为 `stageId` 和 `blockerReason`，可选参数为 `expectedResolutionDate`

2. WHEN `POST /ow/onboardings/v1/{id}/block-stage` 被调用，THE System SHALL 将对应 StageProgress 的 `isBlocked` 设为 true，并向 `blockerHistory` 追加一条新记录，记录包含 `blockerReason`、`blockerStartDate`（= 当前时间）、`expectedResolutionDate`（若传入）

3. THE System SHALL 提供 `POST /ow/onboardings/v1/{id}/unblock-stage` 接口，必填参数为 `stageId`，可选参数为 `resolutionNotes`

4. WHEN `POST /ow/onboardings/v1/{id}/unblock-stage` 被调用，THE System SHALL 将对应 StageProgress 的 `isBlocked` 设为 false，并更新 `blockerHistory` 中最新一条记录的 `blockerResolvedDate`（= 当前时间）、`blockedDays`（= resolvedDate - startDate，取整）、`resolutionNotes`（若传入）

5. IF 调用 `block-stage` 时该 Stage 的 `isBlocked` 已为 true，THEN THE System SHALL 返回 400 错误，提示 "Stage is already blocked"

6. IF 调用 `unblock-stage` 时该 Stage 的 `isBlocked` 为 false，THEN THE System SHALL 返回 400 错误，提示 "Stage is not blocked"

7. WHEN Blocked 状态成功解除，THE System SHALL 触发 `GanttProjectedTimeRecalcHandler` 重算该 Case 所有后续 Stage 的 Projected 时间

8. THE System SHALL 对 `block-stage` 和 `unblock-stage` 接口应用现有的 Case 操作权限校验，无操作权限的用户收到 403 错误

---

### Requirement 8：列表页缩略甘特图预览

**User Story：** 作为管理者，我需要在 Case 列表页鼠标悬停某行时看到该 Case 的缩略甘特图，以便快速了解整体进度而无需进入详情页。

#### Acceptance Criteria

1. WHEN 用户鼠标在某 Case 行上持续悬停 500ms，THE GanttThumbnail SHALL 在该行附近显示缩略预览弹出层

2. THE `GanttThumbnail` SHALL 显示以下信息：Case 名称、Workflow 名称、"X of Y Stages" 进度文本、Overall 单条进度条（颜色按 Case 整体健康度）、Case Start Date、Case ETA、当前 Stage 名称及 Assignee 姓名

3. THE `GanttThumbnail` SHALL 尺寸约为 400×180px，为只读视图，不提供任何可点击元素（除 "View Full Chart" 按钮外）

4. WHEN 用户点击 `GanttThumbnail` 上的 "View Full Chart" 按钮或缩略图本体，THE GanttModal SHALL 弹出完整甘特图

5. WHEN 用户鼠标移出 Case 行或缩略预览区域，THE GanttThumbnail SHALL 自动隐藏

6. THE System SHALL 使用 debounce（500ms）防止频繁触发缩略图加载，缩略图数据复用列表页已有数据或通过轻量计算派生，不影响列表页渲染性能

7. WHERE Case 列表支持分页或虚拟滚动，THE GanttThumbnail SHALL 不影响列表的滚动和渲染性能

---

### Requirement 9：完整甘特图 — Case 汇总 Header

**User Story：** 作为管理者，我需要在完整甘特图顶部看到 Case 级别的关键汇总信息，包括原始计划与当前预测的对比，以便快速判断整体偏差。

#### Acceptance Criteria

1. THE `GanttSummaryHeader` SHALL 显示以下字段：Case 名称与编码、Workflow 名称、进度文本（"X of Y Stages"）、Case Start Date（MM/DD/YYYY）、Planned End Date（MM/DD/YYYY）、Projected End Date（MM/DD/YYYY）、偏差天数（格式："+N days" 或 "-N days"）

2. THE `GanttSummaryHeader` SHALL 同时展示 "PLANNED（Original Plan）" 区块和 "PROJECTED（Current Forecast）" 区块，两块内容并排显示，各自包含 Start Date 和 End Date

3. WHEN Projected End Date > Planned End Date，THE `GanttSummaryHeader` SHALL 在偏差天数旁使用警告色（Orange/Red）显示正偏差值

4. WHEN Projected End Date ≤ Planned End Date，THE `GanttSummaryHeader` SHALL 在偏差天数旁使用绿色显示零或负偏差值

5. THE `GanttSummaryHeader` 中的 "TIMELINE"、"PLANNED" 和 "PROJECTED" 标签旁各自显示 ⓘ 图标，WHEN 用户 hover 该图标 300ms，THE System SHALL 显示 Tooltip 解释对应术语含义

---

### Requirement 10：完整甘特图 — Stage 时间轴列表

**User Story：** 作为管理者，我需要在甘特图中看到所有 Stage 按 Workflow 顺序排列的时间轴条，每个 Stage 同时展示 Planned 和 Projected 双时间线，以便直观对比。

#### Acceptance Criteria

1. THE `GanttChart` SHALL 按 Workflow 中的 `order_index` 升序排列所有 Stage 行，每行包含：Stage 序号、Stage 名称、Assignee（多人时显示第一个 + "+N"）、GanttStageStatus Badge、Required/Optional 标记

2. THE `GanttChart` SHALL 在时间轴区域为每个 Stage 渲染 Planned 时间条（灰色虚线框样式，30-50% 透明度，仅在 Projected ≠ Planned 时显示）和 Projected 时间条（实色，100% 不透明度，按 GanttStageStatus 颜色渲染）

3. THE `GanttChart` SHALL 在时间轴上显示 Today 垂直参考线，标记当前日期

4. THE `GanttChart` SHALL 支持 Day / Week / Month 三种时间刻度视图切换，用户点击顶部 View 选择器切换

5. THE `GanttChart` SHALL 支持时间轴导航：点击 "Today" 按钮滚动时间轴到当前日期；点击 "< Prev" 和 "Next >" 按钮分别向前和向后移动时间窗口

6. THE `GanttChart` 甘特图纵向尺寸应自适应 Stage 数量，WHEN Stage 数超过 10 个，THE `GanttChart` SHALL 支持纵向滚动

7. THE `GanttChart` SHALL 支持按 GanttStageStatus 筛选 Stage 行（下拉多选），以及按 Assignee 姓名筛选 Stage 行（下拉多选）

8. THE `GanttChart` 的 InProgress Stage 行 SHALL 显示特殊视觉高亮（蓝色边框或发光效果），Overdue Stage 行 SHALL 显示红色视觉样式和警告图标

---

### Requirement 11：Status Badge 与状态解释

**User Story：** 作为管理者，我需要在每个 Stage 的状态 Badge 上看到清晰的状态标识，并通过 Hover Tooltip 获得用户友好的解释，而不需要记忆颜色含义。

#### Acceptance Criteria

1. THE `GanttChart` SHALL 按以下颜色映射渲染 GanttStageStatus Badge：NotStarted → Gray (#D9D9D9)；Delayed → Orange (#FA8C16)；InProgress → Blue (#1890FF)；Overdue → Red (#FF4D4F)；Completed（按时或提前）→ Green (#52C41A)；Completed（延迟）→ Orange (#FA8C16)；Blocked（叠加）→ Purple (#722ED1) 条纹或覆盖层

2. WHEN GanttStageStatus 为 Overdue / Delayed / Completed / Blocked 时，THE Status Badge SHALL 显示 ⓘ 图标；WHEN 用户 hover ⓘ 图标 300ms，THE System SHALL 显示 Tooltip，内容按如下规则生成：Overdue Tooltip 包含实际开始时间和已超期天数；Delayed Tooltip 说明等待上游 Stage 完成及计划开始时间；Blocked Tooltip 包含阻塞原因、阻塞开始日期和已阻塞天数

3. WHEN GanttStageStatus 为 Completed，THE Status Badge SHALL 显示偏差天数标注（格式："+N days"、"-N days" 或 "on time"），WHEN 用户 hover Status Badge，THE System SHALL 显示 Tooltip，按以下 7 种场景生成自然语言说明：
   - 按时开始且提前完成 → "Finished N days ahead of schedule"
   - 按时开始且按时完成 → "Finished as planned"
   - 按时开始且自己超时 → "Took N days longer than planned"
   - 延迟开始且追回 → "Started N days late, finished N days faster"
   - 延迟开始且部分追回 → "Started N days late, finished N days faster"（净 +M days）
   - 延迟开始且未超时 → "Started N days late, finished as planned"
   - 延迟开始且自己也超时 → "Started N days late, took N days longer"

4. WHEN GanttStageStatus 为 NotStarted 或 InProgress，THE Status Badge SHALL 不显示 ⓘ 图标，不需要额外解释

---

### Requirement 12：Stage Popover 详情

**User Story：** 作为管理者，我需要点击某个 Stage 条后看到该 Stage 的完整详细信息，包括时间线、Components 完成情况和阻塞信息，以便了解具体进展。

#### Acceptance Criteria

1. WHEN 用户单击甘特图中某个 Stage 条，THE `StagePopover` SHALL 弹出，显示该 Stage 的详情

2. THE `StagePopover` SHALL 包含以下区块和字段：
   - **基础信息**：Stage 序号与名称、Required/Optional 标记、GanttStageStatus
   - **Assignee**：主负责人姓名（邮箱），若有 Co-Assignees 则显示 "+ N more"
   - **Timeline**：Planned Start、Planned End（ETA）、Actual Start、Actual End（未完成时显示 "--"）、Planned Duration（天数）、Days Elapsed（已实际经过天数）、Days Remaining（仅 InProgress/Overdue 时显示）
   - **Components**：CompletionPercentage 百分比、Checklists（已完成 / 总数）、Questionnaires（已提交 / 总数）、Fields（已填写 / 总数）、Files（已上传数量）
   - **Blockers**：IF `isBlocked` 为 true，THEN 显示当前阻塞原因、阻塞开始日期、预计解除日期和 "Resolve" 按钮

3. THE `StagePopover` SHALL 显示 "Go to Stage" 按钮

4. WHEN 用户双击甘特图中某个 Stage 条，THE GanttModal SHALL 关闭，页面导航到 Case 详情页并滚动到对应 Stage

5. WHEN 用户在 `StagePopover` 中点击 "Mark as Blocked"，THE System SHALL 显示 Blocked 标记弹窗

6. WHEN 用户在 `StagePopover` 中点击 "Resolve"，THE System SHALL 显示 Blocker 解除弹窗

---

### Requirement 13：Blocked 状态 UI 操作

**User Story：** 作为 Assignee 或管理者，我需要在 Case 详情页和甘特图中都能手动标记/解除 Stage 的阻塞状态，确保阻塞信息能及时更新。

#### Acceptance Criteria

1. THE Case 详情页当前 Stage 的操作菜单 SHALL 新增 "Mark as Blocked" 入口，WHEN 用户点击，THE System SHALL 弹出 Blocked 标记弹窗

2. THE Blocked 标记弹窗 SHALL 包含：Stage 名称（只读）、Blocker Reason 文本输入框（必填，不超过 500 字符）、Expected Resolution Date 日期选择器（可选）、Cancel 按钮和 "Mark as Blocked" 确认按钮

3. WHEN 用户提交 Blocked 标记弹窗且 Blocker Reason 为空，THE System SHALL 显示必填校验提示，不提交请求

4. THE Blocker 解除弹窗 SHALL 包含：Stage 名称（只读）、Blocked Since 日期和已阻塞天数（只读）、阻塞原因（只读）、Resolution Notes 文本输入框（可选）、Cancel 按钮和 "Resolve Blocker" 确认按钮

5. WHEN Blocked 标记操作成功，THE System SHALL 刷新甘特图数据，将该 Stage 条显示为 Blocked 视觉样式（Purple 覆盖层 + "BLOCKED" 标记）

6. WHEN Blocker 解除操作成功，THE System SHALL 刷新甘特图数据，恢复该 Stage 的正常 GanttStageStatus 视觉样式

---

### Requirement 14：Case 详情页 Gantt 入口

**User Story：** 作为管理者，我需要在 Case 详情页顶部操作栏看到 Gantt 按钮，点击后直接打开该 Case 的完整甘特图。

#### Acceptance Criteria

1. THE Case 详情页顶部操作栏 SHALL 新增 "Gantt" 按钮，与现有 History、Export、Edit Details 等按钮并排

2. WHEN 用户点击 "Gantt" 按钮，THE System SHALL 打开完整甘特图（模态框或可展开面板形式）

3. THE Gantt 入口按钮 SHALL 对所有有权查看该 Case 详情的用户可见，不设额外权限限制

---

### Requirement 15：Legend 说明区与首次引导 Tour

**User Story：** 作为初次使用的管理者，我需要甘特图提供清晰的图例和首次使用引导，帮助我快速理解各状态颜色和 Planned/Projected 术语的含义。

#### Acceptance Criteria

1. THE 完整甘特图 SHALL 在底部始终显示 Legend 区域，包含所有 GanttStageStatus 的颜色图例说明（颜色块 + 状态名 + 简短描述）、Planned 条形样式说明（灰色虚线 = 原始计划）和 Projected 条形样式说明（实色 = 当前预测）

2. THE Legend 区域 SHALL 支持收起/展开操作

3. WHEN 用户首次打开完整甘特图（按 userId 判断，记录存储于 StageProgress.TourSeenBy 字段复用位置，或独立存储于 ff_onboarding 的扩展字段中），THE System SHALL 自动显示 5 步引导 Tour

4. THE 5 步 Tour SHALL 依次高亮以下区域并配文字说明：Step 1 = Case Summary 区域；Step 2 = Planned vs Projected Timeline 对比区域；Step 3 = Stage 时间轴条（Planned 虚线 vs Projected 实线）；Step 4 = Status Badge 和 ⓘ 图标；Step 5 = Legend 区域

5. THE Tour 每一步 SHALL 提供 "Skip"（跳过全部）和 "Next"（下一步）按钮，最后一步提供 "Got it" 完成按钮

6. WHEN 用户点击 "Skip" 或 "Got it"，THE System SHALL 记录该 userId 已完成 Tour，后续打开甘特图不再自动弹出 Tour

7. THE 完整甘特图右上角 SHALL 显示 "[? Help]" 按钮，WHEN 用户点击，THE System SHALL 显示完整帮助面板，内容包含 Planned/Projected 术语解释、各 Status 含义说明和操作 Tips

---

### Requirement 16：甘特图权限与访问控制

**User Story：** 作为系统，我需要确保甘特图功能遵从现有 Case 权限模型，有查看权限的用户才能访问甘特图数据，有操作权限的用户才能执行 Blocked 操作。

#### Acceptance Criteria

1. THE System SHALL 对 `GET /ow/onboardings/v1/{id}/gantt` 接口复用现有 `IOnboardingPermissionService.EnsureCaseViewPermissionAsync` 权限校验

2. THE System SHALL 对 `POST /ow/onboardings/v1/{id}/block-stage` 和 `POST /ow/onboardings/v1/{id}/unblock-stage` 接口复用现有 `IOnboardingPermissionService.EnsureCaseOperatePermissionAsync` 权限校验

3. THE 缩略甘特图预览 SHALL 仅对有权查看对应 Case 的用户显示，无权限时列表行不触发预览 tooltip

---

### Requirement 17：向后兼容性

**User Story：** 作为系统，我需要确保甘特图功能的新增字段和逻辑不破坏现有 Case、Stage、OnboardingStageProgress 的任何已有功能。

#### Acceptance Criteria

1. THE System SHALL 确保对 `stages_progress_json` 新增字段采用向后兼容的方式（字段可为 null），现有 Case 记录的 StageProgress 读取不受影响

2. THE System SHALL 确保新增的 `ff_stage.sla_days`、`ff_stage.component_weights`、`ff_onboarding.total_variance_days` 列，通过 Migration 以 nullable 或有默认值的方式添加，不影响现有记录

3. THE System SHALL 确保 GanttStageStatus 枚举仅在甘特图响应中使用，不替换也不影响现有系统中 `OnboardingStageProgress.Status`（Pending/InProgress/Completed/Skipped/Rejected/Terminated）的任何逻辑

4. IF 某 Case 的 `plannedStartDate` 为 null（旧 Case），THE GanttDataService SHALL 动态推算 Planned 时间后正常返回数据，前端不感知差异
