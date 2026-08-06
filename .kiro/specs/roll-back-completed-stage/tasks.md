# Implementation Plan: Roll Back Completed Stage

## Overview

本实现计划将 Roll Back Completed Stage 功能拆解为 6 个有序的编码阶段：

1. **数据库 Migration** — 新增 `roll_back_teams` 列，同步 Entity 映射
2. **后端核心逻辑** — 权限校验 + RollBack 服务实现
3. **后端 Controller + DTO 输出** — API 端点 + `CanRollBack` 字段
4. **Stage 配置前端** — Workflow 配置页新增 RollBackTeams 选择器
5. **前端 Roll Back 功能** — Case 详情页 Stage 卡片按钮 + 确认弹窗
6. **后端单元测试与属性测试** — 核心逻辑验证

---

## Tasks

- [x] 1. 数据库 Migration 与 Entity 同步
  - [x] 1.1 创建 Migration 文件，新增 `roll_back_teams` 列
    - 新建 `SqlSugarDB/Migrations/Migration_20260806000001_AddRollBackTeamsToStage.cs`
    - `Up` 方法执行：`ALTER TABLE ff_stage ADD COLUMN IF NOT EXISTS roll_back_teams jsonb;`
    - `Down` 方法执行：`ALTER TABLE ff_stage DROP COLUMN IF EXISTS roll_back_teams;`
    - 两个方法均使用 `IF NOT EXISTS` / `IF EXISTS` 保证幂等
    - _Requirements: 2.1_

  - [x] 1.2 在 MigrationManager.cs 中注册新 Migration
    - 在 `migrations` 数组末尾追加注册：
      `("20260806000001_AddRollBackTeamsToStage", (Action)(() => Migration_20260806000001_AddRollBackTeamsToStage.Up(_db)))`
    - _Requirements: 2.1_

  - [x] 1.3 更新 Stage Entity，新增 RollBackTeams 属性
    - 在 `Domain/Entities/OW/Stage.cs` 新增属性：
      ```csharp
      [SugarColumn(ColumnName = "roll_back_teams", ColumnDataType = "jsonb", IsJson = true)]
      public string RollBackTeams { get; set; }
      ```
    - _Requirements: 2.1_

- [~] 2. Checkpoint — 确认数据库变更基础就绪
  - 确认 MigrationManager 中 Migration 注册正确，Entity 属性已添加，询问用户是否有问题后继续。

- [x] 3. 后端核心逻辑：DTO、服务接口与实现
  - [x] 3.1 创建 RollBackStageInput DTO
    - 新建 `Application.Contracts/Dtos/OW/Onboarding/RollBackStageInput.cs`
    - 包含可选属性 `public string? Reason { get; set; }`，添加 XML 注释
    - _Requirements: 4.2_

  - [x] 3.2 在 IOnboardingStageManagementService 中声明 RollBackStageAsync 方法
    - 打开 `Application.Contracts/IServices/OW/IOnboardingStageManagementService.cs`
    - 新增方法签名：`Task<bool> RollBackStageAsync(long onboardingId, long stageId, RollBackStageInput input);`
    - 添加 XML 文档注释说明参数与返回值
    - _Requirements: 1.1_

  - [x] 3.3 实现 OnboardingStageManagementService.RollBackStageAsync — 权限校验
    - 在 `Application/Services/OW/OnboardingStageManagementService.cs` 中实现 `RollBackStageAsync`
    - 第一步：从仓储加载 Onboarding（不存在则 throw `CRMException(DataNotFound, "Onboarding not found")`）
    - 第二步：从 Onboarding 对应 Workflow 中定位 Stage（不属于 Workflow 则 throw `CRMException(DataNotFound, "Stage not found or does not belong to the current workflow")`）
    - 第三步：调用内部方法 `CheckRollBackPermission`，检查 `stage.RollBackTeams`：
      - 若为 null 或空数组 → throw `CRMException(Forbidden, "该 Stage 未配置 Roll Back 权限")`
      - 获取当前用户团队列表，与 RollBackTeams 求交集，无交集 → throw `CRMException(Forbidden, "您没有执行此操作的权限")`
    - _Requirements: 2.2, 2.3, 2.4_

  - [x] 3.4 实现 OnboardingStageManagementService.RollBackStageAsync — 状态校验与重置
    - 继续实现 `RollBackStageAsync`（接 3.3）
    - 第四步：校验 `OnboardingStageProgress.Status == "Completed"`，否则 throw `CRMException(BusinessError, "只能对已完成的 Stage 执行 Roll Back 操作")`
    - 第五步：重置 `OnboardingStageProgress` 6 个字段：
      - `Status = "InProgress"`
      - `IsCompleted = false`
      - `CompletionTime = null`
      - `CompletedBy = null`
      - `CompletedById = null`
      - `IsCurrent = true`
    - 更新 `Onboarding.CurrentStageId` 为该 StageId，`CurrentStageOrder` 为该 Stage.Order
    - _Requirements: 1.1, 1.2_

  - [x] 3.5 实现 Onboarding 状态联动逻辑
    - 继续实现 `RollBackStageAsync`（接 3.4）
    - 判断 `onboarding.Status == "Completed"` 时，执行：
      - `onboarding.Status = "InProgress"`
      - `onboarding.ActualCompletionDate = null`
    - 调用 `SaveOnboardingChangesAsync(entity)` 持久化所有变更
    - _Requirements: 1.5_

  - [x] 3.6 实现操作日志记录与通知发送（fire-and-forget）
    - 继续实现 `RollBackStageAsync`（接 3.5）
    - 写操作日志：调用 `OperationChangeLogService.LogOperationAsync`，`OperationType = StageReopen (10)`，记录操作者信息、OnboardingId、StageId、StageName、操作时间、Reason
    - 发送通知（fire-and-forget，用 `_ = Task.Run(...)` 包裹，内部 try-catch 记录 `Logger.LogError`，不抛出）：
      - 向被 Roll Back Stage 的当前 Assignee 发通知
      - 向所有 Order 大于该 Stage 的非 Skipped Stage 的 Assignee 发通知
      - 若 Assignee 列表为空则跳过，不产生错误
    - 最终 `return true`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2_

- [x] 4. 后端 Controller 与输出 DTO 变更
  - [x] 4.1 在 OnboardingController 新增 RollBack API 端点
    - 打开 `WebApi/Controllers/OW/OnboardingController.cs`
    - 新增端点：
      ```
      [HttpPost("{onboardingId}/stages/{stageId}/roll-back")]
      ```
    - 注入 `IOnboardingStageManagementService`（已有则复用）
    - 调用 `RollBackStageAsync(onboardingId, stageId, input)` 并返回 `Success(result)`
    - 添加 `[WFEAuthorize]` 授权注解（与同 Controller 其他端点保持一致）
    - _Requirements: 1.1, 2.2_

  - [x] 4.2 在 OnboardingStageProgressOutputDto 中新增 CanRollBack 字段
    - 找到对应的 `OnboardingStageProgressOutputDto`（或 `StageOutputDto`）
    - 新增 `public bool CanRollBack { get; set; }`，添加注释说明该字段由服务层根据 RollBackTeams 与当前用户团队计算
    - _Requirements: 6.2_

  - [x] 4.3 在查询 Stage 进度时填充 CanRollBack 字段
    - 找到返回 Stage 进度列表的服务方法（如 `GetOnboardingStagesAsync` 或类似方法）
    - 在该方法中，对每个 `OnboardingStageProgress` 计算 `CanRollBack`：
      - 获取当前用户团队
      - 与 `stage.RollBackTeams` 求交集，有交集则 `CanRollBack = true`，否则 `false`
      - `RollBackTeams` 为 null 或空时 `CanRollBack = false`
    - 填充到 DTO 后返回
    - _Requirements: 6.2_

- [~] 5. Checkpoint — 后端基础就绪
  - 确认所有后端代码编译通过（`dotnet build`），询问用户是否有调整需求。

- [x] 6. Stage 配置前端：RollBackTeams 选择器
  - [x] 6.1 在 Stage 配置页面新增 RollBackTeams 团队选择器
    - 找到 Workflow Stage 配置表单组件（参考 `OperateTeams`、`ViewTeams` 的实现位置）
    - 在同一区域新增 `RollBackTeams` 多选 Team 选择器，使用与 `OperateTeams` 相同的组件和数据源
    - 绑定数据到 Stage 配置的 `rollBackTeams` 字段
    - 标签文案：`Roll Back Teams`，加 Tooltip 提示："配置后，只有属于这些团队的用户才能对该 Stage 执行 Roll Back。为空时禁止所有人执行 Roll Back。"
    - _Requirements: 2.5_

  - [x] 6.2 确保 Stage 配置保存时 RollBackTeams 正确序列化提交
    - 检查 Stage 配置的保存 API 调用（`updateStage` 或类似函数）
    - 确认 `rollBackTeams` 字段包含在请求 payload 中，类型为字符串数组
    - 如后端 Stage update DTO 尚未包含该字段，则在对应 DTO 中新增 `public string RollBackTeams { get; set; }`
    - _Requirements: 2.1, 2.5_

- [x] 7. 前端 Roll Back 功能：API 函数 + UI 组件
  - [x] 7.1 在 onboarding.ts 中新增 rollBackStage API 函数
    - 打开 `packages/flowFlex-common/src/app/apis/ow/onboarding.ts`
    - 新增函数：
      ```typescript
      export const rollBackStage = (
        onboardingId: string,
        stageId: string,
        reason?: string,
      ) =>
        defHttp.post<boolean>({
          url: `${prefix}/ow/onboardings/${apiVersion}/${onboardingId}/stages/${stageId}/roll-back`,
          data: { reason },
        });
      ```
    - _Requirements: 6.3_

  - [x] 7.2 在 Stage 卡片组件中新增 Roll Back 按钮
    - 找到 Case 详情页中展示 Stage 状态的卡片组件
    - 在 Completed 状态的操作区新增 Roll Back 按钮
    - 显示条件：`stage.status === 'Completed' && stage.canRollBack === true`
    - 按钮样式参考同区域其他操作按钮，文案 `Roll Back`，添加适当 icon
    - 点击时调用下一步实现的确认弹窗逻辑
    - _Requirements: 6.1, 6.2_

  - [x] 7.3 实现 Roll Back 二次确认弹窗与提交逻辑
    - 在 Stage 卡片组件中（或独立 composable/子组件中）实现确认弹窗：
      - 使用 `el-dialog`，标题 `Roll Back Stage`
      - 弹窗正文说明：`此操作将重新打开该 Stage，使其回到 InProgress 状态。`
      - 包含可选 `el-input type="textarea"` 输入 Reason，placeholder `请输入 Roll Back 原因（选填）`
      - 确认按钮：调用 `rollBackStage(onboardingId, stageId, reason)`，期间按钮显示 loading 状态禁止重复点击
      - 成功后：关闭弹窗，刷新 Stage 状态（触发父组件重新获取 Onboarding Stages 数据）
      - 失败后：Axios 拦截器自动展示 `ElMessage.error`（无需额外处理，确认弹窗保持打开）
    - _Requirements: 6.3, 6.4, 6.5_

- [~] 8. Checkpoint — 前端功能就绪
  - 确认前端代码无 TypeScript 编译报错（`pnpm type:check`），询问用户是否有 UI 细节调整。

- [ ] 9. 后端单元测试
  - [ ]\* 9.1 为 RollBackStageAsync 核心流程编写 Happy Path 单元测试
    - 测试文件：`Tests/FlowFlex.Tests/Services/OW/OnboardingStageManagementServiceTests.cs`（已有则追加）
    - Happy path 1：Completed Stage → Roll Back 成功，断言 6 个 Progress 字段已重置
    - Happy path 2：Onboarding.Status == Completed 时联动重置为 InProgress，ActualCompletionDate 为 null
    - 使用 `// Arrange / Act / Assert` 注释结构，Mock 所有仓储依赖
    - _Requirements: 1.1, 1.2, 1.5_

  - [ ]\* 9.2 为 RollBackStageAsync 错误路径编写单元测试
    - Stage 不是 Completed 状态 → 断言 throw BusinessError
    - Stage 不属于该 Workflow → 断言 throw DataNotFound
    - Onboarding 不存在 → 断言 throw DataNotFound
    - _Requirements: 1.3, 1.4_

  - [ ]\* 9.3 为 RollBackTeams 权限校验编写属性测试（FsCheck）
    - 测试文件：`Tests/FlowFlex.Tests/Properties/RollBackPermissionPropertyTests.cs`（新建）
    - **Property 3：RollBackTeams Whitelist 权限语义**（对应 design.md Property 3）
    - 使用 FsCheck 生成任意 `RollBackTeams` 列表和任意用户团队列表，验证：
      - `RollBackTeams` 为 null 或空 → 任何用户均被拒绝
      - `RollBackTeams` 非空且用户团队有交集 → 允许
      - `RollBackTeams` 非空但用户团队无交集 → 拒绝
    - 每个 property 至少 100 次迭代
    - 注释格式：`// Feature: roll-back-completed-stage, Property 3: RollBackTeams Whitelist 权限语义`
    - _Requirements: 2.2, 2.3, 2.4_

  - [ ]\* 9.4 为通知失败不阻断 Roll Back 编写单元测试
    - Mock 通知服务抛出异常
    - 断言 `RollBackStageAsync` 仍返回 `true`，且 Logger.LogError 被调用
    - _Requirements: 3.4_

  - [ ]\* 9.5 为 Onboarding 状态联动编写属性测试（FsCheck）
    - **Property 4：Onboarding 状态联动重置不变量**（对应 design.md Property 4）
    - 生成任意 Status == Completed 的 Onboarding + 其中任意 Completed Stage
    - 执行 RollBack，断言 `Onboarding.Status == "InProgress"` 且 `ActualCompletionDate == null`
    - 注释格式：`// Feature: roll-back-completed-stage, Property 4: Onboarding 状态联动重置不变量`
    - _Requirements: 1.5_

- [~] 10. Final Checkpoint — 确认所有测试通过
  - 运行 `dotnet test` 确认所有单元测试和属性测试通过，运行 `pnpm type:check` 确认前端无类型错误，询问用户是否有最终调整。

---

## Notes

- 任务标记 `*` 为可选（测试相关），可为 MVP 跳过
- 任务 1 是所有后端任务的基础依赖，必须最先执行
- 任务 3.3~3.6 同属一个 Service 方法的实现，按顺序在同一文件中完成
- `CanRollBack` 字段由后端在查询时计算返回，避免前端额外权限 API 调用
- 通知发送使用 fire-and-forget 模式，失败只记 Log 不影响 RollBack 响应
- FsCheck 属性测试需引用 `FsCheck.Xunit` NuGet 包（如项目中已有则复用）

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["3.1", "3.2"] },
    { "id": 2, "tasks": ["3.3"] },
    { "id": 3, "tasks": ["3.4"] },
    { "id": 4, "tasks": ["3.5"] },
    { "id": 5, "tasks": ["3.6", "6.1"] },
    { "id": 6, "tasks": ["4.1", "4.2", "6.2"] },
    { "id": 7, "tasks": ["4.3"] },
    { "id": 8, "tasks": ["7.1"] },
    { "id": 9, "tasks": ["7.2"] },
    { "id": 10, "tasks": ["7.3"] },
    { "id": 11, "tasks": ["9.1", "9.2", "9.3", "9.4", "9.5"] }
  ]
}
```
