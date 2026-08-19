# Implementation Plan: Case Gantt Chart

## Overview

为 FlowFlex WFE Case 模块新增甘特图时间线视图。本计划分 9 个阶段，从后端数据模型出发，逐步构建时间推算引擎、API 层、前端组件，最后完成集成与测试。设计采用零破坏原则，所有新增字段均 nullable，不影响现有 Case 流程。

## Tasks

---

### Phase 1：数据模型与基础设施（后端）

- [x] 1. 扩展 OnboardingStageProgress、Stage、Onboarding 实体及数据库 Migration
  - [x] 1.1 扩展 `OnboardingStageProgress.cs`，新增 Gantt 时间字段与 Blocked 字段
    - 文件：`Domain/Entities/OW/OnboardingStageProgress.cs`
    - 新增字段（全部 nullable，无需 Migration，已序列化进 JSONB）：
      - `PlannedStartDate`、`PlannedEndDate`（`DateTimeOffset?`）
      - `ProjectedStartDate`、`ProjectedEndDate`（`DateTimeOffset?`）
      - `InheritedDelayDays`、`OwnVarianceDays`、`TotalVarianceDays`（`int?`）
      - `IsBlocked`（`bool`，默认 false）
      - `BlockerHistory`（`List<BlockerRecord>`，初始化为 `new()`）
    - 新增内嵌类 `BlockerRecord`（见设计文档 Data Models 章节）
    - _Requirements: 1.1_

  - [x] 1.2 扩展 `Stage.cs`，新增 `SlaDays` 和 `ComponentWeights` 属性
    - 文件：`Domain/Entities/OW/Stage.cs`
    - 新增：`[SugarColumn(ColumnName = "sla_days")] public int? SlaDays { get; set; }`
    - 新增：`[SugarColumn(ColumnName = "component_weights", ColumnDataType = "jsonb", IsJson = true)] public string ComponentWeights { get; set; }`
    - _Requirements: 1.2, 1.3_

  - [x] 1.3 扩展 `Onboarding.cs`，新增 `TotalVarianceDays` 属性
    - 文件：`Domain/Entities/OW/Onboarding.cs`
    - 新增：`[SugarColumn(ColumnName = "total_variance_days")] public int? TotalVarianceDays { get; set; }`
    - _Requirements: 1.5_

  - [x] 1.4 新建 `Migration_20260819000001_AddGanttFieldsToStage.cs`
    - 文件：`SqlSugarDB/Migrations/Migration_20260819000001_AddGanttFieldsToStage.cs`
    - `Up()`：`ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS sla_days INTEGER NULL;` 和 `ADD COLUMN IF NOT EXISTS component_weights JSONB NULL;`
    - `Down()`：`DROP COLUMN IF EXISTS sla_days; DROP COLUMN IF EXISTS component_weights;`
    - _Requirements: 1.2_

  - [x] 1.5 新建 `Migration_20260819000002_AddTotalVarianceDaysToOnboarding.cs`
    - 文件：`SqlSugarDB/Migrations/Migration_20260819000002_AddTotalVarianceDaysToOnboarding.cs`
    - `Up()`：`ALTER TABLE ff_onboarding ADD COLUMN IF NOT EXISTS total_variance_days INTEGER NULL;`
    - `Down()`：`DROP COLUMN IF EXISTS total_variance_days;`
    - _Requirements: 1.5_

  - [x] 1.6 在 `MigrationManager.cs` 的 migrations 数组末尾注册两条新 Migration
    - 文件：`SqlSugarDB/Migrations/MigrationManager.cs`
    - 在现有最后一条 entry 后追加：
      ```csharp
      ("20260819000001_AddGanttFieldsToStage",
          (Action)(() => Migration_20260819000001_AddGanttFieldsToStage.Up(_db))),
      ("20260819000002_AddTotalVarianceDaysToOnboarding",
          (Action)(() => Migration_20260819000002_AddTotalVarianceDaysToOnboarding.Up(_db))),
      ```
    - _Requirements: 1.2, 1.5_

---

### Phase 2：领域事件（后端）

- [x] 2. 新建领域事件与 MediatR Handlers
  - [x] 2.1 新建 `OnboardingStartedEvent.cs`
    - 文件：`Domain.Shared/Events/OnboardingStartedEvent.cs`
    - 实现 `INotification`，包含：`OnboardingId`、`StartDate`（`DateTimeOffset`）、`EstimatedCompletionDate`（`DateTimeOffset?`）、`TenantId`、`UserId`、`UserName`
    - _Requirements: 2.1_

  - [x] 2.2 新建 `OnboardingStageMovedEvent.cs`
    - 文件：`Domain.Shared/Events/OnboardingStageMovedEvent.cs`
    - 实现 `INotification`，包含：`OnboardingId`、`FromStageId`（`long`）、`ToStageId`（`long`）、`TenantId`、`UserId`、`UserName`
    - _Requirements: 3.1_

  - [x] 2.3 修改 `OnboardingStatusService.StartOnboardingAsync`，在 `result == true` 后同步发布 `OnboardingStartedEvent`
    - 文件：`Application/Services/OW/OnboardingServices/OnboardingStatusService.cs`
    - 注入 `IMediator`（构造函数），在 `if (result)` 代码块内追加同步 `await _mediator.Publish(new OnboardingStartedEvent { ... })`，必须在 `_backgroundTaskQueue.QueueBackgroundWorkItem` **之前**执行
    - 用 try-catch 包裹 Publish 调用，异常记录 Error 日志，不阻断主响应
    - _Requirements: 2.1_

  - [x] 2.4 新建 `GanttPlannedTimeInitHandler.cs`
    - 文件：`Application/Notification/GanttPlannedTimeInitHandler.cs`
    - 实现 `INotificationHandler<OnboardingStartedEvent>`
    - 在 `Handle()` 中：加载 Onboarding 及其 Stages（按 order_index 排序），调用共用 `ComputePlannedTimes()` 算法，将结果写入每个 `StageProgress.PlannedStartDate` / `PlannedEndDate`，持久化回 `stages_progress_json`
    - 算法规则（见设计文档 Key Algorithms → ComputePlannedTimes）：Stage 1 的 `plannedStartDate = NormalizeToStartOfDay(caseStartDate)`；后续 Stage 的 `plannedStartDate = prevPlannedEndDate + 1 天`；`plannedEndDate = plannedStartDate + duration - 1`；fallback = caseEtaDate != null ? Round((eta - start) / count) : 7
    - 幂等性保护：若 StageProgress 已有 `PlannedStartDate` 则跳过（不覆盖）
    - try-catch 包裹全部逻辑，异常仅记录 Error 日志
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 2.5 新建 `GanttProjectedTimeRecalcHandler.cs`
    - 文件：`Application/Notification/GanttProjectedTimeRecalcHandler.cs`
    - 实现 `INotificationHandler<OnboardingStageCompletedEvent>` 和 `INotificationHandler<OnboardingStageMovedEvent>`，两个 `Handle()` 均委托到 `RecalcProjectedTimesAsync(onboardingId)`
    - 算法规则（见设计文档 Key Algorithms → ComputeProjectedTimes）：
      - Stage 已完成：Projected = Actual（固定）
      - IsBlocked = true：该 Stage 及所有后续 Stage 的 Projected 设为 null，continue
      - 上游 ProjectedEndDate == null：当前 Stage Projected 设为 null，continue
      - 前置已完成：projectedStart = prevCompletionTime + 1 天
      - 前置 InProgress：projectedStart = today + Round(prevEstimated × (1 - prevPct/100)) 天（最小 1 天）
      - 前置未开始：projectedStart = prevProjectedEndDate + 1 天
      - projectedEnd = projectedStart + duration - 1 天
    - try-catch 包裹，异常记录 Error 日志，不影响 StageComplete 主流程
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

---

### Phase 3：GanttService 与 API（后端）

- [x] 3. 新建 GanttService 完整实现与 GanttController
  - [x] 3.1 新建所有 Gantt DTOs
    - 目录：`Application.Contracts/Dtos/OW/Gantt/`
    - 文件列表及结构（完全对应设计文档 Data Models → GanttDataResponseDto）：
      - `GanttDataResponseDto.cs`（含 Summary + Stages）
      - `GanttCaseSummaryDto.cs`（所有 Req 6.2 字段）
      - `GanttStageItemDto.cs`（所有 Req 6.3 字段，含 `GanttStatus` string）
      - `GanttAssigneeDto.cs`（Name + Email）
      - `GanttComponentsDto.cs`（Req 6.4 所有字段）
      - `BlockStageInputDto.cs`（StageId long + BlockerReason string[必填，≤500] + ExpectedResolutionDate DateTimeOffset?）
      - `UnblockStageInputDto.cs`（StageId long + ResolutionNotes string?）
    - 为 `BlockStageInputDto` 创建 FluentValidation `BlockStageInputValidator`（验证 BlockerReason 不为空且不超 500 字符）
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 7.1, 7.3_

  - [x] 3.2 新建 `IGanttService.cs` 接口
    - 文件：`Application.Contracts/IServices/OW/IGanttService.cs`
    - 接口方法：
      - `Task<GanttDataResponseDto> GetGanttDataAsync(long onboardingId)`
      - `Task<bool> BlockStageAsync(long onboardingId, BlockStageInputDto input)`
      - `Task<bool> UnblockStageAsync(long onboardingId, UnblockStageInputDto input)`
    - _Requirements: 6, 7_

  - [x] 3.3 新建 `GanttService.cs` — GetGanttDataAsync 实现
    - 文件：`Application/Services/OW/GanttService.cs`
    - 实现 `IGanttService, IScopedService`
    - `GetGanttDataAsync()` 流程：
      1. `EnsureCaseViewPermissionAsync(onboardingId)`
      2. 加载 Onboarding（不存在或 IsValid=false → 404）
      3. 加载 Workflow 及关联 Stages（按 order_index 排序）
      4. 反序列化 StagesProgressJson
      5. 旧 Case fallback（`PlannedStartDate == null`）：调用 `ComputePlannedTimes()` 动态推算，**不写库**
      6. 实时计算每个 Stage 的 `completionPercentage`（见设计文档 ComputeCompletionPercentage 算法）
      7. 调用 `DeriveGanttStageStatus(stageProgress, today)` 派生 ganttStatus（见设计文档规则）
      8. 解析 Assignee ID → 用户名（调用用户服务或缓存）
      9. 聚合 `GanttCaseSummaryDto`（统计 overdueStages、delayedStages、blockedStages 等）
      10. 返回 `GanttDataResponseDto`
    - 提取私有方法：`ComputePlannedTimes()`、`DeriveGanttStageStatus()`、`ComputeCompletionPercentage()`（与 Handler 共用算法逻辑）
    - _Requirements: 2.5, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4, 6.1–6.7, 16.1_

  - [x] 3.4 新建 `GanttService.cs` — BlockStageAsync + UnblockStageAsync 实现
    - 在 3.3 同一文件中继续实现：
    - `BlockStageAsync()`：
      1. `EnsureCaseOperatePermissionAsync()`
      2. 加载 Onboarding 及 StageProgress
      3. 若 `isBlocked == true` → 400 "Stage is already blocked"
      4. 设 `isBlocked = true`，向 `BlockerHistory` append 新 `BlockerRecord`（blockerStartDate = 当前时间）
      5. 持久化 StagesProgressJson
      6. 触发 `GanttProjectedTimeRecalcHandler` 重算（通过 Mediator 发布 `OnboardingStageMovedEvent`，或直接注入 handler 调用）
    - `UnblockStageAsync()`：
      1. `EnsureCaseOperatePermissionAsync()`
      2. 若 `isBlocked == false` → 400 "Stage is not blocked"
      3. 设 `isBlocked = false`，更新 BlockerHistory 最新记录的 resolvedDate、blockedDays、resolutionNotes
      4. 持久化 StagesProgressJson
      5. 触发 Projected 重算
    - _Requirements: 7.1–7.8, 16.2_

  - [x] 3.5 新建 `GanttController.cs`（5 个接口）
    - 文件：`WebApi/Controllers/OW/GanttController.cs`
    - 路由：`[Route("ow/gantt/v1")]`
    - 接口：
      - `GET {onboardingId}` → `GetGanttDataAsync`
      - `POST {onboardingId}/block` → `BlockStageAsync`（传入 `[FromBody] BlockStageInputDto`）
      - `POST {onboardingId}/unblock` → `UnblockStageAsync`（传入 `[FromBody] UnblockStageInputDto`）
      - `GET tour/seen` → `IUserTourRecordService.HasSeenAsync(userId, "gantt-case-tour")`
      - `POST tour/mark-seen` → `IUserTourRecordService.MarkSeenAsync(userId, "gantt-case-tour")`
    - 所有接口均使用 `Success<T>(data)` 包装返回
    - _Requirements: 6.1, 6.5, 6.6, 7.1, 7.3, 15.6_

  - [x] 3.6 新建 `IUserTourRecordService` 接口 + `UserTourRecordService` 实现
    - 接口文件：`Application.Contracts/IServices/OW/IUserTourRecordService.cs`
    - 方法：`Task<bool> HasSeenAsync(string userId, string tourKey)` 和 `Task MarkSeenAsync(string userId, string tourKey)`
    - 实现文件：`Application/Services/OW/UserTourRecordService.cs`，基于 `ff_user_tour_records` 表（Migration 已存在）
    - 实现 `IScopedService`
    - _Requirements: 15.3, 15.6_

  - [x] 3.7 在 DI 注册中确认 `IGanttService` → `GanttService` 和 `IUserTourRecordService` → `UserTourRecordService` 已自动注入
    - 由于两个 Service 都实现 `IScopedService`，DI 自动注册机制（现有 `AddScopedServices()` 扫描）会自动处理
    - 验证 `Program.cs` 或 `ServiceCollectionExtensions.cs` 中的自动注册扫描包含 Application 层
    - _Requirements: 6, 7_

---

### Phase 4：后端单元测试

- [ ] 4. 编写后端单元测试
  - [ ] 4.1 新建 `GanttAlgorithmTests.cs` — ComputePlannedTimes 算法测试
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttAlgorithmTests.cs`
    - 测试场景：
      - `ComputePlannedTimes_AllStagesHaveEstimatedDuration_ShouldCalculateCorrectly`（3 stages [7,5,3]，验证连续性 & 起止日期）
      - `ComputePlannedTimes_NullEstimatedDuration_UsesEtaFallback`（有 ETA，验证均分逻辑）
      - `ComputePlannedTimes_NullEstimatedDurationNoEta_UsesDefaultSevenDays`
      - `ComputePlannedTimes_PlannedDatesAreConsecutive_NoGapsOrOverlaps`（验证 Property 1）
      - `ComputePlannedTimes_FirstStagePlanStartEqualsCase_StartDate`（验证 Property 2）
    - _Requirements: 2.2, 2.3_

  - [ ] 4.2 新建 `GanttStatusTests.cs` — DeriveGanttStageStatus 测试
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttStatusTests.cs`
    - 测试场景（全部 5 种状态 + 优先级顺序）：
      - `DeriveGanttStageStatus_IsCompleted_ReturnsCompleted`
      - `DeriveGanttStageStatus_StartedAndPastPlannedEnd_ReturnsOverdue`
      - `DeriveGanttStageStatus_StartedAndBeforePlannedEnd_ReturnsInProgress`
      - `DeriveGanttStageStatus_NotStartedAndPastPlannedStart_ReturnsDelayed`
      - `DeriveGanttStageStatus_NotStartedAndBeforePlannedStart_ReturnsNotStarted`
      - `DeriveGanttStageStatus_CompletedAlwaysWins_EvenIfPastPlannedEnd`（验证优先级）
    - _Requirements: 5.1, 5.2_

  - [ ] 4.3 新建 `GanttProjectedTimeTests.cs` — ComputeProjectedTimes 测试
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttProjectedTimeTests.cs`
    - 测试场景：
      - `ComputeProjectedTimes_AllPrevCompleted_StartsFromPrevActualEnd`
      - `ComputeProjectedTimes_PrevInProgress_EstimatesRemainingDays`
      - `ComputeProjectedTimes_PrevNotStarted_UsesProjectedEndDate`
      - `ComputeProjectedTimes_BlockedStage_SetsNullForBlockedAndAllSubsequent`（验证 Property 5）
      - `ComputeProjectedTimes_UpstreamNullProjected_PropagatesNull`
      - `ComputeProjectedTimes_NoBlockedStages_AllProjectedDatesAreConsecutive`（验证 Property 4）
    - _Requirements: 3.1, 3.2, 3.3_

  - [ ] 4.4 新建 `GanttBlockStageTests.cs` — Block/Unblock 边界测试
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttBlockStageTests.cs`
    - 测试场景：
      - `BlockStageAsync_AlreadyBlocked_ThrowsBusinessError`
      - `BlockStageAsync_Success_AppendsBlockerRecordToHistory`
      - `BlockStageAsync_Success_SetsIsBlockedTrue`
      - `UnblockStageAsync_NotBlocked_ThrowsBusinessError`
      - `UnblockStageAsync_Success_FillsResolvedDateAndBlockedDays`
      - `UnblockStageAsync_Success_SetsIsBlockedFalse`
    - _Requirements: 7.1–7.6_

  - [ ]\* 4.5 新建 `GanttPlannedTimeInitHandlerTests.cs`
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttPlannedTimeInitHandlerTests.cs`
    - 测试场景：
      - `Handle_OnboardingStartedEvent_WritesPlannedTimesToStagesProgressJson`
      - `Handle_StagesAlreadyHavePlannedDates_DoesNotOverwrite`（幂等性）
      - `Handle_Exception_DoesNotPropagateToMainFlow`
    - _Requirements: 2.1, 2.4_

  - [ ]\* 4.6 新建 `GanttProjectedTimeRecalcHandlerTests.cs`
    - 文件：`Tests/FlowFlex.Tests/Gantt/GanttProjectedTimeRecalcHandlerTests.cs`
    - 测试场景：
      - `Handle_OnboardingStageCompletedEvent_TriggersRecalculation`
      - `Handle_OnboardingStageMovedEvent_TriggersRecalculation`
      - `Handle_BlockedStage_SetsNullProjectedFromBlockedStageOnward`
      - `Handle_Exception_DoesNotPropagateToMainFlow`
    - _Requirements: 3.1, 3.4_

- [ ] 5. Checkpoint — 后端编译 + 单元测试
  - 运行 `dotnet build` 确认零编译错误
  - 运行 `dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests` 确认新增测试全部通过
  - 如有问题，在继续前解决

---

### Phase 5：前端 API 与 Store

- [ ] 6. 修复并完善前端 gantt.ts 和 Pinia Store
  - [ ] 6.1 修复并完善 `gantt.ts`
    - 文件：`packages/flowFlex-common/src/app/apis/ow/gantt.ts`
    - 移除 `'Blocked'` from `GanttStageStatus` 枚举（与设计文档对齐，改为独立的 `isBlocked` 字段）
    - 将 `GanttStageItem.status` 重命名为 `ganttStatus: GanttStageStatus`（与后端 DTO 字段名一致）
    - 将 `ownPerformanceDays` 重命名为 `ownVarianceDays`（与设计文档一致）
    - `GanttStageItem` 增加 `isRequired: boolean` 字段
    - 增加 `GanttAssignee` 接口（`name: string; email?: string`），将 `assignee: string[]` 改为 `assignee: GanttAssignee[]`，`coAssignees` 同理
    - 取消所有 TODO 注释，实现真实 API 调用（`defHttp.get`、`defHttp.post`）：
      - `getOnboardingGanttData` → `GET ow/gantt/v1/{onboardingId}`
      - `blockStage` → `POST ow/gantt/v1/{onboardingId}/block`
      - `unblockStage` → `POST ow/gantt/v1/{onboardingId}/unblock`
      - 新增 `getGanttTourSeen()` → `GET ow/gantt/v1/tour/seen`
      - 新增 `markGanttTourSeen()` → `POST ow/gantt/v1/tour/mark-seen`
    - 删除 `createMockGanttData()` 函数和全部 Mock 相关代码（保留类型定义）
    - _Requirements: 6, 7, 15.6_

  - [ ] 6.2 新建 Pinia `ganttStore.ts`
    - 文件：`packages/flowFlex-common/src/app/stores/modules/gantt.ts`
    - Store ID：`'item-wfe-app-gantt'`
    - State：`ganttData: Map<string, GanttDataResponse>`、`loading: boolean`、`tourSeen: boolean | null`（null = 未查询）
    - Actions：
      - `fetchGanttData(onboardingId: string)` — 有缓存直接返回，否则调用 API 存入 Map
      - `blockStage(onboardingId, params)` — 调用 API 后 `invalidateCache(onboardingId)`
      - `unblockStage(onboardingId, stageId, notes?)` — 调用 API 后 `invalidateCache(onboardingId)`
      - `checkTourSeen()` — 调用 API，结果存入 `tourSeen`
      - `markTourSeen()` — 调用 API，`tourSeen = true`
      - `invalidateCache(onboardingId)` — 从 Map 中删除对应 key
    - _Requirements: 15.6_

---

### Phase 6：前端甘特图核心组件

- [ ] 7. 新建 GanttSummaryHeader.vue
  - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttSummaryHeader.vue`
  - 接收 Props：`summary: GanttCaseSummary`
  - 展示两个并排区块 "PLANNED（Original Plan）" 和 "PROJECTED（Current Forecast）"，各含 Start/End Date
  - 偏差天数计算：`projectedEndDate - plannedEndDate`，正值用橙/红色，零或负值用绿色（格式："+N days"）
  - ⓘ 图标 hover 300ms 后显示 Tooltip（解释 PLANNED / PROJECTED 含义）
  - 显示：Case 名称、Workflow 名称、"X of Y Stages"、整体完成度百分比
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 8. 新建 GanttChart.vue（SVG 渲染引擎）
  - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttChart.vue`
  - 接收 Props：`stages: GanttStageItem[]`、`viewMode: 'day'|'week'|'month'`（默认 week）
  - SVG 坐标系：`UNIT_WIDTH = { day: 40, week: 120, month: 300 }`（px per unit），`dateToX(date, viewStart, mode)` 函数
  - 左侧固定列（300px）：Stage 序号、名称（溢出 ellipsis）、Assignee 姓名（多人时 "+N"）、Status Badge、Required 标记
  - 右侧水平可滚动 SVG 区域：时间刻度行、Today 红色垂直参考线
  - 每行渲染两条 rect：
    - Planned rect（灰色虚线框，50% 透明，仅当 Projected ≠ Planned 时显示，y 偏移 +2，高 14px）
    - Projected rect（实色填充，按 GanttStageStatus 颜色映射，y 偏移 +12，高 14px）
  - 工具栏：Day/Week/Month 切换、Today 按钮（滚动至今天）、Prev/Next 导航按钮、按 GanttStageStatus 和 Assignee 的多选筛选下拉
  - Stage 数超过 10 支持纵向滚动
  - InProgress 行：蓝色左边框高亮；Overdue 行：红色样式 + 警告图标
  - 点击 Stage 条：emit `stage-click` 事件（带 stageId）；双击：emit `stage-double-click`
  - _Requirements: 10.1–10.8_

- [ ] 9. 新建 GanttStageRow.vue（单行 Stage 渲染）
  - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttStageRow.vue`
  - 接收 Props：`stage: GanttStageItem`、`rowY: number`、`dateToX: Function`
  - 渲染：Status Badge（颜色按 Requirement 11.1 映射表）；isBlocked 时叠加紫色条纹 SVG pattern
  - Badge ⓘ 图标逻辑：Overdue/Delayed/Completed/Blocked 时显示，hover 300ms 展示对应 Tooltip 文字（见 Req 11.2 和 11.3 的 7 种场景）
  - Completed Badge 显示偏差标注（+N days / -N days / on time）
  - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [ ] 10. 新建 GanttStagePopover.vue（Stage 详情面板）
  - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttStagePopover.vue`
  - 接收 Props：`stage: GanttStageItem`（或 null，null 时隐藏），`onboardingId: string`
  - 展示三个区块（见 Req 12.2）：
    - 基础信息：Stage 名称、Required/Optional、GanttStageStatus Badge
    - Timeline：Planned Start/End、Actual Start/End（未完成时 "--"）、Planned Duration、Days Elapsed、Days Remaining（仅 InProgress/Overdue）
    - Components：CompletionPercentage、Checklists、Questionnaires、Fields、Files 统计
    - Blockers（`isBlocked == true`）：阻塞原因、开始日期、已阻塞天数、"Resolve" 按钮
  - "Go to Stage" 按钮：emit `go-to-stage` 事件
  - "Mark as Blocked" 按钮（当 Stage 未被 Blocked 时显示）：emit `open-block-modal`
  - "Resolve" 按钮（当 Stage isBlocked = true 时显示）：emit `open-unblock-modal`
  - _Requirements: 12.1, 12.2, 12.3, 12.5, 12.6_

- [ ] 11. 新建 GanttLegend.vue（颜色图例）
  - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttLegend.vue`
  - 支持收起/展开（默认展开）
  - 展示：所有 GanttStageStatus 颜色块 + 状态名 + 简短描述（见 Req 11.1 颜色表）、Planned 条形样式说明（灰色虚线 = 原始计划）、Projected 条形样式说明（实色 = 当前预测）
  - 固定显示在甘特图底部
  - _Requirements: 15.1, 15.2_

---

### Phase 7：前端交互与辅助组件

- [ ] 12. 新建交互弹窗与模态框组件
  - [ ] 12.1 新建 `BlockStageModal.vue`（Mark as Blocked 弹窗）
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/BlockStageModal.vue`
    - Props：`visible: boolean`、`stageName: string`
    - Emits：`confirm({ blockerReason, expectedResolutionDate? })` 和 `cancel`
    - 字段：Stage 名称（只读）、Blocker Reason 文本框（必填，≤500 字符，提交时前端校验）、Expected Resolution Date 日期选择器（可选）、Cancel + "Mark as Blocked" 按钮
    - 提交时 Blocker Reason 为空则显示校验提示，不触发 confirm
    - _Requirements: 13.1, 13.2, 13.3_

  - [ ] 12.2 新建 `UnblockStageModal.vue`（Resolve Blocker 弹窗）
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/UnblockStageModal.vue`
    - Props：`visible: boolean`、`stage: GanttStageItem`
    - Emits：`confirm({ resolutionNotes? })` 和 `cancel`
    - 字段：Stage 名称（只读）、Blocked Since 日期（只读）、已阻塞天数（只读）、阻塞原因（只读）、Resolution Notes 文本框（可选）、Cancel + "Resolve Blocker" 按钮
    - _Requirements: 13.4_

  - [ ] 12.3 新建 `GanttThumbnail.vue`（缩略预览 400×180px）
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttThumbnail.vue`
    - Props：`summary: GanttCaseSummary`、`stages: GanttStageItem[]`
    - 只读视图，展示：Case 名称、Workflow 名称、"X of Y Stages"、Overall 进度条（颜色按整体健康度）、Start Date、ETA、当前 Stage 名称 + Assignee 姓名
    - "View Full Chart" 按钮：emit `view-full-chart`
    - 整体可点击：emit `view-full-chart`
    - 尺寸约 400×180px，无其他可点击元素
    - _Requirements: 8.2, 8.3, 8.4_

  - [ ] 12.4 新建 `GanttModal.vue`（完整甘特图模态框 80vw）
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttModal.vue`
    - Props：`visible: boolean`、`onboardingId: string`
    - 组合子组件：`GanttSummaryHeader` + `GanttChart`（含 `GanttStageRow`）+ `GanttStagePopover` + `GanttLegend` + `GanttTour`
    - 打开时触发 `ganttStore.fetchGanttData(onboardingId)` 加载数据
    - 右上角 "[? Help]" 按钮：显示帮助面板（Planned/Projected 术语说明 + 各 Status 含义 + 操作 Tips）
    - Stage 双击：关闭模态框，emit `go-to-stage(stageId)` 由父组件处理导航
    - Mark as Blocked / Resolve 操作完成后：`ganttStore.invalidateCache(onboardingId)` + 重新 fetchGanttData
    - _Requirements: 8.4, 12.4, 14.2, 15.7_

  - [ ] 12.5 新建 `GanttTour.vue`（5 步首次引导）
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/GanttTour.vue`
    - Props：`active: boolean`
    - Emits：`complete`（全部完成或跳过）
    - 5 步顺序高亮区域（见设计文档 GanttTour 表格）：GanttSummaryHeader → Planned/Projected 对比区域 → Stage 时间轴条 → Status Badge + ⓘ → Legend 区域
    - 每步：Skip（跳过全部）和 Next（下一步）按钮，最后一步显示 "Got it"
    - 完成或跳过时：调用 `ganttStore.markTourSeen()`
    - 通过 `ganttStore.checkTourSeen()` 决定是否自动显示（`tourSeen === false` 时自动激活）
    - _Requirements: 15.3, 15.4, 15.5, 15.6_

---

### Phase 8：前端集成

- [ ] 13. 前端集成到 onboardingList 列表页与详情页
  - [ ] 13.1 修改 `onboardingList/index.vue`（列表页行 hover 甘特图预览）
    - 文件：`src/app/views/onboard/onboardingList/index.vue`
    - 引入 `GanttThumbnail.vue` 和 `GanttModal.vue`
    - 在列表行添加 `@mouseenter` / `@mouseleave` 事件：
      - 使用 500ms debounce（`setTimeout / clearTimeout`），防止频繁触发
      - hover 持续 500ms 后调用 `ganttStore.fetchGanttData(row.id)` 并显示 `GanttThumbnail`
      - mouseleave 时清除 timer，隐藏 Thumbnail
    - `GanttThumbnail` 的 `view-full-chart` 事件：打开 `GanttModal`
    - 列表页无权限查看 Case 时不触发 Thumbnail（通过现有权限字段判断）
    - _Requirements: 8.1, 8.2, 8.4, 8.5, 8.6, 8.7, 16.3_

  - [ ] 13.2 修改 `onboardingList/detail.vue`（Case 详情页 Gantt 按钮）
    - 找到 Case 详情页顶部操作栏文件（与现有 History、Export、Edit Details 等按钮同区域）
    - 新增 "Gantt" 按钮（与现有按钮并排）
    - 点击按钮：打开 `GanttModal`（传入当前 Case ID）
    - `GanttModal` 的 `go-to-stage` 事件：滚动至对应 Stage 组件（通过现有 `setActiveStage` 机制）
    - Gantt 按钮对所有有查看权限的用户可见，无额外权限限制
    - _Requirements: 14.1, 14.2, 14.3_

  - [ ] 13.3 在 Case 详情页当前 Stage 操作菜单中添加 "Mark as Blocked" 入口
    - 找到 Case 详情页当前 Stage 操作菜单（通常在 Stage 卡片的操作按钮区）
    - 新增 "Mark as Blocked" 菜单项，点击后显示 `BlockStageModal`（传入当前 Stage ID 和 Onboarding ID）
    - 操作成功后触发 `ganttStore.invalidateCache` + 刷新当前 Stage 状态
    - _Requirements: 13.1_

---

### Phase 9：前端单元测试

- [ ] 14. 编写前端单元测试
  - [ ]\* 14.1 新建 `GanttChart.spec.ts`
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/__tests__/GanttChart.spec.ts`
    - 测试场景：
      - `dateToX — Day 模式下正确计算日期到像素坐标`
      - `dateToX — Week 模式下正确计算`
      - `dateToX — Month 模式下正确计算`
      - `渲染时 Planned rect 在 Projected ≠ Planned 时显示`
      - `渲染时 Projected rect 按 GanttStageStatus 映射颜色`
      - `Today 参考线在正确的 X 位置渲染`
    - _Requirements: 10.2, 10.3_

  - [ ]\* 14.2 新建 `GanttSummaryHeader.spec.ts`
    - 文件：`src/app/views/onboard/onboardingList/components/gantt/__tests__/GanttSummaryHeader.spec.ts`
    - 测试场景：
      - `正偏差（projectedEndDate > plannedEndDate）时偏差天数显示警告色类名`
      - `负偏差时偏差天数显示绿色类名`
      - `零偏差时显示绿色类名或 "on time"`
    - _Requirements: 9.3, 9.4_

  - [ ]\* 14.3 新建 `ganttStore.spec.ts`
    - 文件：`src/app/stores/modules/__tests__/ganttStore.spec.ts`
    - 测试场景：
      - `fetchGanttData — 首次调用存入 Map`
      - `fetchGanttData — 已有缓存不重复调用 API`
      - `invalidateCache — 清除对应 onboardingId 的缓存数据`
      - `blockStage — 调用后自动 invalidateCache`
    - _Requirements: 前端 Store 设计_

- [ ] 15. 最终 Checkpoint — 完整功能验证
  - 确认后端构建 `dotnet build` 通过
  - 确认前端构建 `pnpm build:production`（或 `pnpm type:check`）通过，无 TypeScript 类型错误
  - 人工验证清单（不自动执行，提醒用户）：
    - 旧 Case GET gantt → 前端正常显示（动态推算 fallback）
    - Case start → GET gantt 立即可见 Planned 时间（同步 Publish）
    - Stage 完成 → GET gantt 后 Projected 时间已更新
    - block-stage → GET gantt → Blocked Stage 及后续 Projected 为 null
    - unblock-stage → GET gantt → Projected 恢复正常
    - Tour 首次打开显示，第二次不显示

## Notes

- 所有测试子任务（带 `*` 标记）为可选，可跳过以加快 MVP 交付，但建议在完整发布前补全
- 每个任务均引用具体需求编号（如 `Req 6.1`），方便追溯
- Phase 1–3 纯后端，Phase 5–9 纯前端，Phase 4 为后端测试——可并行安排后端和前端开发者
- `blockStage` / `unblockStage` 完成后需触发 Projected 重算；可通过直接调用 `GanttProjectedTimeRecalcHandler.RecalcProjectedTimesAsync()` 实现（不需要额外发 Event）
- `GanttService` 中 `ComputePlannedTimes`、`DeriveGanttStageStatus`、`ComputeProjectedTimes` 三个算法函数应提取为 `private static` 方法，以便 Handler 和 Service 共用同一套逻辑

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "1.5"] },
    { "id": 2, "tasks": ["1.6"] },
    { "id": 3, "tasks": ["2.1", "2.2"] },
    { "id": 4, "tasks": ["2.3", "2.4", "2.5"] },
    { "id": 5, "tasks": ["3.1", "3.2"] },
    { "id": 6, "tasks": ["3.3", "3.6"] },
    { "id": 7, "tasks": ["3.4", "3.5", "3.7"] },
    { "id": 8, "tasks": ["4.1", "4.2", "4.3", "6.1", "6.2"] },
    { "id": 9, "tasks": ["4.4", "4.5", "4.6", "7", "8", "9"] },
    { "id": 10, "tasks": ["10", "11"] },
    { "id": 11, "tasks": ["12.1", "12.2", "12.3"] },
    { "id": 12, "tasks": ["12.4", "12.5"] },
    { "id": 13, "tasks": ["13.1", "13.2", "13.3"] },
    { "id": 14, "tasks": ["14.1", "14.2", "14.3"] }
  ]
}
```
