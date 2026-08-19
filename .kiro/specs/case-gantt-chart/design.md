# Design Document — Case Gantt Chart

## Overview

为 FlowFlex WFE 系统的 Case 模块新增**甘特图时间线视图**。每个 Case 由多个按序推进的 Stage 组成，当前系统仅有单 Stage 进度条，缺乏整体进度与时间偏差的可视化能力。

本功能在**不改动任何现有流程逻辑**的前提下，增量添加：
- 数据模型扩展（StageProgress JSONB + ff_stage/ff_onboarding Migration）
- 时间推算引擎（Planned 初始化 / Projected 动态重算）
- 甘特图专属 API（数据查询、Blocked 状态管理、Tour 记录）
- 前端甘特图组件（缩略预览、完整模态框、5 步引导 Tour）

访问入口三处：Case 列表页悬停缩略图、缩略图 "View Full Chart" 按钮、Case 详情页 "Gantt" 按钮。

---

## Architecture

### 整体数据流

```
┌──────────────────────────────────────────────────────────────────┐
│ Frontend                                                          │
│  onboardingList/index.vue (hover 500ms debounce)                 │
│  └─→ GanttThumbnail.vue (只读预览)                                │
│       └─→ GanttModal.vue (完整模态框 80vw)                        │
│            ├─ GanttSummaryHeader.vue                             │
│            ├─ GanttChart.vue  [SVG 渲染]                         │
│            │   ├─ GanttStageRow.vue  × N                         │
│            │   └─ GanttStagePopover.vue                          │
│            ├─ GanttLegend.vue                                    │
│            └─ GanttTour.vue                                      │
│  gantt.ts (Axios) → Pinia ganttStore                             │
└──────────────────┬───────────────────────────────────────────────┘
                   │ REST
┌──────────────────▼───────────────────────────────────────────────┐
│ GanttController (ow/gantt/v1)                                     │
│  GET  /{onboardingId}          → IGanttService.GetGanttDataAsync  │
│  POST /{onboardingId}/block    → IGanttService.BlockStageAsync    │
│  POST /{onboardingId}/unblock  → IGanttService.UnblockStageAsync  │
│  GET  /tour/seen               → IUserTourRecordService           │
│  POST /tour/mark-seen          → IUserTourRecordService           │
└──────────────────┬───────────────────────────────────────────────┘
                   │
┌──────────────────▼───────────────────────────────────────────────┐
│ GanttService                                                       │
│  · EnsureCaseViewPermissionAsync / EnsureCaseOperatePermissionAsync│
│  · ComputePlannedTimes()  ← 共用算法（Handler + fallback）         │
│  · ComputeProjectedTimes()                                        │
│  · ComputeCompletionPercentage()（实时，不持久化）                  │
│  · DeriveGanttStageStatus()                                       │
└──────────────────┬───────────────────────────────────────────────┘
                   │
┌──────────────────▼───────────────────────────────────────────────┐
│ MediatR Events & Handlers                                          │
│  OnboardingStartedEvent    → GanttPlannedTimeInitHandler          │
│  OnboardingStageCompletedEvent  ─┐                                │
│  OnboardingStageMovedEvent      ─┴→ GanttProjectedTimeRecalcHandler│
└──────────────────┬───────────────────────────────────────────────┘
                   │ SqlSugar ORM
┌──────────────────▼───────────────────────────────────────────────┐
│ PostgreSQL                                                         │
│  ff_onboarding.stages_progress_json (JSONB)                       │
│  ff_stage.sla_days / component_weights                            │
│  ff_onboarding.total_variance_days                                │
└──────────────────────────────────────────────────────────────────┘
```

### 核心设计原则

1. **零破坏性**：所有新增字段均 nullable，旧 Case 数据读取路径不受影响
2. **实时计算**：completionPercentage 和 GanttStageStatus 在查询时派生，不持久化
3. **事件驱动**：Planned 初始化和 Projected 重算均由 MediatR 事件驱动，与业务流程解耦
4. **复用权限**：完全复用现有 `IOnboardingPermissionService`，不新增权限维度

---

## Components and Interfaces

### 后端接口层

#### GanttController

```csharp
// WebApi/Controllers/OW/GanttController.cs
[Route("ow/gantt/v1")]
public class GanttController : ControllerBase
{
    [HttpGet("{onboardingId}")]
    public async Task<IActionResult> GetGanttData(long onboardingId)
        → Success<GanttDataResponseDto>

    [HttpPost("{onboardingId}/block")]
    public async Task<IActionResult> BlockStage(long onboardingId, [FromBody] BlockStageInputDto input)
        → Success<bool>

    [HttpPost("{onboardingId}/unblock")]
    public async Task<IActionResult> UnblockStage(long onboardingId, [FromBody] UnblockStageInputDto input)
        → Success<bool>

    [HttpGet("tour/seen")]
    public async Task<IActionResult> GetTourSeen()
        → Success<bool>

    [HttpPost("tour/mark-seen")]
    public async Task<IActionResult> MarkTourSeen()
        → Success<bool>
}
```

#### IGanttService

```csharp
// Application.Contracts/IServices/OW/IGanttService.cs
public interface IGanttService
{
    Task<GanttDataResponseDto> GetGanttDataAsync(long onboardingId);
    Task<bool> BlockStageAsync(long onboardingId, BlockStageInputDto input);
    Task<bool> UnblockStageAsync(long onboardingId, UnblockStageInputDto input);
}
```

#### MediatR Handlers

```csharp
// GanttPlannedTimeInitHandler: INotificationHandler<OnboardingStartedEvent>
// 职责：根据 Stage 顺序和 EstimatedDuration 推算所有 Stage 的 plannedStartDate/plannedEndDate
// 写入：更新 ff_onboarding.stages_progress_json

// GanttProjectedTimeRecalcHandler:
//   INotificationHandler<OnboardingStageCompletedEvent>,
//   INotificationHandler<OnboardingStageMovedEvent>
// 职责：重算所有未完成 Stage 的 projectedStartDate/projectedEndDate
// 写入：更新 ff_onboarding.stages_progress_json
```

### 前端接口层

#### API 模块（gantt.ts）

```typescript
// 修复后的类型（移除 Blocked，统一命名）
export type GanttStageStatus = 'NotStarted' | 'Delayed' | 'InProgress' | 'Overdue' | 'Completed'

export interface GanttAssignee {
  name: string
  email?: string
}

export interface GanttStageItem {
  stageId: string
  stageName: string
  stageOrder: number
  color?: string
  isRequired: boolean
  ganttStatus: GanttStageStatus      // 后端派生的甘特图专属状态
  isBlocked: boolean
  assignee: GanttAssignee[]
  coAssignees: GanttAssignee[]
  plannedStartDate: string
  plannedEndDate: string
  projectedStartDate: string | null
  projectedEndDate: string | null
  actualStartDate: string | null
  actualEndDate: string | null
  estimatedDurationDays: number
  completionPercentage: number
  inheritedDelayDays?: number | null  // 字段统一：ownVarianceDays（非 ownPerformanceDays）
  ownVarianceDays?: number | null
  totalVarianceDays?: number | null
  blockedDays: number
  blockReason?: string | null
  expectedResolutionDate?: string | null
  components?: GanttComponents
  lastSavedBy?: string
  lastSavedAt?: string
}

// 真实 API 调用（不使用 mock）
export function getOnboardingGanttData(onboardingId: string | number): Promise<GanttDataResponse>
export function blockStage(onboardingId: string | number, params: BlockStageParams): Promise<boolean>
export function unblockStage(onboardingId: string | number, stageId: string, resolutionNotes?: string): Promise<boolean>
export function getGanttTourSeen(): Promise<boolean>
export function markGanttTourSeen(): Promise<void>
```

#### Pinia Store（gantt.ts）

```typescript
// stores/modules/gantt.ts — store id: 'item-wfe-app-gantt'
state: {
  ganttData: Map<string, GanttDataResponse>  // onboardingId → 缓存数据
  loading: boolean
  tourSeen: boolean | null  // null = 未查询
}
actions: {
  fetchGanttData(onboardingId: string): Promise<GanttDataResponse>
  blockStage(onboardingId: string, params: BlockStageParams): Promise<void>
  unblockStage(onboardingId: string, stageId: string, notes?: string): Promise<void>
  checkTourSeen(): Promise<boolean>
  markTourSeen(): Promise<void>
  invalidateCache(onboardingId: string): void
}
```

---

## Data Models

### OnboardingStageProgress — 新增字段

以下字段新增到 `OnboardingStageProgress.cs`（序列化进 JSONB，无需 Migration）：

```csharp
// ── Gantt 时间字段 ──────────────────────────────────────────────
/// <summary>Planned start date — set at Case start, never mutated afterwards</summary>
public DateTimeOffset? PlannedStartDate { get; set; }

/// <summary>Planned end date — set at Case start, never mutated afterwards</summary>
public DateTimeOffset? PlannedEndDate { get; set; }

/// <summary>Projected start date — recalculated on each Stage advance; null when stage is blocked</summary>
public DateTimeOffset? ProjectedStartDate { get; set; }

/// <summary>Projected end date — recalculated on each Stage advance; null when stage is blocked</summary>
public DateTimeOffset? ProjectedEndDate { get; set; }

// actualStartDate → 已有 StartTime
// actualEndDate   → 已有 CompletionTime

// ── 偏差分析字段（仅 Completed Stage 有值）─────────────────────
/// <summary>= actualStartDate - plannedStartDate (days). Positive = late start.</summary>
public int? InheritedDelayDays { get; set; }

/// <summary>= actualDuration - estimatedDuration (days). Positive = took longer.</summary>
public int? OwnVarianceDays { get; set; }

/// <summary>= actualEndDate - plannedEndDate (days). Positive = late finish.</summary>
public int? TotalVarianceDays { get; set; }

// ── Blocked 状态字段 ────────────────────────────────────────────
/// <summary>Whether this stage is currently blocked</summary>
public bool IsBlocked { get; set; } = false;

/// <summary>Full history of blocker records for this stage</summary>
public List<BlockerRecord> BlockerHistory { get; set; } = new();
```

#### BlockerRecord（新增内嵌类）

```csharp
public class BlockerRecord
{
    public string BlockerReason { get; set; }           // 必填，≤500 字符
    public DateTimeOffset? BlockerStartDate { get; set; }
    public DateTimeOffset? ExpectedResolutionDate { get; set; }
    public DateTimeOffset? BlockerResolvedDate { get; set; }
    public string ResolutionNotes { get; set; }
    public int? BlockedDays { get; set; }              // = resolvedDate - startDate（取整）
}
```

### Stage Entity — 新增字段

```csharp
// Domain/Entities/OW/Stage.cs 新增：

/// <summary>SLA requirement in days; null = no SLA</summary>
[SugarColumn(ColumnName = "sla_days")]
public int? SlaDays { get; set; }

/// <summary>
/// Component weights for CompletionPercentage calculation (JSONB).
/// Format: [{"type":"checklist","id":"1001","name":"CustomerInfo","weight":40}]
/// Sum of all weights must equal 100. Null = equal distribution.
/// </summary>
[SugarColumn(ColumnName = "component_weights", ColumnDataType = "jsonb", IsJson = true)]
public string ComponentWeights { get; set; }
```

### Onboarding Entity — 新增字段

```csharp
// Domain/Entities/OW/Onboarding.cs 新增：

/// <summary>
/// Overall variance in days = Case actualEndDate - plannedEndDate.
/// Updated when the Case completes. Null for in-progress cases.
/// </summary>
[SugarColumn(ColumnName = "total_variance_days")]
public int? TotalVarianceDays { get; set; }
```

### 数据库 Migration

**Migration_20260819000001_AddGanttFieldsToStage.cs**

```sql
-- Up
ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS sla_days INTEGER NULL;
ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS component_weights JSONB NULL;

-- Down
ALTER TABLE ff_stage DROP COLUMN IF EXISTS sla_days;
ALTER TABLE ff_stage DROP COLUMN IF EXISTS component_weights;
```

**Migration_20260819000002_AddTotalVarianceDaysToOnboarding.cs**

```sql
-- Up
ALTER TABLE ff_onboarding ADD COLUMN IF NOT EXISTS total_variance_days INTEGER NULL;

-- Down
ALTER TABLE ff_onboarding DROP COLUMN IF EXISTS total_variance_days;
```

两条 Migration 在 `MigrationManager.cs` 的 migrations 数组末尾注册：

```csharp
("20260819000001_AddGanttFieldsToStage",
    (Action)(() => Migration_20260819000001_AddGanttFieldsToStage.Up(_db))),
("20260819000002_AddTotalVarianceDaysToOnboarding",
    (Action)(() => Migration_20260819000002_AddTotalVarianceDaysToOnboarding.Up(_db))),
```

### GanttDataResponseDto（完整结构）

```csharp
// Application.Contracts/Dtos/OW/Gantt/GanttDataResponseDto.cs
public class GanttDataResponseDto
{
    public GanttCaseSummaryDto Summary { get; set; }
    public List<GanttStageItemDto> Stages { get; set; }
}

public class GanttCaseSummaryDto
{
    public string OnboardingId { get; set; }
    public string CaseName { get; set; }
    public string CaseCode { get; set; }
    public string WorkflowName { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    // Dates (ISO 8601)
    public DateTimeOffset? PlannedStartDate { get; set; }
    public DateTimeOffset? PlannedEndDate { get; set; }
    public DateTimeOffset? ProjectedEndDate { get; set; }
    public DateTimeOffset? ActualStartDate { get; set; }
    public DateTimeOffset? ActualEndDate { get; set; }
    // Stats
    public decimal OverallCompletionPercentage { get; set; }
    public int TotalStages { get; set; }
    public int CompletedStages { get; set; }
    public int OverdueStages { get; set; }
    public int DelayedStages { get; set; }
    public int BlockedStages { get; set; }
    public string CurrentStageName { get; set; }
    public int CurrentStageOrder { get; set; }
}

public class GanttStageItemDto
{
    public string StageId { get; set; }
    public string StageName { get; set; }
    public int StageOrder { get; set; }
    public string Color { get; set; }
    public bool IsRequired { get; set; }
    // Gantt-specific status (5 values, separate from StageProgress.Status)
    public string GanttStatus { get; set; }  // NotStarted|Delayed|InProgress|Overdue|Completed
    // Assignees
    public List<GanttAssigneeDto> Assignee { get; set; }
    public List<GanttAssigneeDto> CoAssignees { get; set; }
    // Three sets of dates
    public DateTimeOffset? PlannedStartDate { get; set; }
    public DateTimeOffset? PlannedEndDate { get; set; }
    public DateTimeOffset? ProjectedStartDate { get; set; }
    public DateTimeOffset? ProjectedEndDate { get; set; }
    public DateTimeOffset? ActualStartDate { get; set; }
    public DateTimeOffset? ActualEndDate { get; set; }
    // Duration & progress
    public int EstimatedDurationDays { get; set; }
    public decimal CompletionPercentage { get; set; }
    // Variance (Completed stages only)
    public int? InheritedDelayDays { get; set; }
    public int? OwnVarianceDays { get; set; }
    public int? TotalVarianceDays { get; set; }
    // Blocker
    public bool IsBlocked { get; set; }
    public int BlockedDays { get; set; }
    public string BlockReason { get; set; }
    public DateTimeOffset? ExpectedResolutionDate { get; set; }
    // Components summary
    public GanttComponentsDto Components { get; set; }
    // Audit
    public string LastSavedBy { get; set; }
    public DateTimeOffset? LastSavedAt { get; set; }
    public int? DaysElapsed { get; set; }
}

public class GanttAssigneeDto
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class GanttComponentsDto
{
    public int ChecklistsTotal { get; set; }
    public int ChecklistsCompleted { get; set; }
    public int QuestionnairesTotal { get; set; }
    public int QuestionnairesSubmitted { get; set; }
    public int FieldsTotal { get; set; }
    public int FieldsFilled { get; set; }
    public int FilesUploaded { get; set; }
}
```

---

## Key Algorithms

### ComputePlannedTimes（Planned 时间推算）

供 `GanttPlannedTimeInitHandler` 和旧 Case fallback 共用。

```
Input:
  stages[]          — 按 order_index 升序排列的 Stage 列表
  caseStartDate     — Case.StartDate（DateTimeOffset）
  caseEtaDate       — Case.EstimatedCompletionDate（可 null）

Output:
  Dictionary<long stageId, (DateTimeOffset plannedStart, DateTimeOffset plannedEnd)>

Algorithm:
  fallbackDays = caseEtaDate != null
                 ? Math.Max(1, Round((caseEtaDate - caseStartDate).TotalDays / stages.Count))
                 : 7

  current = NormalizeToStartOfDay(caseStartDate)

  for each stage in stages (ordered by order_index):
    duration = (stage.EstimatedDuration != null && stage.EstimatedDuration > 0)
               ? (int)Math.Round(stage.EstimatedDuration.Value)
               : fallbackDays

    plannedStart = current
    plannedEnd   = current + (duration - 1) days   // inclusive end
    result[stage.Id] = (plannedStart, plannedEnd)
    current = plannedEnd + 1 day
```

规则说明：
- Planned 一旦写入后，任何对 `plannedStartDate` / `plannedEndDate` 的修改请求应被 GanttService 拦截并返回 400
- 旧 Case（plannedStartDate = null）在 GET 查询时动态推算，**不写库**

### ComputeProjectedTimes（Projected 时间重算）

```
Input:
  stagesProgress[]  — 所有 StageProgress 列表，已加载 Stage 元数据
  today             — NormalizeToStartOfDay(DateTimeOffset.UtcNow)

Algorithm:
  for each stageProgress in order (by stageOrder):
    if stageProgress.IsBlocked:
      stageProgress.ProjectedStartDate = null
      stageProgress.ProjectedEndDate   = null
      continue  // 后续 Stage 依赖此 Stage，全部设 null

    if previous stage exists and previous.ProjectedEndDate == null:
      // upstream is blocked or not computable
      stageProgress.ProjectedStartDate = null
      stageProgress.ProjectedEndDate   = null
      continue

    if stageProgress.IsCompleted:
      // 已完成：Projected = Actual（固定，不再变动）
      stageProgress.ProjectedStartDate = stageProgress.StartTime
      stageProgress.ProjectedEndDate   = stageProgress.CompletionTime
    else:
      projectedStart =
        if prev == null:
          caseStartDate
        elif prev.IsCompleted:
          prev.CompletionTime + 1 day
        elif prev.IsInProgress (actualStartDate != null):
          remainingDays = Round(prevEstimated * (1 - prev.completionPct / 100))
          today + Max(remainingDays, 1) days
        else:
          prev.ProjectedEndDate + 1 day

      duration = Round(stage.EstimatedDuration) or fallbackDays
      stageProgress.ProjectedStartDate = projectedStart
      stageProgress.ProjectedEndDate   = projectedStart + (duration - 1) days
```

### DeriveGanttStageStatus（状态派生，查询时计算）

```
Input: OnboardingStageProgress, today

Rules (评估顺序):
  1. IsCompleted == true                         → Completed
  2. actualStartDate != null && today > plannedEndDate → Overdue
  3. actualStartDate != null && today ≤ plannedEndDate → InProgress
  4. actualStartDate == null && today > plannedStartDate → Delayed
  5. else                                         → NotStarted

注：isBlocked 与 ganttStatus 正交，在 DTO 层并行返回
```

### ComputeCompletionPercentage（查询时实时计算）

```
Input: stage, stageProgress, componentWeights, raw component data

if stage.Components is empty:
  return 0

weights = stage.ComponentWeights (parsed) or EqualDistribution(stage.Components)

completionPct = 0
for each component in stage.Components:
  w = weights[component.type + component.id]
  switch component.type:
    case "checklist":
      comp = completedTasks / totalTasks  (0 if totalTasks == 0)
    case "questionnaire":
      comp = answeredRequired / totalRequired
      if totalRequired == 0: comp = 1.0
    case "fields":
      comp = filledRequiredFields / totalRequiredFields (0 if 0 required)
    case "files":
      comp = min(1.0, uploadedCount / minRequired)  (skip if no minRequired)
    case "quickLink":
      comp = 0  // Quick Link 不计入完成度
  completionPct += w * comp

return Round(completionPct, 2)  // 0–100
```

---

## Event Design

### OnboardingStartedEvent（新建）

```csharp
// Domain.Shared/Events/OnboardingStartedEvent.cs
public class OnboardingStartedEvent : INotification
{
    public long OnboardingId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EstimatedCompletionDate { get; set; }
    public string TenantId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; }
}
```

发布位置：`OnboardingStatusService.StartOnboardingAsync`，在 `result == true` 后通过 `IMediator.Publish` 同步发布（不在 BackgroundTask 中，确保 Planned 时间在响应前写入）。

```csharp
// OnboardingStatusService.StartOnboardingAsync — 修改点
if (result)
{
    // 同步发布，确保 Planned 时间立即写入
    await _mediator.Publish(new OnboardingStartedEvent
    {
        OnboardingId = id,
        StartDate = entity.StartDate!.Value,
        EstimatedCompletionDate = entity.EstimatedCompletionDate,
        TenantId = _userContext.TenantId,
        UserId = long.TryParse(_userContext.UserId, out var uid) ? uid : 0,
        UserName = _userContext.UserName
    });

    // 现有日志（保持不变）
    _backgroundTaskQueue.QueueBackgroundWorkItem(...);
}
```

### OnboardingStageMovedEvent（新建）

```csharp
// Domain.Shared/Events/OnboardingStageMovedEvent.cs
public class OnboardingStageMovedEvent : INotification
{
    public long OnboardingId { get; set; }
    public long FromStageId { get; set; }
    public long ToStageId { get; set; }
    public string TenantId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; }
}
```

发布位置：现有"move-to-stage"业务逻辑触发点（与 `OnboardingStageCompletedEvent` 的发布位置相邻，不替换它）。

### GanttProjectedTimeRecalcHandler 监听双事件

```csharp
public class GanttProjectedTimeRecalcHandler :
    INotificationHandler<OnboardingStageCompletedEvent>,
    INotificationHandler<OnboardingStageMovedEvent>
{
    // 两个 Handle 方法均调用同一内部方法 RecalcProjectedTimesAsync(onboardingId)
}
```

---

## Frontend Component Design

### GanttChart.vue — SVG 渲染方案

使用纯 SVG，不引入第三方甘特图库：

```
布局：
┌─────────────────────────────────────────────────────────┐
│ [工具栏：Day/Week/Month | Today | < Prev | Next > | 筛选] │
├──────────────────────┬──────────────────────────────────┤
│  Stage 列表列         │  时间轴 SVG 区域                  │
│  (固定宽度 300px)     │  (水平可滚动)                     │
│  ─ 序号              │  ─ 时间刻度行                     │
│  ─ Stage 名称        │  ─ Today 参考线（红色竖线）         │
│  ─ Assignee          │  ─ 每行：                         │
│  ─ Status Badge      │    ① Planned rect（灰色虚线框）    │
│  ─ Required 标记     │    ② Projected rect（实色填充）    │
├──────────────────────┴──────────────────────────────────┤
│  Legend（可收起）                                         │
└─────────────────────────────────────────────────────────┘
```

**SVG 坐标系计算（核心逻辑）：**

```typescript
// 根据视图模式计算单位像素宽度
const UNIT_WIDTH = { day: 40, week: 120, month: 300 }  // px per unit

function dateToX(date: string, viewStart: Date, mode: ViewMode): number {
  const diff = daysBetween(viewStart, parseISO(date))
  return mode === 'day'   ? diff * UNIT_WIDTH.day
       : mode === 'week'  ? (diff / 7) * UNIT_WIDTH.week
       :                    (diff / 30) * UNIT_WIDTH.month
}

// Planned rect：灰色虚线框，半透明
// Projected rect：实色填充，按 GanttStageStatus 颜色
// 两 rect 纵向偏移，Planned 在上（y+2），Projected 在下（y+12），高 14px
```

**颜色映射：**

| GanttStageStatus | Projected 条颜色   | Badge 颜色            |
|-----------------|-------------------|-----------------------|
| NotStarted      | #D9D9D9 (Gray)   | #D9D9D9               |
| Delayed         | #FA8C16 (Orange)  | #FA8C16               |
| InProgress      | #1890FF (Blue)    | #1890FF               |
| Overdue         | #FF4D4F (Red)     | #FF4D4F               |
| Completed（准时）| #52C41A (Green)  | #52C41A               |
| Completed（延迟）| #FA8C16 (Orange) | #FA8C16               |
| isBlocked=true  | #722ED1 条纹叠加  | Purple 覆盖层          |

### GanttThumbnail.vue

- 尺寸：约 400×180px，只读
- 内容：Case 名称、Workflow 名称、"X of Y Stages"、Overall 进度条、Start Date、ETA、当前 Stage 名称/Assignee
- "View Full Chart" 按钮触发 GanttModal
- 数据：复用列表页已缓存数据或轻量计算派生（不发起额外网络请求）

### GanttTour.vue — 5 步引导

| Step | 高亮区域                          | 说明文字                                     |
|------|----------------------------------|----------------------------------------------|
| 1    | GanttSummaryHeader               | "查看 Case 的计划时间与当前预测时间"           |
| 2    | Planned vs Projected 区域对比    | "灰色虚线 = 原始计划；实色 = 当前预测"         |
| 3    | 某 Stage 时间轴条                 | "条形长度代表预计持续天数，颜色代表当前状态"   |
| 4    | Status Badge + ⓘ 图标            | "Hover ⓘ 图标查看状态详情与偏差分析"           |
| 5    | Legend 区域                      | "随时在此查看所有颜色与状态含义"               |

Tour 记录：通过 `IUserTourRecordService`，tourKey = `"gantt-case-tour"`，按 userId 记录，后续打开不再自动显示。

### hover 防抖实现（onboardingList/index.vue）

```typescript
let hoverTimer: ReturnType<typeof setTimeout> | null = null
let currentHoveredRow: OnboardingRow | null = null

const handleRowMouseEnter = (row: OnboardingRow) => {
    hoverTimer = setTimeout(() => {
        currentHoveredRow = row
        ganttStore.fetchGanttData(row.id)
        showGanttThumbnail(row)
    }, 500)
}

const handleRowMouseLeave = () => {
    if (hoverTimer) clearTimeout(hoverTimer)
    hoverTimer = null
    hideGanttThumbnail()
}
```

---

## Error Handling

| 场景                                   | 处理方式                                                  |
|---------------------------------------|----------------------------------------------------------|
| GET gantt — Case 不存在或已软删除      | 404，`CRMException(DataNotFound)`                        |
| GET gantt — 无查看权限                 | 403，`EnsureCaseViewPermissionAsync` 内部抛出             |
| block-stage — 该 Stage 已为 Blocked   | 400，message: "Stage is already blocked"                 |
| unblock-stage — 该 Stage 未被 Blocked | 400，message: "Stage is not blocked"                     |
| block-stage / unblock-stage — 无操作权限 | 403，`EnsureCaseOperatePermissionAsync` 内部抛出        |
| BlockerReason 为空                    | FluentValidation 校验，400                               |
| 修改 Planned 时间的请求               | 400，message: "Planned dates cannot be modified after Case start" |
| GanttPlannedTimeInitHandler 异常      | 记录 Error 日志，不阻断 StartOnboarding 响应（try-catch 内） |
| GanttProjectedTimeRecalcHandler 异常  | 记录 Error 日志，不影响 StageComplete 主流程              |
| 旧 Case（plannedStartDate 为 null）   | 动态推算兜底，不返回错误，前端无感知                       |
| Component 权重之和 ≠ 100             | API 写入时 FluentValidation 校验，400                     |

---

## Testing Strategy

本功能是纯 CRUD + 算法计算场景，PBT 不适用（无复杂解析器/序列化器，核心是时间推算的条件分支逻辑）。采用以下测试策略：

### 后端单元测试（xUnit + Moq + FluentAssertions）

测试文件位置：`Tests/FlowFlex.Tests/Gantt/`

**GanttServiceTests.cs — 核心场景：**

```
ComputePlannedTimes_AllStagesHaveEstimatedDuration_ShouldCalculateCorrectly
  Arrange: 3 stages with duration [7, 5, 3], startDate = 2025-01-01
  Assert:  stage1=[Jan1, Jan7], stage2=[Jan8, Jan12], stage3=[Jan13, Jan15]

ComputePlannedTimes_NullEstimatedDuration_ShouldUseFallback
  Arrange: stage.EstimatedDuration = null, no ETA → fallback = 7
  Assert:  all stages get 7-day planned windows

DeriveGanttStageStatus_IsCompleted_ReturnsCompleted
DeriveGanttStageStatus_StartedAndPastPlannedEnd_ReturnsOverdue
DeriveGanttStageStatus_StartedAndBeforePlannedEnd_ReturnsInProgress
DeriveGanttStageStatus_NotStartedAndPastPlannedStart_ReturnsDelayed
DeriveGanttStageStatus_NotStartedAndBeforePlannedStart_ReturnsNotStarted

ComputeProjectedTimes_BlockedStage_SetsNullForBlockedAndSubsequent
ComputeProjectedTimes_AllPrevCompleted_StartsFromPrevActualEnd

BlockStageAsync_AlreadyBlocked_ThrowsBusinessError
UnblockStageAsync_NotBlocked_ThrowsBusinessError
BlockStageAsync_Success_AppendsBlockerRecord
UnblockStageAsync_Success_FillsResolvedDateAndBlockedDays
```

**GanttPlannedTimeInitHandlerTests.cs：**

```
Handle_OnboardingStartedEvent_WritesPlannedTimesToStagesProgressJson
Handle_WithExistingPlannedDates_ShouldNotOverwrite
```

**GanttProjectedTimeRecalcHandlerTests.cs：**

```
Handle_StageCompleted_RecalculatesProjectedForSubsequentStages
Handle_OnboardingStageMovedEvent_TriggersRecalc
Handle_BlockedStage_SetsNullProjectedFromBlockedStageOnward
```

### 前端单元测试（Jest + @vue/test-utils）

测试文件位置：`src/app/views/onboard/onboardingList/components/__tests__/`

```
GanttChart.spec.ts:
  - 渲染 SVG 时正确计算日期到像素坐标
  - Planned rect 和 Projected rect 均正确渲染
  - Today 参考线位置正确

GanttSummaryHeader.spec.ts:
  - 正偏差时显示警告色
  - 负偏差时显示绿色

ganttStore.spec.ts:
  - fetchGanttData 缓存 Map 正确更新
  - invalidateCache 清除对应 onboardingId 数据
```

### 集成测试要点（人工验证）

- 旧 Case（plannedStartDate = null）GET gantt → 前端正常显示（动态推算兜底）
- Case start → GET gantt 立即可见 Planned 时间（同步发布事件）
- Stage 完成 → GET gantt 后 Projected 时间已更新
- block-stage → GET gantt → Blocked Stage 及后续 Projected 为 null
- unblock-stage → GET gantt → Projected 恢复正常
- Tour seen 记录：首次打开显示 Tour，第二次打开不显示

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Planned 时间连续性

*For any* Case 的所有 Stage 列表（按 order_index 排列），`ComputePlannedTimes` 计算结果中相邻 Stage 满足：Stage(N).plannedStartDate = Stage(N-1).plannedEndDate + 1 天，且 Stage(N).plannedEndDate ≥ Stage(N).plannedStartDate。

**Validates: Requirements 2.2**

### Property 2: Planned 时间覆盖 Case 全周期

*For any* Case，Stage 1 的 `plannedStartDate` = `Case.StartDate`，最后一个 Stage 的 `plannedEndDate` ≥ Stage 1 的 `plannedStartDate`。

**Validates: Requirements 2.2**

### Property 3: EstimatedDuration 聚合

*For any* 所有 Stage 均有有效 `EstimatedDuration` 的 Case，所有 Stage 的 `estimatedDuration` 之和（取整后）= 最后一个 Stage 的 `plannedEndDate` - Case.StartDate 的天数差。

**Validates: Requirements 2.2**

### Property 4: Projected 时间单调性

*For any* 没有 Blocked Stage 的 Case，`ComputeProjectedTimes` 结果中所有 Stage 满足：Stage(N).projectedStartDate = Stage(N-1).projectedEndDate + 1 天。

**Validates: Requirements 3.2**

### Property 5: Blocked 传播性

*For any* Stage S 被标记为 Blocked，Stage S 及 S 之后所有 Stage 的 `projectedStartDate` 和 `projectedEndDate` 均为 null。

**Validates: Requirements 3.3**

### Property 6: GanttStageStatus 与实际时间一致性

*For any* Stage，若 `IsCompleted = true` 则 `ganttStatus = Completed`；若 `actualStartDate != null && today > plannedEndDate` 则 `ganttStatus = Overdue`；规则优先级顺序严格遵循需求定义，不产生歧义状态。

**Validates: Requirements 5.2**

### Property 7: Component 权重合法性

*For any* 已配置 `ComponentWeights` 的 Stage，所有 weight 之和等于 100；若 `ComponentWeights` 为空，则 CompletionPercentage 计算时等权分配不超过 100。

**Validates: Requirements 1.3, 4.2**

### Property 8: CompletionPercentage 边界约束

*For any* Stage，`completionPercentage` 的值始终在 [0, 100] 区间内。

**Validates: Requirements 4.1, 4.2**
