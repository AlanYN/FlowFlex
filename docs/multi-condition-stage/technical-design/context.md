# Context: Multi-Condition Stage — 技术设计

> 更新于 2026-07-03，反映最终实现。

## 技术栈

| 层     | 技术                                                 |
| ------ | ---------------------------------------------------- |
| 后端   | C# / ASP.NET Core / SqlSugar / Microsoft RulesEngine |
| 前端   | Vue 3 + TypeScript + Element Plus + TailwindCSS      |
| 数据库 | PostgreSQL (JSONB for rules/actions)                 |

---

## 核心架构决策

| 决策          | 方案                                   | 理由                                                           |
| ------------- | -------------------------------------- | -------------------------------------------------------------- |
| 评估策略      | First-match-wins（按 order）           | 可预测、可控，代码预留 `SelectWinningCondition` 方法切换为随机 |
| Fallback 存储 | `ff_stage.condition_fallback_stage_id` | Stage 级概念，语义清晰，不依赖具体 condition                   |
| 保存模式      | 前端统一 Save → 拆成多个独立 CRUD 请求 | 避免后端批量事务复杂度，复用现有接口                           |
| 唯一约束      | 已删除                                 | 应用层做上限校验，避免 order swap 时约束冲突                   |
| Order 分配    | 后端 `CreateAsync` 自动 max(order)+1   | 新增永远追加末尾，排序通过独立 reorder 接口                    |

---

## 数据库变更

### Migration: `Migration_20260701000001_AddMultiConditionSupport`

```sql
-- 1. Stage 加 fallback 列
ALTER TABLE ff_stage ADD COLUMN condition_fallback_stage_id BIGINT NULL;

-- 2. Condition 加 order 列
ALTER TABLE ff_stage_condition ADD COLUMN "order" INTEGER NOT NULL DEFAULT 0;

-- 3. 删除唯一约束（允许多 condition）
DROP INDEX IF EXISTS uq_stage_condition;

-- 4. 数据迁移（现有 fallback 搬到 stage）
UPDATE ff_stage s SET condition_fallback_stage_id = sc.fallback_stage_id
FROM ff_stage_condition sc WHERE sc.stage_id = s.id AND sc.is_valid = true AND sc.fallback_stage_id IS NOT NULL;

-- 5. 新索引
CREATE INDEX idx_stage_condition_stage_order ON ff_stage_condition(stage_id, "order") WHERE is_valid = true;
```

---

## 后端文件清单

### 新增文件

| 文件                                                                         | 职责                                       |
| ---------------------------------------------------------------------------- | ------------------------------------------ |
| `SqlSugarDB/Migrations/Migration_20260701000001_AddMultiConditionSupport.cs` | 数据库迁移（含 Up/Down）                   |
| `Application.Contracts/Dtos/OW/StageCondition/ReorderItemDto.cs`             | Reorder + UpdateConditionFallback 请求 DTO |

### 修改文件

| 文件                                                                        | 修改内容                                                                                       |
| --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `Domain/Entities/OW/Stage.cs`                                               | 加 `ConditionFallbackStageId` 字段                                                             |
| `Domain/Entities/OW/StageCondition.cs`                                      | 加 `Order` 字段，`FallbackStageId` 标记 `[IsIgnore]`                                           |
| `Domain.Shared/Const/StageConditionConstants.cs`                            | 加 `MaxConditionsPerStage = 10`                                                                |
| `Application.Contracts/Dtos/OW/StageCondition/StageConditionInputDto.cs`    | 移除 `FallbackStageId`                                                                         |
| `Application.Contracts/Dtos/OW/StageCondition/StageConditionOutputDto.cs`   | 加 `Order`                                                                                     |
| `Application.Contracts/Dtos/OW/StageCondition/ConditionEvaluationResult.cs` | 加 `MatchedConditionId` + `MatchedConditionName`                                               |
| `Application.Contracts/Dtos/OW/Stage/StageOutputDto.cs`                     | 加 `ConditionFallbackStageId`                                                                  |
| `Application.Contracts/IServices/OW/IStageConditionService.cs`              | `GetByStageIdAsync` → 返回 `List`；加 `ReorderAsync`                                           |
| `Application.Contracts/IServices/OW/IStageService.cs`                       | 加 `UpdateConditionFallbackAsync`                                                              |
| `Application/Services/OW/StageCondition/StageConditionService.cs`           | CreateAsync: 移除唯一校验 → 上限校验 + 自动 order；GetByStageIdAsync 返回列表；加 ReorderAsync |
| `Application/Services/OW/StageCondition/RulesEngineService.cs`              | 多 condition 循环评估 (first-match-wins) + `SelectWinningCondition` 策略方法 + Stage fallback  |
| `Application/Services/OW/StageService.cs`                                   | 加 `UpdateConditionFallbackAsync` 实现                                                         |
| `Application/Maps/StageMapProfile.cs`                                       | 加 `ConditionFallbackStageId` 映射                                                             |
| `WebApi/Controllers/OW/StageConditionController.cs`                         | GetByStageId 返回 List + PATCH reorder 接口                                                    |
| `WebApi/Controllers/OW/StageController.cs`                                  | PATCH condition-fallback 接口                                                                  |
| `SqlSugarDB/Migrations/MigrationManager.cs`                                 | 注册新 migration                                                                               |

---

## 前端文件清单

### 新增文件

| 文件                                                                    | 职责                                 |
| ----------------------------------------------------------------------- | ------------------------------------ |
| `src/app/views/onboard/workflow/components/condition/ConditionCard.vue` | 可折叠/展开的单个 condition 编辑卡片 |

### 修改文件

| 文件                                                                           | 修改内容                                                                                                       |
| ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| `src/types/condition.d.ts`                                                     | `StageCondition` 移除 `fallbackStageId`；`FallbackConfig` 改为 `continue/jump`；加 `ReorderItem` 类型          |
| `src/app/apis/ow/index.ts`                                                     | `getConditionByStage` → `getConditionsByStage`（返回数组）；加 `reorderConditions` + `updateConditionFallback` |
| `src/app/views/onboard/workflow/components/condition/StageConditionEditor.vue` | 重写为列表容器（管理多 condition + Fallback Radio + 统一 Save）                                                |
| `src/app/views/onboard/workflow/condition-editor.vue`                          | `open()` 调用传 conditionId 参数，点 condition 节点自动展开对应卡片                                            |
| `src/app/stores/modules/workflowCanvas.ts`                                     | 加 `getConditionsByStageId` + `getConditionCountByStageId` getter；节点数据加 `conditionCount`                 |
| `src/app/components/workflow-canvas/nodes/StageNode.vue`                       | Badge 显示 "N Conditions" 数量                                                                                 |

---

## API 端点

| 方法   | 路径                                                 | 用途                                                            |
| ------ | ---------------------------------------------------- | --------------------------------------------------------------- |
| GET    | `/ow/stage-conditions/v1/by-stage/{stageId}`         | 获取 stage 的 condition 列表（按 order 排序）**[改为返回数组]** |
| POST   | `/ow/stage-conditions/v1`                            | 创建 condition（自动分配 order）                                |
| PUT    | `/ow/stage-conditions/v1/{id}`                       | 更新 condition                                                  |
| DELETE | `/ow/stage-conditions/v1/{id}`                       | 删除 condition                                                  |
| PATCH  | `/ow/stage-conditions/v1/by-stage/{stageId}/reorder` | 批量更新 order                                                  |
| PATCH  | `/ow/stages/v1/{id}/condition-fallback`              | 更新 Stage 的 fallback 配置                                     |

---

## 评估执行流程

```
Stage Complete → POST /evaluate-and-execute/by-onboarding/{id}/stage/{stageId}
  │
  ├── 获取所有 active conditions（按 order 排序）
  │
  ├── 如果无 active condition → 走顺序下一步（忽略 fallback）
  │
  ├── 遍历 conditions（first-match-wins）：
  │   ├── 构建 input data（从 stage 组件收集数据）
  │   ├── 评估 rules（AND/OR 逻辑）
  │   ├── 如果满足 → 执行该 condition 的 actions → 返回结果（停止遍历）
  │   └── 如果不满足 → 继续下一个 condition
  │
  └── 所有 condition 都不满足：
      ├── 取 Stage.ConditionFallbackStageId
      ├── 有值 → GoToStage（Jump to chosen stage）
      └── 无值 → 走顺序下一步（Continue to next stage）
```

---

## 前端保存流程

```
用户点 Save →
  Phase 1: 删除（对比快照，移除的 condition 调 DELETE）
  Phase 2: 创建 + 更新（新 condition 调 POST，修改的调 PUT）
         → POST 返回后将真实 ID 写回本地数组
  Phase 3: Reorder（所有有 ID 的 conditions 按当前顺序统一 PATCH reorder）
  Phase 4: Fallback（如有变化调 PATCH condition-fallback）
```

---

## 策略扩展点

```csharp
// RulesEngineService.cs — 切换为随机只需改这一个方法
private Domain.Entities.OW.StageCondition SelectWinningCondition(
    List<Domain.Entities.OW.StageCondition> matchedConditions)
{
    // First-match-wins: return the condition with lowest order
    return matchedConditions.OrderBy(c => c.Order).First();

    // 未来切换为随机：
    // var random = new Random();
    // return matchedConditions[random.Next(matchedConditions.Count)];
}
```
