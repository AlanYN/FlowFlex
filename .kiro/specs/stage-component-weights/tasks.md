# Implementation Plan: Stage Component Weights

## Overview

为 Stage 编辑界面增加 Component 权重配置功能。分 8 个阶段推进：后端 DTO 层、后端业务逻辑、后端单元测试、前端类型定义、前端 StageComponentsSelector 核心改造、前端 StageForm 集成、前端 API 类型更新、前端单元测试。

前置状态确认：
- `Stage.cs` 已有 `ComponentWeights`（JSONB，OW-705 Migration 已建列）✅
- `GanttService` 已有 `ParseComponentWeights` + 私有 `ComponentWeightEntry` 类 ✅
- `ComponentWeightItem` DTO、`StageInputDto.ComponentWeights`、`StageOutputDto.ComponentWeights`、`StageMapProfile` 映射均尚未建立 ❌
- `StageService.UpdateAsync` 无权重校验与持久化逻辑 ❌
- 前端全部尚未实现 ❌
- 测试项目无 FsCheck 依赖，需补充 ❌

---

## Tasks

- [ ] 1. Phase 1：后端数据模型与 DTO

  - [ ] 1.1 确认 `Stage.cs` 上 `ComponentWeights` 属性已存在（无需修改）
    - 读取 `Domain/Entities/OW/Stage.cs`，确认 `[SugarColumn(ColumnName = "component_weights", ColumnDataType = "jsonb", IsJson = true)] public string ComponentWeights` 已存在
    - 若不存在则补充该属性，不需要新建 Migration（列已由 `20260819000001_AddGanttFieldsToStage` 建立）
    - _Requirements: 6.1_

  - [ ] 1.2 新建 `ComponentWeightItem.cs` DTO
    - 创建文件 `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Stage/ComponentWeightItem.cs`
    - 声明 `public class ComponentWeightItem`，包含属性：`string Type`、`string Id`、`string Name`、`int Weight`（范围 0–100）
    - 添加 XML 注释说明 Type 取值（`"fields" | "checklist" | "questionnaire" | "files" | "quickLink"`）和 Id 规则（fields 类型固定为 `"fields"`）
    - _Requirements: 6.1_

  - [ ] 1.3 `StageInputDto.cs` 新增 `ComponentWeights` 字段
    - 在 `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Stage/StageInputDto.cs` 末尾添加：
      `public List<ComponentWeightItem>? ComponentWeights { get; set; }`
    - 添加 XML 注释：`null = 不更新现有权重；非 null 且非空时所有权重之和必须等于 100`
    - _Requirements: 6.3_

  - [ ] 1.4 `StageOutputDto.cs` 新增 `ComponentWeights` 字段
    - 在 `packages/flowFlex-backend/Application.Contracts/Dtos/OW/Stage/StageOutputDto.cs` 末尾添加：
      `public List<ComponentWeightItem>? ComponentWeights { get; set; }`
    - 添加 XML 注释：`null = 未配置权重`
    - _Requirements: 6.5_

  - [ ] 1.5 `StageMapProfile.cs` 新增反序列化映射
    - 在 `packages/flowFlex-backend/Application/Maps/StageMapProfile.cs` 的 `Stage → StageOutputDto` 映射中添加：
      ```csharp
      .ForMember(dest => dest.ComponentWeights, opt => opt.MapFrom(src =>
          string.IsNullOrWhiteSpace(src.ComponentWeights)
              ? null
              : TryDeserializeComponentWeights(src.ComponentWeights)))
      ```
    - 新增私有静态方法 `TryDeserializeComponentWeights(string json)`：反序列化失败时记录 Warning 并返回 null（使用现有 `LogWarning` 辅助方法）
    - 同时在 `StageInputDto → Stage` 映射中 `Ignore` `ComponentWeights` 列（权重持久化由 Service 层手动处理，不通过 AutoMapper 写入）
    - _Requirements: 6.5_

- [ ] 2. Phase 2：后端业务逻辑

  - [ ] 2.1 `StageService.UpdateAsync`：校验 WeightSum == 100
    - 在 `packages/flowFlex-backend/Application/Services/OW/StageService.cs` 的 `UpdateAsync` 方法中，在 `ValidateTeamSelectionsAsync` 调用之后、`UseTranAsync` 之前插入校验段：
      ```csharp
      // Validate component weights if provided
      if (input.ComponentWeights != null && input.ComponentWeights.Count > 0)
      {
          var invalidWeights = input.ComponentWeights.Where(w => w.Weight < 0 || w.Weight > 100).ToList();
          if (invalidWeights.Any())
              throw new CRMException(ErrorCodeEnum.CustomError,
                  $"Component weight value must be between 0 and 100");
          var weightSum = input.ComponentWeights.Sum(w => w.Weight);
          if (weightSum != 100)
              throw new CRMException(ErrorCodeEnum.CustomError,
                  $"Component weights must sum to 100. Current sum: {weightSum}");
      }
      ```
    - _Requirements: 6.4_

  - [ ] 2.2 `StageService.UpdateAsync`：事务块内序列化 ComponentWeights
    - 在事务块内 `_mapper.Map(input, stageInTransaction)` 之后添加：
      ```csharp
      // Persist component weights (null = no change; empty list = clear)
      if (input.ComponentWeights != null)
      {
          stageInTransaction.ComponentWeights = input.ComponentWeights.Count > 0
              ? JsonSerializer.Serialize(input.ComponentWeights, _jsonOptions)
              : null;
      }
      ```
    - _Requirements: 6.1, 6.3_

  - [ ] 2.3 `GanttService.ComputeCompletionPercentage`：完善加权计算与孤立记录过滤
    - 在 `packages/flowFlex-backend/Application/Services/OW/GanttService.cs` 的 `ParseComponentWeights` 方法中：
      - 将 `ComponentWeightEntry` 私有类替换为直接使用 `ComponentWeightItem`（从 `FlowFlex.Application.Contracts.Dtos.OW.Stage` 引入），对齐 DTO 类型（字段名 Type/Id/Name/Weight 已一致）
      - 在解析出 items 后增加孤立记录过滤：仅保留 `item.Id` 与当前 components 中实际存在的 ID 匹配的记录（fields 固定匹配 components 中 key="fields" 的项；checklist/questionnaire 等按 id 字符串匹配对应 ChecklistIds/QuestionnaireIds 转字符串后的集合）
    - 在 `ComputeCompletionPercentage` 开头处补充快速返回：
      - 若 `components` 为空 → 返回 `0m`（组件为空不等同于 100，已有逻辑维持）
      - 若所有 components 的 key 均为 quicklink/quicklinks → 返回 `100m`
    - _Requirements: 8.1, 8.2, 8.3_

  - [ ] 2.4 Checkpoint — 编译后端确保无错误
    - 确保所有测试通过，询问用户是否有疑问。

- [ ] 3. Phase 3：后端单元测试

  - [ ] 3.1 为测试项目添加 FsCheck.Xunit NuGet 依赖
    - 在 `packages/flowFlex-backend/Tests/FlowFlex.Tests/FlowFlex.Tests.csproj` 中添加：
      `<PackageReference Include="FsCheck.Xunit" Version="3.1.0" />`
    - _Requirements: testing infrastructure_

  - [ ]* 3.2 新建 `ComponentWeightItem_SerializationTests.cs`（Property 3）
    - 文件路径：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/OW/ComponentWeightItem_SerializationTests.cs`
    - **Property 3: ComponentWeights 序列化 round-trip**
    - **Validates: Requirements 6.1, 6.5**
    - 使用 FsCheck Arbitrary 生成有效的 `List<ComponentWeightItem>`（每条 Type/Id/Name 非空，Weight 在 0–100，整体 Sum = 100），序列化后反序列化，断言字段值等价
    - 同时包含边界值 EXAMPLE 测试：Weight=0、Weight=100、单条合计=100

  - [ ]* 3.3 新建 `StageService_ComponentWeights_Tests.cs`（EXAMPLE tests）
    - 文件路径：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/OW/StageService_ComponentWeights_Tests.cs`
    - 测试用例覆盖：
      - `UpdateAsync_WeightSumNot100_ThrowsCRMException`：提供 WeightSum=80 的 ComponentWeights，断言抛出 CRMException 且包含 "must sum to 100" 消息
      - `UpdateAsync_WeightSumEquals100_SavesSuccessfully`：提供 WeightSum=100 的 ComponentWeights，断言 UpdateAsync 返回 true，且 `_stageRepository.UpdateAsync` 被调用
      - `UpdateAsync_NullComponentWeights_PreservesExistingWeights`：input.ComponentWeights = null，断言 stage.ComponentWeights 保持原值不变
      - `UpdateAsync_EmptyComponentWeights_ClearsWeights`：input.ComponentWeights = empty list，断言 stage.ComponentWeights 被设置为 null
    - _Requirements: 6.3, 6.4_

  - [ ]* 3.4 新建 `GanttService_ComputeCompletionPercentage_Tests.cs`（EXAMPLE + Property 5）
    - 文件路径：`packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/OW/GanttService_ComputeCompletionPercentage_Tests.cs`
    - 测试用例覆盖（EXAMPLE）：
      - `N0Components_Returns0`：components 为空，返回 0
      - `AllQuickLinkComponents_Returns100`：所有组件为 quickLink，返回 100
      - `WithConfiguredWeights_ReturnsWeightedResult`：已知权重配置（questionnaire 60 + checklist 40），预期返回加权后的完成度
      - `NullComponentWeights_UsesFallbackEqualDistribution`：`stage.ComponentWeights = null`，验证平均分配路径被走到
      - `OrphanWeightRecords_AreIgnored`：ComponentWeights 中包含 ID 不在 components 里的记录，验证孤立记录被忽略
    - **Property 5: ComputeCompletionPercentage 结果范围 [0, 100]**（FsCheck）：
      - **Validates: Requirements 8.1, 8.2**
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 4. Phase 4：前端类型定义

  - [ ] 4.1 `types/onboard.d.ts` 新增 `ComponentWeightItem` 接口，扩展 `ComponentsData`
    - 在 `packages/flowFlex-common/src/types/onboard.d.ts` 中新增接口：
      ```typescript
      export interface ComponentWeightItem {
          type: 'fields' | 'checklist' | 'questionnaire' | 'files' | 'quickLink';
          id: string;
          name: string;
          weight: number; // integer 0–100
      }
      ```
    - 在 `ComponentsData` 接口末尾新增可选字段：`componentWeights?: ComponentWeightItem[];`
    - _Requirements: 6.1, 6.5_

- [ ] 5. Phase 5：前端 StageComponentsSelector 核心改造

  - [ ] 5.1 新增 `componentWeights` 响应式状态和工具函数
    - 在 `packages/flowFlex-common/src/app/views/onboard/workflow/components/StageComponentsSelector.vue` 的 `<script setup>` 中添加：
      - `const componentWeights = ref<Map<string, number>>(new Map())`
      - `function getWeightKey(type: string, id: string): string` → 返回 `\`${type}_${id}\``
      - `function getWeight(type: string, id: string): number` → 从 Map 取值，默认返回 0
      - `function setWeight(type: string, id: string, value: number): void` → 边界修正 `Math.min(100, Math.max(0, Math.round(value)))` 后写入 Map，并调用 `emitWeights()`
    - _Requirements: 1.2, 1.3, 1.4_

  - [ ] 5.2 实现 `getWeightableItems`（排除 quickLink）
    - 新增函数 `getWeightableItems(): SelectedItem[]`：返回 `selectedItems.value.filter(i => i.type !== 'quickLink')`
    - _Requirements: 2.2, 1.6_

  - [ ] 5.3 实现 `applyFallbackWeights`（均分非 QuickLink，Quick Link 固定 0）
    - 新增函数 `applyFallbackWeights(): void`：
      - 取 `getWeightableItems()`，若为空则所有权重置 0
      - 否则 `base = Math.floor(100 / count)`，`remainder = 100 % count`，第一个实例权重为 `base + remainder`，其余为 `base`
      - quickLink 类型实例权重写入 0
    - _Requirements: 1.6_

  - [ ] 5.4 watch `props.modelValue.componentWeights`，加载已保存数据或触发 fallback
    - 添加 `watch(() => props.modelValue.componentWeights, (saved) => { ... }, { immediate: true })`
    - 若 `saved` 非空：过滤孤立记录（仅保留 `isItemInSelectedList(item.type, item.id)` 为 true 的条目），写入 `componentWeights`
    - 若 `saved` 为空：调用 `applyFallbackWeights()`
    - _Requirements: 1.5, 1.6, 7.4_

  - [ ] 5.5 实现 `balanceWeights`（均分非 QuickLink，余数补第一个）
    - 新增函数 `balanceWeights(): void`，逻辑与 `applyFallbackWeights` 相同（均分非 quickLink），完成后调用 `emitWeights()`
    - _Requirements: 2.2_

  - [ ] 5.6 实现 `emitWeights`（拼装 `ComponentWeightItem[]` 并 emit）
    - 新增函数 `emitWeights(): void`：遍历 `selectedItems.value`，拼装 `ComponentWeightItem[]`（id 字段：fields 类型用 `"fields"`，其他用 `element.key`），通过 `emit('update:modelValue', { ...props.modelValue, componentWeights: weights })` 传出
    - _Requirements: 6.1_

  - [ ] 5.7 WeightEditor 模板：每个 SelectedItem 卡片内新增 Slider + 数字输入框行
    - 在现有每个 SelectedItem 渲染卡片内（Portal Access 行之前），添加：
      ```html
      <div class="border-t px-3 py-2 flex items-center gap-3">
          <el-icon class="text-gray-500 flex-shrink-0"><Scale /></el-icon>
          <span class="text-xs text-gray-500 flex-shrink-0">Weight</span>
          <el-slider :model-value="getWeight(element.type, element.key)" :min="0" :max="100" :step="1"
              class="flex-1" size="small" @input="(v) => setWeight(element.type, element.key, Number(v))" />
          <el-input-number :model-value="getWeight(element.type, element.key)" :min="0" :max="100"
              :precision="0" :step="1" size="small" class="w-20 flex-shrink-0"
              @change="(v) => setWeight(element.type, element.key, v ?? 0)" />
          <span class="text-xs text-gray-500 flex-shrink-0">%</span>
      </div>
      ```
    - 引入 `Scale` 图标（Element Plus icons）
    - _Requirements: 1.1, 1.2_

  - [ ] 5.8 Balance 按钮：SelectedItems 区域右上角，空列表时禁用
    - 在 SelectedItems 标题行右侧添加：
      ```html
      <el-button size="small" :disabled="selectedItems.length === 0" @click="balanceWeights">
          <el-icon class="mr-1"><Scale /></el-icon> Balance
      </el-button>
      ```
    - _Requirements: 2.1, 2.3_

  - [ ] 5.9 联动：新增 Component 时权重初始化为 0，删除时从 Map 移除
    - 在现有 `toggleField`/`addComponent`/`removeComponent`（或等效函数）调用后：
      - 新增时：`componentWeights.value.set(getWeightKey(type, id), 0)`，再调用 `emitWeights()`
      - 删除时：`componentWeights.value.delete(getWeightKey(type, id))`，再调用 `emitWeights()`
    - _Requirements: 7.1, 7.2_

  - [ ] 5.10 TotalWeightIndicator：底部合计行，绿色/橙色状态
    - 新增 `computed` 属性：
      - `weightSum`：对所有非 quickLink 实例求权重之和
      - `isWeightSumValid`：`selectedItems.length === 0 || weightSum.value === 100`
    - 在 SelectedItems 列表底部添加 TotalWeightIndicator 模板：
      - `isWeightSumValid` 为 true → 绿色勾 + "Total weight 100%"
      - 否则 → 橙色警告 + 差值提示文字（short 或 over）+ 当前合计值
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [ ] 5.11 PreviewPanel：以 100% 假设完成度展示每行公式，含进度条
    - 新增 `computed` 属性 `completionPreview`：对 `selectedItems` 每项计算 `{ name, weight, subtotal }`（subtotal = weight × 1.0 = weight，因假设 ComponentCompletion=100%）
    - 新增 `computed` 属性 `totalCompletion`：`Math.round(Σ subtotal * 10) / 10`
    - 在 TotalWeightIndicator 下方添加 PreviewPanel 模板：
      - 每行格式：`[name] · 100% done  [weight]% × 100% = [weight].0%`
      - 底部总计 + `<el-progress :percentage="totalCompletion" />`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ] 5.12 Checkpoint — 前端开发服务器可正常运行，选中 Component 时 WeightEditor 正确渲染
    - 确保所有 TypeScript 无类型错误，询问用户是否有疑问。

- [ ] 6. Phase 6：前端 StageForm 集成

  - [ ] 6.1 `formData` 新增 `componentWeights` 字段
    - 在 `packages/flowFlex-common/src/app/views/onboard/workflow/components/StageForm.vue` 中，`formData` 的 `ref` 初始值里增加 `componentWeights: [] as ComponentWeightItem[]`
    - 导入 `ComponentWeightItem` 类型（从 `#/onboard` 导入）
    - _Requirements: 6.1_

  - [ ] 6.2 `updateComponentsData` 同步接收 `componentWeights`
    - 在 `updateComponentsData(val: ComponentsData)` 函数内追加：
      `formData.value.componentWeights = val.componentWeights ?? []`
    - _Requirements: 6.1_

  - [ ] 6.3 `onMounted` 初始化时恢复 `componentWeights`
    - 在加载已有 Stage 数据的 `onMounted`/watch 逻辑中，对 `componentWeights` 字段做恢复：
      `formData.value.componentWeights = (props.stage as any)?.componentWeights ?? []`
    - _Requirements: 6.5_

  - [ ] 6.4 `submitForm` 前端校验（WeightSum≠100 时报错阻断）
    - 在现有权限校验之后、API 调用之前插入：
      ```typescript
      if (formData.value.components.length > 0) {
          const weightSum = formData.value.componentWeights
              .filter(w => w.type !== 'quickLink')
              .reduce((s, w) => s + w.weight, 0)
          const hasNonQuickLink = formData.value.componentWeights.some(w => w.type !== 'quickLink')
          if (hasNonQuickLink && weightSum !== 100) {
              ElMessage.error(
                  `Component weights must add up to 100%. Current total: ${formData.value.componentWeights.reduce((s, w) => s + w.weight, 0)}%`
              )
              return
          }
      }
      ```
    - _Requirements: 5.1, 5.3, 5.4_

  - [ ] 6.5 `submitForm` payload 带入 `componentWeights`
    - 在构建 payload 时添加：
      `componentWeights: formData.value.components.length > 0 ? formData.value.componentWeights : []`
    - _Requirements: 6.3_

- [ ] 7. Phase 7：前端 API 类型更新

  - [ ] 7.1 `apis/ow/index.ts` Stage Update 请求体和 Stage 查询响应体新增 `componentWeights`
    - 在 `packages/flowFlex-common/src/app/apis/ow/index.ts` 中：
      - `updateStage(id, params)` 的 `params` 当前为 `any`，添加 JSDoc 说明 `params.componentWeights?: ComponentWeightItem[]`（或升级为具名类型）
      - `getStagesByWorkflow` 返回值的 Stage 对象包含 `componentWeights?: ComponentWeightItem[]`（JSDoc 注释说明）
    - _Requirements: 6.3, 6.5_

- [ ] 8. Phase 8：前端单元测试

  - [ ]* 8.1 `balanceWeights` 函数：Property 1 fast-check + EXAMPLE 用例
    - 创建测试文件 `packages/flowFlex-common/src/app/views/onboard/workflow/components/__tests__/balanceWeights.spec.ts`
    - **Property 1: Balance 均分后权重合计为 100**
    - **Validates: Requirements 2.2**
    - 使用 fast-check（项目已有依赖）对 N=1,2,3,5,7,9,11 个非 quickLink 实例进行 arbitrary 生成并验证 `balanceWeights` 后 sum=100
    - EXAMPLE：quickLink 实例权重保持 0；N=0 时不改变 Map

  - [ ]* 8.2 `applyFallbackWeights` 函数：Property 2 fast-check + EXAMPLE 用例
    - 在同一测试文件或新建 `applyFallbackWeights.spec.ts`：
    - **Property 2: FallbackWeight 初始化合计为 100**
    - **Validates: Requirements 1.6**
    - 对任意 N≥1 个非 quickLink 实例验证初始化后 sum=100
    - EXAMPLE：全为 quickLink 时全为 0

  - [ ]* 8.3 `StageComponentsSelector` 组件渲染测试
    - 创建 `StageComponentsSelector.spec.ts`：
      - 有 SelectedItems 时 WeightEditor 行渲染（EXAMPLE）
      - `weightSum=100` 时 TotalWeightIndicator 显示绿色状态（EXAMPLE）
      - `weightSum<100` / `weightSum>100` 时显示橙色警告（EXAMPLE）
      - PreviewPanel 行数等于 SelectedItems 数量（EXAMPLE）

  - [ ]* 8.4 `StageForm.submitForm` 权重校验测试
    - 创建 `StageForm.spec.ts`（或在现有文件追加）：
      - `weightSum≠100` 时 `ElMessage.error` 被调用，`updateStage` API 不被调用（EXAMPLE）
      - `weightSum=100` 时正常调用 `updateStage`（EXAMPLE）
    - _Requirements: 5.1, 5.3, 5.4_

  - [ ] 9. Final Checkpoint — 确保所有测试通过
    - 确保所有测试通过，询问用户是否有疑问。

## Notes

- 任务标记 `*` 为可选（测试类），可跳过以加速 MVP 交付
- 后端无需新建 Migration：`ff_stage.component_weights` 列已由 `20260819000001_AddGanttFieldsToStage` 建立
- `GanttService` 已有 `ParseComponentWeights` 逻辑和私有 `ComponentWeightEntry` 类；Task 2.3 是将其与新 `ComponentWeightItem` DTO 对齐，同时补充孤立记录过滤和 quickLink=0 快速返回
- `StageMapProfile` 中 `StageInputDto → Stage` 映射须 Ignore `ComponentWeights`，防止 AutoMapper 覆盖 null（权重持久化由 Service 手动处理）
- 前端 `fast-check` 是否已安装需在 Task 8.1 执行前确认；若未安装需先 `pnpm add -D fast-check`
- FsCheck.Xunit 需在 Task 3.1 执行前添加到 `FlowFlex.Tests.csproj`（当前 csproj 中无此依赖）

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["1.3", "1.4"] },
    { "id": 2, "tasks": ["1.5", "3.1"] },
    { "id": 3, "tasks": ["2.1", "2.3", "4.1"] },
    { "id": 4, "tasks": ["2.2"] },
    { "id": 5, "tasks": ["3.2", "3.3", "3.4", "5.1", "7.1"] },
    { "id": 6, "tasks": ["5.2", "5.3", "5.6"] },
    { "id": 7, "tasks": ["5.4", "5.5", "5.7", "5.8"] },
    { "id": 8, "tasks": ["5.9", "5.10", "5.11", "6.1"] },
    { "id": 9, "tasks": ["6.2", "6.3"] },
    { "id": 10, "tasks": ["6.4", "6.5"] },
    { "id": 11, "tasks": ["8.1", "8.2", "8.3", "8.4"] }
  ]
}
```
