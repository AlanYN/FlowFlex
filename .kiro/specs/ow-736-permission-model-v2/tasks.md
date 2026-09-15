# Implementation Plan: OW-736 权限模型重构 V2

## Overview

本计划将 OW-736 权限模型重构拆解为 12 个 Phase，共 40+ 个编码子任务。核心路径：

1. **数据层**（Phase 1–2）：Migration + Entity + OnboardingStageProgress 字段扩展
2. **计算层**（Phase 3）：PermissionCalculator 纯函数辅助类
3. **接口层**（Phase 4）：DTO 变更（Workflow / Stage / Onboarding / CaseStage）
4. **业务层**（Phase 5）：Service 改造（快照写入、三层取交集、Roll Back）
5. **映射层**（Phase 6）：AutoMapper Profile 1:1 字段映射
6. **API 层**（Phase 7）：两个新端点
7. **前端**（Phase 8–11）：四个 UI 组件改造/新建
8. **测试**（Phase 12）：单元测试 + Property-Based Tests + 集成测试

---

## Tasks

- [x] 1. Phase 1 — 数据库 Migration 与 Entity 变更

  - [x] 1.1 Migration_202609080001 — ff_workflow 新增 Runtime 字段 + Workflow Entity 更新
    - 在 `SqlSugarDB/Migrations/` 创建 `Migration_202609080001_AddWorkflowRuntimePermission.cs`
    - SQL：`ALTER TABLE ff_workflow ADD COLUMN IF NOT EXISTS runtime_use_same_as_template BOOLEAN NOT NULL DEFAULT TRUE, runtime_view_permission_mode SMALLINT NOT NULL DEFAULT 0, runtime_view_teams JSONB NULL, runtime_operate_teams JSONB NULL`
    - `Down()` 移除上述四列（`DROP COLUMN IF EXISTS`）
    - 同步更新 `Domain/Entities/OW/Workflow.cs`：新增 `RuntimeUseSameAsTemplate`、`RuntimeViewPermissionMode`、`RuntimeViewTeams`、`RuntimeOperateTeams` 四个带 `[SugarColumn]` 的属性
    - 注意：**不加** `RuntimeUseSameTeamForOperate`（Workflow Runtime 层设计不需要）
    - _Requirements: 2.1, 2.4, 20.1, 20.3_

  - [x] 1.2 Migration_202609080002 — ff_stage 新增 Runtime/Template 字段 + Stage Entity 更新
    - 创建 `Migration_202609080002_AddStageRuntimePermission.cs`
    - SQL：`ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS template_use_same_as_workflow BOOLEAN NOT NULL DEFAULT TRUE, runtime_use_same_as_workflow BOOLEAN NOT NULL DEFAULT TRUE, runtime_view_permission_mode SMALLINT NOT NULL DEFAULT 0, runtime_view_teams JSONB NULL, runtime_operate_teams JSONB NULL, runtime_use_same_team_for_operate BOOLEAN NOT NULL DEFAULT TRUE`
    - `Down()` 移除上述六列
    - 同步更新 `Domain/Entities/OW/Stage.cs`：新增对应六个带 `[SugarColumn]` 属性；`roll_back_teams` 列名/字段名不变，仅更新语义注释（语义重定位为 Runtime 层）
    - _Requirements: 3.1, 3.6, 20.1, 20.3_

  - [x] 1.3 Migration_202609080003 — ff_onboarding 新增快照字段 + SQL 回填 + C# JSONB 批量回填
    - 创建 `Migration_202609080003_AddOnboardingPermissionSnapshot.cs`
    - **Step 1 (DDL)**：`ALTER TABLE ff_onboarding ADD COLUMN IF NOT EXISTS max_view_permission_mode SMALLINT NULL, max_view_teams JSONB NULL, max_operate_teams JSONB NULL`
    - **Step 2 (SQL 回填，~1h)**：`UPDATE ff_onboarding o SET max_view_permission_mode = CASE WHEN w.runtime_use_same_as_template THEN w.view_permission_mode ELSE w.runtime_view_permission_mode END, ...` WHERE `o.max_view_permission_mode IS NULL AND o.is_valid = TRUE`（幂等条件）
    - **Step 3 (C# JSONB 回填，~7h)**：在 `Up()` 方法中分批（每批 100 条）查询 `is_valid=true` 且 `stages_progress_json IS NOT NULL` 的 Case，反序列化 `stages_progress_json`，检查 `MaxStageViewTeams == null`（幂等条件），调用 `PermissionCalculator.ComputeStageEffectiveRuntime` 计算，写入 `MaxStage*` 五个字段后序列化回 `stages_progress_json`
    - 如果关联 Workflow 不存在（`is_valid=false`），记录 Warning 日志并跳过，不中断整个 Migration
    - `Down()` 仅移除三个新增列，**不回滚**回填数据
    - 同步更新 `Domain/Entities/OW/Onboarding.cs`：新增 `MaxViewPermissionMode`（nullable）、`MaxViewTeams`、`MaxOperateTeams` 三个 `[SugarColumn]` 属性
    - **注意：此 Migration 依赖 Migration 1 的 `runtime_use_same_as_template` 列已存在**
    - _Requirements: 4.1, 4.4, 19.1–19.6, 20.2, 20.3_

  - [x] 1.4 MigrationManager 注册三个 Migration（按顺序）
    - 在 `SqlSugarDB/MigrationManager.cs` 的 `migrations` 数组末尾追加三条注册：
      ```csharp
      ("202609080001_AddWorkflowRuntimePermission", ...),
      ("202609080002_AddStageRuntimePermission", ...),
      ("202609080003_AddOnboardingPermissionSnapshot", ...),
      ```
    - 顺序不可颠倒（Migration 3 的 SQL 回填依赖 Migration 1 的列）
    - _Requirements: 20.1, 20.2_

- [x] 2. Phase 2 — OnboardingStageProgress C# 类字段扩展

  - [x] 2.1 新增 Case Stage 实际权限配置字段（14 个属性）
    - 在 `Domain/Entities/OW/OnboardingStageProgress.cs` 新增 14 个属性：
      `StagePermissionInheritFromWorkflowStage`（bool?，默认 true）、`StageViewPermissionMode`、`StageViewPermissionSubjectType`、`StageViewTeams`、`StageViewUsers`、`StageUseSameTeamForOperate`、`StageOperatePermissionSubjectType`、`StageOperateTeams`、`StageOperateUsers`、`StageRollBackInherit`（bool，默认 true）、`StageRollBackUseSameAsOperate`（bool，默认 true）、`StageRollBackPermissionSubjectType`、`StageRollBackTeams`、`StageRollBackUsers`
    - _Requirements: 5.1_

  - [x] 2.2 新增 Case Stage 权限快照字段（5 个属性）
    - 新增只读快照属性：`MaxStageViewPermissionMode`（ViewPermissionModeEnum?）、`MaxStageViewTeams`（List\<string\>）、`MaxStageOperatePermissionMode`（ViewPermissionModeEnum?）、`MaxStageOperateTeams`（List\<string\>）、`MaxStageRollBackTeams`（List\<string\>）
    - 快照需同时存 Mode 和 Teams（原因：下游三层取交集需要 Mode 判断访问语义）
    - _Requirements: 5.2, 5.3_

  - [ ]* 2.3 验证 JSON 反序列化向后兼容
    - 编写单元测试：对缺失上述新字段的旧格式 JSON 进行反序列化，验证结果中新字段均为 null（不抛异常）
    - 验证 `StagePermissionInheritFromWorkflowStage = null` 在业务逻辑中等同于 `true`（向后兼容旧数据）
    - _Requirements: 5.4, 5.8_

- [x] 3. Phase 3 — PermissionCalculator 静态辅助类（新建）

  - [x] 3.1 实现 ComputeWorkflowEffectiveRuntime + WorkflowEffectiveRuntime record
    - 新建 `Application/Services/Shared/PermissionCalculator.cs`
    - 定义 `public record WorkflowEffectiveRuntime(ViewPermissionModeEnum ViewMode, List<string> ViewTeams, List<string> OperateTeams)`
    - 实现 `public static WorkflowEffectiveRuntime ComputeWorkflowEffectiveRuntime(Workflow workflow)`：
      - `RuntimeUseSameAsTemplate = true` → 返回 Template 字段 `(ViewPermissionMode, ViewTeams, OperateTeams)`
      - `RuntimeUseSameAsTemplate = false` → 返回 `(RuntimeViewPermissionMode, RuntimeViewTeams, RuntimeOperateTeams)`
    - 纯函数，无副作用，无 DB 访问
    - _Requirements: 2.2, 2.3, 14.1, 14.4_

  - [x] 3.2 实现 ComputeStageEffectiveRuntime + StageEffectiveRuntime record
    - 定义 `public record StageEffectiveRuntime(ViewPermissionModeEnum ViewMode, List<string> ViewTeams, List<string> OperateTeams, List<string> RollBackTeams)`
    - 实现 `public static StageEffectiveRuntime ComputeStageEffectiveRuntime(Stage stage, WorkflowEffectiveRuntime workflowRuntime)`：
      - `RuntimeUseSameAsWorkflow = true` → 返回 `workflowRuntime` 的 View/Operate，以及 Stage 自身 `roll_back_teams`
      - `RuntimeUseSameAsWorkflow = false` + `RuntimeUseSameTeamForOperate = true` → OperateTeams = 独立配置的 RuntimeViewTeams
      - `RuntimeUseSameAsWorkflow = false` + `RuntimeUseSameTeamForOperate = false` → OperateTeams = `RuntimeOperateTeams`
      - 独立配置时需确保结果 Teams ⊆ workflowRuntime Teams（否则抛 `CRMException`）
    - _Requirements: 3.4, 3.5, 3.7, 14.2_

  - [x] 3.3 实现 IsSubsetOf 方法
    - `public static bool IsSubsetOf(List<string> teams, List<string> maxTeams)`
    - 空集/null → 始终返回 true（空集是任意集合的子集）
    - maxTeams 为 null/空 → teams 也为空/null 时返回 true，否则返回 false
    - _Requirements: 14.3_

  - [ ]* 3.4 PermissionCalculator 单元测试（覆盖所有边界 + Property-Based Tests）
    - 使用 xUnit + FsCheck（`FsCheck.Xunit` 包，新增至 Tests 项目）
    - **ComputeWorkflowEffectiveRuntime**：`RuntimeUseSameAsTemplate = true/false` 两条路径各至少 3 个具体示例
    - **ComputeStageEffectiveRuntime**：继承/独立配置/`RuntimeUseSameTeamForOperate` 三条路径
    - **IsSubsetOf**：null、空集、完全子集、超出范围四类边界
    - **Property-Based Test — Property 2（确定性）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 2: Workflow Effective Runtime 计算确定性
      [Property]
      public Property ComputeWorkflowEffectiveRuntime_IsDeterministic(...)
      ```
    - **Property-Based Test — Property 9（IsSubsetOf 空集单位元）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 9: IsSubsetOf 空集单位元
      [Property]
      public Property IsSubsetOf_EmptyOrNull_AlwaysTrue(...)
      ```
    - **Property-Based Test — Property 5（Stage Runtime 子集约束）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 5: Stage Effective Runtime 子集约束
      ```
    - _Requirements: 14.3, 14.4_

- [x] 4. Phase 4 — DTO 变更

  - [x] 4.1 WorkflowInputDto / WorkflowOutputDto 新增 Runtime 字段
    - `WorkflowInputDto`（`Application.Contracts/Dtos/OW/Workflow/`）新增：`RuntimeUseSameAsTemplate`（bool，默认 true）、`RuntimeViewPermissionMode`、`RuntimeViewTeams`、`RuntimeOperateTeams`
    - 注意：**不加** `RuntimeUseSameTeamForOperate`
    - `WorkflowOutputDto` 在上述基础上新增三个 Effective 只读字段：`EffectiveRuntimeViewPermissionMode`、`EffectiveRuntimeViewTeams`、`EffectiveRuntimeOperateTeams`（AutoMapper 不计算，Service 层手动填充）
    - 现有 Template 字段（`ViewPermissionMode`、`ViewTeams` 等）保持不变
    - _Requirements: 10.1, 10.2, 10.4, 10.5_

  - [x] 4.2 StageInputDto / StageOutputDto 新增 Template 继承标志 + Runtime 字段
    - `StageInputDto` 新增：`TemplateUseSameAsWorkflow`（bool，默认 true）、`RuntimeUseSameAsWorkflow`（bool，默认 true）、`RuntimeViewPermissionMode`、`RuntimeViewTeams`、`RuntimeOperateTeams`、`RuntimeUseSameTeamForOperate`（bool，默认 true）
    - 保留现有 `RollBackTeams` 字段（语义重定位为 Runtime 层，字段名/API 契约不变）
    - `StageOutputDto` 在上述基础上新增四个 Effective 只读字段：`EffectiveRuntimeViewPermissionMode`、`EffectiveRuntimeViewTeams`、`EffectiveRuntimeOperateTeams`、`EffectiveRollBackTeams`
    - _Requirements: 11.1, 11.2, 11.3_

  - [x] 4.3 OnboardingInputDto / OnboardingOutputDto 新增继承标志 + 快照只读字段
    - `OnboardingInputDto` 新增：`UseWorkflowRuntimePermission`（bool，默认 true）
    - `OnboardingOutputDto` 新增：`UseWorkflowRuntimePermission`、`MaxViewPermissionMode`（nullable）、`MaxViewTeams`、`MaxOperateTeams`（三个只读快照字段）
    - 现有 Case 权限字段（`ViewPermissionMode` 等）保持不变
    - _Requirements: 12.1, 12.2, 12.5_

  - [x] 4.4 新建 CaseStagePermissionInputDto
    - 在 `Application.Contracts/Dtos/OW/Onboarding/` 新建 `CaseStagePermissionInputDto.cs`
    - 包含：`InheritFromWorkflowStage`（bool）、`ViewPermissionMode`（nullable）、`ViewPermissionSubjectType`、`ViewTeams`、`ViewUsers`、`UseSameTeamForOperate`（bool）、`OperatePermissionSubjectType`、`OperateTeams`、`OperateUsers`、`RollBackInherit`（bool）、`RollBackUseSameAsOperate`（bool）、`RollBackPermissionSubjectType`、`RollBackTeams`、`RollBackUsers`
    - _Requirements: 13.1_

  - [x] 4.5 OnboardingStageProgressDto 扩展
    - 新增 14 个实际配置字段（与 `OnboardingStageProgress` C# 类对应）
    - 新增 3 个 Effective 只读字段：`EffectiveViewTeams`、`EffectiveOperateTeams`、`EffectiveRollBackTeams`（Service 层计算后手动填充）
    - _Requirements: 13.5_

- [x] 5. Checkpoint — Phase 1–4 完成后验证
  - 确认所有 Entity 字段与 Migration SQL 列名一致（PascalCase ↔ snake_case）
  - 确认 DTO 中 **不存在** `RuntimeUseSameTeamForOperate`（Workflow 层）
  - 确认 `PermissionCalculator` 无任何 Repository 依赖
  - 确认三个 Migration 在 MigrationManager 中按正确顺序注册
  - 请求用户确认是否有疑问。

- [x] 6. Phase 5 — Service 层改造

  - [x] 6.1 OnboardingCrudService.CreateAsync — 快照写入逻辑
    - 在 `Application/Services/OW/OnboardingServices/OnboardingCrudService.cs` 的 `CreateAsync` 中，于 `db.Insertable` 之前新增快照写入步骤：
      1. 加载 Workflow 实体及其所有 Stage
      2. 调用 `PermissionCalculator.ComputeWorkflowEffectiveRuntime(workflow)` 写入 `onboarding.MaxViewPermissionMode`、`MaxViewTeams`、`MaxOperateTeams`
      3. foreach Stage → 调用 `PermissionCalculator.ComputeStageEffectiveRuntime(stage, wfRuntime)` → 写入对应 `StageProgress.MaxStage*` 五个字段
    - 快照写入与 Case 创建在同一事务中（原子性）；任何异常触发整体回滚
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 6.2 CasePermissionService — 继承模式读快照 + 非继承模式子集校验
    - 在 `Application/Services/OW/Permission/CasePermissionService.cs` 中：
      - 继承模式（`UseWorkflowRuntimePermission = true`）：**不查 Workflow 实体**，直接读 `onboarding.MaxViewTeams` / `MaxOperateTeams` 快照
      - 忽略 Input DTO 中提供的 View/Operate Teams（方案 A）
      - 非继承模式（`UseWorkflowRuntimePermission = false`）：调用 `PermissionCalculator.IsSubsetOf(view_teams, max_view_teams)`，不满足则抛 `CRMException(ErrorCodeEnum.PermissionBoundaryExceeded, ...)`
    - 对现有所有 Case 权限配置，重构前后 `CanView` / `CanOperate` 结果必须一致
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [x] 6.3 StagePermissionService — 三层取交集改造（Layer 1 快照 + Layer 2 EffectiveRuntime + Layer 3 Case Stage）
    - 在 `Application/Services/OW/Permission/StagePermissionService.cs` 中改造权限检查：
      - **Layer 1**：从 `Onboarding.MaxViewTeams`（快照）读取，**不调用** Workflow Repository
      - **Layer 2**：调用 `PermissionCalculator.ComputeStageEffectiveRuntime(stage, wfRuntime).ViewTeams`
      - **Layer 3**：`StagePermissionInheritFromWorkflowStage == true（或 null）` → 用 `MaxStageViewTeams` 快照；否则用 `StageViewTeams` / `StageViewUsers`
    - Layer 1 拒绝后立即返回，不再检查 Layer 2/3
    - `POST /ow/permissions/v1/check` 的契约（返回 `CanView` / `CanOperate`）保持不变
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [x] 6.4 Roll Back 权限校验改造（StageRollBackInherit 三条路径）
    - 在 StagePermissionService（或专用 RollBackPermissionService）中：
      - `StageRollBackInherit = true` → 用 `MaxStageRollBackTeams` 快照
      - `StageRollBackInherit = false AND StageRollBackUseSameAsOperate = true` → 用有效的 Case Stage Operate Teams
      - `StageRollBackInherit = false AND StageRollBackUseSameAsOperate = false` → 用 `StageRollBackTeams` / `StageRollBackUsers`
      - 不在有效 Roll Back 集合中 → 抛 HTTP 403 `CRMException(ErrorCodeEnum.RollBackPermissionDenied, ...)`
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 6.5 ReapplyWorkflowPermissionService — Reapply 方法
    - 在 `Application/Services/OW/Permission/` 中新建或扩展 Service，实现 `ReapplyWorkflowPermissionAsync(long onboardingId)`：
      1. 加载 Onboarding 实体及父 Workflow
      2. 调用 `PermissionCalculator.ComputeWorkflowEffectiveRuntime(workflow)` 重新计算
      3. 覆写 `max_view_permission_mode`、`max_view_teams`、`max_operate_teams`（**不覆盖** Stage 快照）
      4. 持久化更新
    - _Requirements: 4.5, 13.6_

- [x] 7. Phase 6 — AutoMapper Profiles

  - [x] 7.1 WorkflowMapProfile — 新增 Runtime 字段 1:1 映射
    - 在 `Application/Maps/WorkflowMapProfile.cs`（或对应文件）中，为 `Workflow → WorkflowOutputDto` 添加 `RuntimeUseSameAsTemplate`、`RuntimeViewPermissionMode`、`RuntimeViewTeams`、`RuntimeOperateTeams` 的 1:1 映射
    - **不在 Profile 中计算 Effective 字段**；Effective 字段由 Service 层调用 `PermissionCalculator` 后手动赋值
    - `WorkflowInputDto → Workflow` 反向映射同步补充
    - _Requirements: 6.6, 14.5_

  - [x] 7.2 StageMapProfile — 新增 Template/Runtime 字段 1:1 映射
    - `Stage → StageOutputDto`：新增六个字段的 1:1 映射
    - `StageInputDto → Stage` 反向映射同步补充
    - Effective 字段不在 Profile 中计算
    - _Requirements: 6.6, 14.5_

  - [x] 7.3 OnboardingMapProfile — 新增继承标志 + 快照字段 1:1 映射
    - `Onboarding → OnboardingOutputDto`：新增 `UseWorkflowRuntimePermission`、`MaxViewPermissionMode`、`MaxViewTeams`、`MaxOperateTeams` 的 1:1 映射
    - `OnboardingStageProgress → OnboardingStageProgressDto`：新增全部 Case Stage 实际配置字段 + 快照字段的 1:1 映射
    - Effective 字段（`EffectiveViewTeams` 等）不在 Profile 中计算
    - _Requirements: 6.6, 14.5_

  - [ ]* 7.4 验证 Service 层手动填充 Effective 字段
    - 在 WorkflowService、StageService、OnboardingService 的相关查询方法中，确认映射后有 `PermissionCalculator` 调用并手动赋值 Effective 字段
    - 编写单元测试：mock Workflow 实体 → 调用 Service → 断言 OutputDto 中的 Effective 字段值正确
    - _Requirements: 6.6, 14.5_

- [x] 8. Phase 7 — API 端点

  - [x] 8.1 PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}（新增）
    - 在 `WebApi/Controllers/OW/OnboardingController.cs` 新增方法 `UpdateStagePermissionsAsync`
    - `[HttpPut("{id}/stage-permissions/{stageId}")]`，权限：`[WFEAuthorize(PermissionConsts.Case.Update)]`
    - 校验逻辑：
      - `InheritFromWorkflowStage = false AND ViewPermissionMode = VisibleTo AND 无 Teams/Users` → 400 验证错误
      - `ViewTeams ⊄ MaxStageViewTeams` → `CRMException(ErrorCodeEnum.PermissionBoundaryExceeded, ...)`
    - 继承状态（`InheritFromWorkflowStage = true`）→ 清空所有独立配置字段
    - 返回 `SuccessResponse<bool>`
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [x] 8.2 POST /ow/onboardings/v1/{id}/reapply-workflow-permission（新增）
    - 在 `OnboardingController.cs` 新增方法 `ReapplyWorkflowPermissionAsync`
    - `[HttpPost("{id}/reapply-workflow-permission")]`，权限：`[WFEAuthorize(PermissionConsts.Case.Update)]`
    - 调用 6.5 的 `ReapplyWorkflowPermissionAsync(id)` 服务方法
    - 返回 `SuccessResponse<bool>`
    - _Requirements: 13.6_

- [x] 9. Checkpoint — Phase 5–7 完成后验证
  - 确认 Layer 1 权限检查不调用 Workflow Repository（通过 mock 断言）
  - 确认 `CasePermissionService` 继承模式不触发额外 DB 查询（mock 断言）
  - 确认两个新端点路由正确（`ow/` 前缀）
  - 请求用户确认是否有疑问。

- [x] 10. Phase 8 — 前端 Workflow Permissions 弹窗改造

  - [x] 10.1 Template Permissions 区块（View + Operate）
    - 在 `WorkflowPermissionsDialog.vue`（或对应路径）中，确认 Template Permissions 区块结构：
      - View：下拉（Public / Visible to / Invisible to）+ 对应 Team 多选 + "EFFECTIVE TEAMS" 预览
      - Operate：`[✓] Use same team that have view permission` checkbox；未勾选时展示独立 Team 多选 + "EFFECTIVE TEAMS" 预览
    - 绑定 API：`WorkflowOutputDto.ViewPermissionMode / ViewTeams / OperateTeams / UseSameTeamForOperate`
    - _Requirements: 15.2, 15.8_

  - [x] 10.2 Runtime Permissions 区块（Use same checkbox + 独立配置，无 Use same operate checkbox）
    - 新增 "Runtime Permissions" 区块，副标题："Set the maximum runtime access for cases and stages using this workflow."
    - `[ ] Use same permissions as Template Permissions`（默认 false）：勾选时 View + Operate 只读展示继承自 Template 的 Effective Teams
    - 未勾选时展开：Runtime View 下拉（Public / Visible to / Invisible to）+ Team 多选 + EFFECTIVE TEAMS 预览；Runtime Operate Teams 独立 Team 多选（**无 "Use same" checkbox**）
    - 绑定新增 DTO 字段：`RuntimeUseSameAsTemplate`、`RuntimeViewPermissionMode`、`RuntimeViewTeams`、`RuntimeOperateTeams`、`EffectiveRuntimeViewTeams`、`EffectiveRuntimeOperateTeams`
    - _Requirements: 15.3, 15.4, 15.5, 15.6, 15.7_

- [x] 11. Phase 9 — 前端 Stage Permissions Tab 重做

  - [x] 11.1 Template Permissions 区块（继承/独立切换）
    - 在 Stage Edit 弹窗的 Permissions Tab 中，新增 "Template Permissions" 区块
    - `[✓] Use same permissions as Workflow Template Permissions`（默认 true）：勾选时只读预览 "Matches the workflow's Template view/operate permission. EFFECTIVE TEAMS: [...]"
    - 未勾选时展开 View 下拉（Public / Visible to / Invisible to）+ Operate（含 "Use same team that have view permission" checkbox）
    - _Requirements: 16.2, 16.3, 16.4_

  - [x] 11.2 Runtime Permissions 区块（继承/独立切换）
    - 新增 "Runtime Permissions" 区块
    - `[✓] Use same permission as Workflow Runtime Permissions`（默认 true）：勾选时只读预览
    - 未勾选时展开：Runtime View 下拉 + Runtime Operate Teams 独立选择器（**无 "Use same" checkbox**）
    - _Requirements: 16.5, 16.6_

  - [x] 11.3 Roll Back Teams 区块（choosable 限制在 Effective Runtime Operate Teams 内）
    - 新增 "Roll Back Teams" 区块
    - 说明文字："Can only include teams that also have Runtime Operate permission."
    - Team 多选的 choosable range 动态限制在 `EffectiveRollBackTeams`（来自 `StageOutputDto.EffectiveRollBackTeams`）范围内
    - EFFECTIVE TEAMS 预览
    - _Requirements: 16.7, 16.8_

- [x] 12. Phase 10 — 前端 Case Access Control 区块改造

  - [x] 12.1 固定说明文字 + Use same checkbox
    - 在 Create/Edit Case 弹窗的 Access Control 区块顶部添加固定说明文字（文案见 Requirements 17.1）
    - 新增 "Use same permission as Workflow Runtime Permissions" checkbox（绑定 `UseWorkflowRuntimePermission`）
    - 勾选时 View + Operate 只读展示来自 `max_*` 快照的 Effective Teams
    - _Requirements: 17.1, 17.2, 17.3_

  - [x] 12.2 choosable tree 限制逻辑（MaxViewTeams / MaxOperateTeams）
    - 非继承模式下：View 选择器（Visible to / Invisible to 模式）的 choosable range 限制在 `MaxViewTeams` 内
    - Operate 选择器（非 Use same 时）的 choosable range 限制在当前 Case View 已选 Teams 内
    - Private 模式：仅展示 Individual Users 选择器
    - _Requirements: 17.4, 17.5, 17.6_

- [x] 13. Phase 11 — 前端 CaseStagePermissionDialog 全新组件

  - [x] 13.1 继承状态 UI（5 种 View/Operate/RollBack 只读展示）
    - 新建 `src/app/components/global/CaseStagePermissionDialog/index.vue`
    - 继承状态（`StagePermissionInheritFromWorkflowStage = true`，默认）：展示 "Use same permission as workflow stage runtime" checkbox（选中）；View / Operate / Roll Back 区块只读展示 EFFECTIVE TEAMS（来自 `MaxStage*` 快照）
    - 标题 "Case Stage Permission"，副标题显示 Stage 名称
    - _Requirements: 18.1, 18.2, 18.3_

  - [x] 13.2 独立配置 UI（View / Operate / RollBack 三个区块）
    - 取消继承后展开独立配置：
      - View：下拉（Public / Visible to / Invisible to，**不含 Private**）+ Team/User 选择器
      - Operate：`[✓] Use same teams and users that have view permission`；未勾选时展开，choosable range = `MaxStageOperateTeams ∩ Case Stage View Effective Teams`
      - Roll Back：`[✓] Use same teams and users that have operate permission`；未勾选时展开，choosable range = effective Case Stage Operate Teams，支持 User Teams / Individual Users 切换
    - _Requirements: 18.4, 18.6, 18.7, 18.8_

  - [x] 13.3 保存校验逻辑 + API 调用
    - 前端表单校验：`ViewPermissionMode = VisibleTo AND 无 Teams/Users` → 展示验证错误，阻止提交
    - Case Stage 层不允许选择 Private 模式（下拉选项中移除 Private）
    - 保存时调用 `PUT /ow/onboardings/v1/{id}/stage-permissions/{stageId}`（Body：`CaseStagePermissionInputDto`）
    - 成功后刷新 Stage Progress 显示
    - _Requirements: 18.5, 18.9_

  - [x] 13.4 shield icon 入口集成（Case Progress 面板 Stage 卡片）
    - 在 Case 详情页右侧 Case Progress 面板的每个 Stage 卡片上，添加护盾图标（shield icon）按钮
    - 点击 → 打开 `CaseStagePermissionDialog`，传入 `caseId` 和 `stageId`
    - _Requirements: 18.1_

- [ ] 14. Phase 12 — 独立测试任务

  - [ ]* 14.1 PermissionCalculator 单元测试（含 Property-Based Tests）
    - 在 `Tests/FlowFlex.Tests/` 中创建 `PermissionCalculatorTests.cs`
    - 覆盖：`ComputeWorkflowEffectiveRuntime` 两条路径、`ComputeStageEffectiveRuntime` 三条路径、`IsSubsetOf` 四类边界
    - Property-Based Tests 使用 `FsCheck.Xunit`（Property 2、Property 5、Property 9）
    - 每个 Property 注释标注：`// Feature: ow-736-permission-model-v2, Property {N}: {title}`
    - _Requirements: 14.3, 14.4_

  - [ ]* 14.2 StagePermissionService 三层逻辑单元测试
    - 创建 `StagePermissionServiceTests.cs`
    - 覆盖：三层均通过 → `CanView = true`；Layer 1/2/3 各自拒绝的独立场景；Layer 1 来源验证（mock Workflow Repository，断言未被调用）
    - Roll Back 权限四种计算路径各一个测试用例
    - **Property-Based Test — Property 6（三层取交集单调性）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 6: 三层权限取交集单调性
      ```
    - _Requirements: 8.1–8.6_

  - [ ]* 14.3 CasePermissionService 快照读取 + 子集校验单元测试
    - 创建 `CasePermissionServiceTests.cs`
    - 继承模式：mock `onboarding.MaxViewTeams`，断言 Workflow Repository **未被调用**
    - 非继承模式越界：验证 `CRMException` 被抛出
    - **Property-Based Test — Property 7（权限校验输入越界拒绝）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 7: 权限校验输入越界拒绝
      ```
    - _Requirements: 7.1–7.4_

  - [ ]* 14.4 OnboardingCrudService 快照写入单元测试
    - 创建 `OnboardingCrudServiceTests.cs`（或扩展现有）
    - Case 创建后断言：`Onboarding.MaxViewTeams` == `PermissionCalculator.ComputeWorkflowEffectiveRuntime(workflow).ViewTeams`
    - 每个 StageProgress 的 `MaxStageViewTeams` == 对应 Stage 的 `ComputeStageEffectiveRuntime` 输出
    - **Property-Based Test — Property 3（快照写入完整性）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 3: 快照写入完整性（Round-trip）
      ```
    - **Property-Based Test — Property 4（快照不可变性）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 4: 快照不可变性
      ```
    - _Requirements: 6.1–6.5_

  - [ ]* 14.5 Migration 幂等性验证（重复执行不覆盖已有快照）
    - 在测试项目中为 Migration_202609080003 编写幂等性测试：
      - 已有 `max_view_permission_mode IS NOT NULL` 的记录，重复执行 Migration `Up()` → 值不变
      - 已有 `MaxStageViewTeams != null` 的 StageProgress JSON，重复执行 → 值不变
    - **Property-Based Test — Property 10（历史数据回填幂等性）**：
      ```csharp
      // Feature: ow-736-permission-model-v2, Property 10: 历史数据回填幂等性
      ```
    - _Requirements: 19.3, 20.3_

  - [ ]* 14.6 E2E 集成测试
    - **场景 1 — Case 创建快照写入**：创建 Case → 查询 `ff_onboarding.max_view_teams` → 修改 Workflow Runtime Permission → 再次查询 → 值不变（Property 4 验证）
    - **场景 2 — Reapply**：调用 `POST /ow/onboardings/v1/{id}/reapply-workflow-permission` → 验证 `max_*` 字段已更新为最新 Workflow Runtime
    - **场景 3 — 三层权限 E2E**：构造三层权限配置 → 调用 `POST /ow/permissions/v1/check` → 验证结果符合交集逻辑
    - **注意：此测试依赖 Phase 7（API 端点）完成**
    - _Requirements: 8.1–8.6, 4.5_

- [ ] 15. Final Checkpoint — 所有任务完成后验证
  - 确认所有测试通过（`dotnet test`）
  - 确认 FsCheck Property Tests 至少运行 100 次迭代
  - 确认 `PermissionCalculator` 无 Repository 依赖（grep 检查）
  - 确认前端 Case Stage 层无 Private 选项
  - 请求用户确认是否有疑问。

---

## Notes

- 任务标注 `*` 为可选测试任务，可跳过以加快 MVP 交付速度
- Phase 3（PermissionCalculator）是 Phase 5/6 的硬前置依赖，务必优先完成
- Migration 3 中 C# JSONB 回填约 7h，是整个计划工时最重的单体任务，建议单独排期
- AutoMapper Profile 只做 1:1 字段映射，**禁止**在 Profile 中注入 Repository 或调用 PermissionCalculator
- Workflow Runtime 层**无** `RuntimeUseSameTeamForOperate`，Stage Runtime 层**有**，两者不对称，实现时需特别注意
- Roll Back Teams 的 choosable range 约束必须同时在前端（交互限制）和后端（保存校验）双重保证

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "2.1", "2.2"] },
    { "id": 2, "tasks": ["1.4", "2.3", "3.1", "3.2", "3.3"] },
    { "id": 3, "tasks": ["3.4", "4.1", "4.2", "4.3", "4.4", "4.5"] },
    { "id": 4, "tasks": ["6.1", "6.2", "6.3", "6.4", "6.5", "7.1", "7.2", "7.3"] },
    { "id": 5, "tasks": ["7.4", "8.1", "8.2"] },
    { "id": 6, "tasks": ["10.1", "10.2", "11.1", "11.2", "11.3", "12.1", "12.2", "13.1", "13.2"] },
    { "id": 7, "tasks": ["13.3", "13.4", "14.1", "14.2", "14.3", "14.4", "14.5"] },
    { "id": 8, "tasks": ["14.6"] }
  ]
}
```

---

## Hotfixes & Out-of-Plan Changes（计划外 Bug 修复记录）

> 以下是开发测试阶段发现并修复的问题，均超出原始任务计划范围。按发现顺序记录，每条包含：问题描述、根本原因、修复位置。

---

### HF-01 前端 Runtime Permissions Team 选择器不显示（NewWorkflowForm）

**问题**：Edit Workflow → Permissions → Runtime Permissions 独立配置模式下，Team 输入框完全不可见。

**根本原因**：
1. `NewWorkflowForm.vue` 的 `<script>` 里**缺少 `import FlowflexUserSelector`**，Vue 静默忽略未知组件，两侧 selector 均未渲染。
2. `el-select` 选中后写回的 `runtimeViewPermissionMode` 可能是字符串 `"1"` 而非数字 `1`，导致 `shouldShowRuntimeViewTeams` 严格比较失败，Team 块不显示。

**修复**：
- `packages/flowFlex-common/src/app/views/onboard/workflow/components/NewWorkflowForm.vue`
  - 新增 `import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue'`
  - `shouldShowRuntimeViewTeams` 改用 `Number()` 转换后比较
  - el-select 加 `@change="(v) => { formData.runtimeViewPermissionMode = Number(v); }"`

---

### HF-02 FlowflexUserSelector 空状态边框不可见

**问题**：空状态下 FlowflexUserSelector 在嵌套 dialog / Wujie 微前端 shadow DOM 中边框消失，组件看起来不存在。

**根本原因**：`el-input__wrapper` 用 CSS 变量 `--el-input-border-color` 实现 box-shadow 边框，CSS 变量在某些上下文中失效。

**修复**：
- `packages/flowFlex-common/src/app/components/form/flowflexUser/index.vue`
  - 可编辑模式 wrapper div 加 `border border-gray-300 rounded-lg bg-white` 作为保底样式

---

### HF-03 Edit Workflow 保存后 Runtime Permissions 被重置（前端提交漏字段）

**问题**：Edit Workflow 保存后重新打开，"Use same permissions as Template Permissions" 始终被勾选，Runtime 独立配置丢失。

**根本原因**：`workflow/index.vue` 的 `updateWorkflow` 函数构建 `params` 时**漏掉了四个 Runtime 字段**：`runtimeUseSameAsTemplate`、`runtimeViewPermissionMode`、`runtimeViewTeams`、`runtimeOperateTeams`，后端使用 DTO 默认值 `true` 保存。

**修复**：
- `packages/flowFlex-common/src/app/views/onboard/workflow/index.vue`
  - `updateWorkflow` 的 `params` 对象追加上述四个字段

---

### HF-04 EFFECTIVE TEAMS 显示 ID 而非名称（多处）

**问题**：以下位置 EFFECTIVE TEAMS 显示原始 snowflake ID，而非 team 名称：
1. Edit Workflow → Runtime Permissions 继承/独立两种模式
2. Edit Stage → Template Permissions / Runtime Permissions 继承展示
3. Edit Stage → EFFECTIVE TEAMS 文字行

**根本原因**：所有展示计算属性直接调用 `.join(', ')` 拼接 ID 数组，没有经过 ID→名称解析。

**修复**：
- `NewWorkflowForm.vue`：新增 `menuStore`、`teamNameMap`、`buildNameMap`、`resolveNames`；所有展示 computed 改为 `resolveNames(ids).join(', ')`
- `StagePermissions.vue`：同上，新增 name resolver，修复 5 处展示 computed

---

### HF-05 Edit Case 保存后 Access Control 权限被重置

**问题**：Edit Case 取消 "Use same permission as Workflow Runtime Permissions"，保存后重新打开仍显示勾选状态。

**根本原因（两层）**：
1. **数据库缺列**：`ff_onboarding` 表缺少 `use_workflow_runtime_permission` 列，Migration 001/002/003 均未建此列。
2. **SqlSugar 跳过 false 值**：`SafeUpdateOnboardingAsync` 的正常路径通过 `UpdateAsync(entity)` 更新，SqlSugar 可能将 `bool false` 当默认值跳过；fallback raw SQL 路径也未包含该列。

**修复**：
- `SqlSugarDB/Migrations/Migration_202609140001_AddUseWorkflowRuntimePermissionToOnboarding.cs`（新建）：`ALTER TABLE ff_onboarding ADD COLUMN IF NOT EXISTS use_workflow_runtime_permission BOOLEAN NOT NULL DEFAULT TRUE`
- `MigrationManager.cs`：末尾注册新 Migration
- `OnboardingCrudService.cs` → `SafeUpdateOnboardingAsync`：
  - 正常路径：`UpdateAsync(entity)` 后追加 raw SQL 强制写入 `use_workflow_runtime_permission`
  - fallback with stages_progress_json：raw SQL 加入该列
  - fallback without stages_progress_json：raw SQL 加入该列

---

### HF-06 Runtime Permissions Operate 可选范围不受 View 约束（多处）

**问题**：以下位置 Runtime Operate selector 可以选择所有 team，不受 Runtime View 已选 teams 限制：
1. Edit Workflow → Runtime Permissions → Operate
2. Edit Stage → Runtime Permissions → Operate

**根本原因**：两处 `FlowflexUserSelector` 均未传 `:choosable-tree-data`。

**修复**：
- `NewWorkflowForm.vue`：新增 `runtimeOperateChoosableTreeData` ref + `buildRuntimeOperateChoosableTree` 函数；监听 `runtimeViewPermissionMode` 和 `runtimeViewTeams` 变化自动重建；Operate selector 绑定 `:choosable-tree-data`
- `StagePermissions.vue`：同上逻辑，Runtime Operate selector 绑定 `:choosable-tree-data="runtimeOperateChoosableTreeData"`

---

### HF-07 Edit Stage Inherited EFFECTIVE TEAMS 显示不正确（WorkflowService 未填充 Effective 字段）

**问题**：Edit Stage → Runtime Permissions → 继承模式下，EFFECTIVE TEAMS (View) 始终显示 "All teams"，不反映 Workflow 实际配置。

**根本原因**：`WorkflowService.GetByIdAsync` 在 `_mapper.Map` 后**未调用 `PermissionCalculator.ComputeWorkflowEffectiveRuntime`**，`EffectiveRuntimeViewTeams`、`EffectiveRuntimeOperateTeams`、`EffectiveRuntimeViewPermissionMode` 三个字段始终为空。

**修复**：
- `Application/Services/OW/WorkflowService.cs`
  - 新增 `using FlowFlex.Application.Services.Shared`
  - `GetByIdAsync` 在 mapper.Map 后追加：`var effectiveRuntime = PermissionCalculator.ComputeWorkflowEffectiveRuntime(entity); result.EffectiveRuntimeViewPermissionMode = effectiveRuntime.ViewMode; ...`

---

### HF-08 Create Case 弹窗 Access Control 继承模式显示 "All teams"

**问题**：Create Case 选择 Workflow 后，Access Control 继承模式的 View/Operate Effective Teams 显示 "All teams (Public)"，不反映 Workflow 实际 Runtime 权限。

**根本原因**：新建 Case 时 `formData.maxViewTeams = []`（没有快照来源），`CasePermissionSelector` 收到空数组显示 "All teams"。Workflow 的 `effectiveRuntimeViewTeams` 只在 `GetByIdAsync` 时填充，但列表接口 `getWorkflowList` 不填充。

**修复**：
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/index.vue`
  - 新增 `import { getWorkflowDetail }` 
  - Workflow `el-select` 加 `@change="handleCreateCaseWorkflowChange"`
  - 新增 `handleCreateCaseWorkflowChange(workflowId)`：调 `getWorkflowDetail(id)`，将返回的 `effectiveRuntimeViewTeams`、`effectiveRuntimeOperateTeams`、`effectiveRuntimeViewPermissionMode` 写入 `formData.maxViewTeams`、`formData.maxOperateTeams`、`formData.maxViewPermissionMode`
  - `handleNewOnboarding` 里对默认 Workflow 也提前调一次

---

### HF-09 Case Stage Permission Dialog 继承模式显示 "All teams"（PascalCase vs camelCase 字段名不匹配）

**问题**：`CaseStagePermissionDialog` 继承模式下 EFFECTIVE TEAMS 始终显示 "All teams (Public)"，即使快照数据正确。

**根本原因**：
- `stages_progress_json` 由 `System.Text.Json` 序列化，字段名为 **PascalCase**（`MaxStageViewPermissionMode`）
- HTTP API 层 Newtonsoft.Json 未配置 `CamelCaseNamingPolicy`，DTO 字段名也是 PascalCase
- `CaseStagePermissionDialog` 所有字段访问使用 **camelCase**（`d.maxStageViewPermissionMode`），导致取到 `undefined`，mode 检查 `undefined === null || undefined` 为 true，返回 "All teams"

**修复**：
- `packages/flowFlex-common/src/app/components/global/CaseStagePermissionDialog/index.vue`
  - 新增 `getField(d, camelKey)` helper：先尝试 camelCase，找不到则 fallback PascalCase
  - `inheritViewDisplay`、`inheritOperateDisplay`、`inheritRollBackDisplay`、`inheritViewEmptyLabel`、`inheritOperateEmptyLabel` 全部改用 `getField`
  - `initFormData` 改用 `getField`
  - `buildViewChoosableTree`、`buildOperateChoosableTree` 改用 `getField`

---

### HF-10 旧 Case stages_progress_json 为空时 MaxStage* 快照不填充

**问题**：旧 Case（`stages_progress_json = []`）首次打开 Case Stage Permission Dialog 时，快照为空，显示 "All teams"。

**根本原因**：`EnsureStagesProgressInitializedAsync` 遇到空数组时调用 `InitializeStagesProgressAsync` 初始化 Stage Progress，但**不调用** `WriteStagePermissionSnapshotsAsync`，MaxStage* 字段始终为空。

**修复**：
- `OnboardingStageProgressService.cs`
  - 构造函数新增 `IWorkflowRepository workflowRepository` 依赖
  - using 新增 `FlowFlex.Application.Services.Shared`
  - `EnsureStagesProgressInitializedAsync` 空数组初始化后追加：拉取 Workflow 实体，调 `PermissionCalculator.ComputeStageEffectiveRuntime`，填充所有 StageProgress 的 `MaxStage*` 字段，调 `SafeUpdateOnboardingAsync` 持久化

---

### HF-11 Stage Runtime Operate 始终等于 View（RuntimeUseSameTeamForOperate 默认值问题）

**问题**：Stage Runtime 独立配置时，`MaxStageOperateTeams` 与 `MaxStageViewTeams` 相同，而非使用 `RuntimeOperateTeams`。

**根本原因**：
- `Stage.RuntimeUseSameTeamForOperate` 列默认值为 `TRUE`
- `StagePermissions.vue` 的 `emitUpdate` 未输出此字段，后端始终用 DTO 默认值 `true` 保存
- `PermissionCalculator.ComputeStageEffectiveRuntime` 遇到 `RuntimeUseSameTeamForOperate = true` 时将 `operateTeams = viewTeams`
- 但设计文档（Requirement 16.6）明确：Stage Runtime Operate **无 "Use same" checkbox**，Operate 始终独立配置

**修复**：
- `StagePermissions.vue` → `emitUpdate`：追加 `runtimeUseSameTeamForOperate: false`（设计上永远 false）
- `PermissionCalculator.cs` → `ComputeStageEffectiveRuntime` 独立配置分支：移除 `if (stage.RuntimeUseSameTeamForOperate)` 逻辑，**始终使用 `stage.RuntimeOperateTeams`**（忽略该字段）

---

### HF-12 StageEffectiveRuntime 缺少 OperateMode 字段导致 MaxStageOperatePermissionMode 取值错误

**问题**：`OnboardingCrudService` 和 `OnboardingStageProgressService` 两处均写了 `stageProgress.MaxStageOperatePermissionMode = stageRuntime.ViewMode`，将 Operate 的 mode 错误地复用为 View 的 mode。

**根本原因**：`StageEffectiveRuntime` record 只有 `ViewMode`，没有独立的 `OperateMode` 字段，导致调用方只能用 ViewMode 凑数。

**修复**：
- `PermissionCalculator.cs`
  - `StageEffectiveRuntime` record 新增 `OperateMode` 字段
  - 继承模式：`OperateMode = workflowRuntime.ViewMode`
  - 独立配置：`OperateMode = stage.RuntimeViewPermissionMode`
- `OnboardingCrudService.cs`：`MaxStageOperatePermissionMode = stageRuntime.ViewMode` → `stageRuntime.OperateMode`
- `OnboardingStageProgressService.cs`：同上

---

### HF-13 Stage Runtime EFFECTIVE TEAMS 展示 ID 而非名称（StagePermissions）

**问题**：`StagePermissions.vue` 中所有 EFFECTIVE TEAMS 文字展示（Template View/Operate 继承展示、Runtime 继承展示、Roll Back 展示、独立配置 EFFECTIVE TEAMS）均显示 snowflake ID。

**根本原因**：所有展示 computed 直接调用 `.join(', ')`，未经过名称解析。

**修复**：
- `StagePermissions.vue`
  - 新增 `teamNameMap`、`buildNameMap`、`resolveNames` helper（与 `NewWorkflowForm.vue` 相同模式）
  - 在已有 `menuStore` 初始化后追加 `menuStore.getFlowflexUserDataWithCache('').then(tree => teamNameMap.value = buildNameMap(...))`
  - 修复 5 处展示 computed：`effectiveTemplateViewDisplay`、`effectiveTemplateOperateDisplay`、`workFlowEffectiveRuntimeViewDisplay`、`workFlowEffectiveRuntimeOperateDisplay`、`effectiveRollBackDisplay`、`runtimeViewEffectiveDisplay`、`runtimeOperateEffectiveDisplay`
