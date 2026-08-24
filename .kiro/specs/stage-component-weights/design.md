# Design Document: Stage Component Weights

## Overview

为 FlowFlex WFE 系统的 Stage 编辑界面增加 Component 权重配置功能。

当前 Stage 完成度（CompletionPercentage）对所有 Component 均等对待，无法体现各 Component 的业务重要程度。甘特图功能（OW-705）需要基于权重来计算加权完成度。本功能在 Edit Stage → Components Tab 的 Selected Items 列表中为每个 ComponentInstance 新增 WeightEditor（Slider + 数字输入框），并提供 Balance 一键均分、TotalWeightIndicator 实时合计反馈，以及 StageCompletionPreview 预览面板。权重数据持久化到 `ff_stage.component_weights`（JSONB），后端 GanttService 的 `ComputeCompletionPercentage` 方法优先读取权重加权计算，无权重时退回平均分配。

**核心价值：** 允许 Workflow 管理员按业务重要度为每个 Component 分配权重占比，使甘特图中的 Stage 完成度更贴近真实进展。

---

## Architecture

功能涉及前后端联动，不引入新的服务层或数据库表，只在现有 Stage CRUD 流程上扩展。

```
┌─────────────────────────────────────────────────────────────────┐
│  StageComponentsSelector.vue                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  componentWeights: Map<string, number>  (reactive state) │   │
│  │  WeightEditor per SelectedItem                           │   │
│  │  Balance button / TotalWeightIndicator / PreviewPanel    │   │
│  └──────────────────────────────────────────────────────────┘   │
│          ↕ update:modelValue (ComponentsData + componentWeights)│
│  StageForm.vue                                                  │
│  - submitForm() validates weightSum === 100                     │
│  - passes componentWeights in API payload                       │
└──────────────────────┬──────────────────────────────────────────┘
                       │ PUT /ow/stages/v1/{id}
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│  StageController → StageService.UpdateAsync()                   │
│  - 后端二次校验 WeightSum == 100                                  │
│  - 序列化 ComponentWeights → JSON → stage.ComponentWeights       │
│  - SqlSugar UPDATE ff_stage SET component_weights = ...         │
└──────────────────────┬──────────────────────────────────────────┘
                       │ GET /ow/gantt/v1/{onboardingId}
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│  GanttService.ComputeCompletionPercentage()                     │
│  - ParseComponentWeights(stage.ComponentWeights, components)    │
│  - IF weights exist → 加权计算                                    │
│  - ELSE → 平均分配（FallbackWeight）                              │
└─────────────────────────────────────────────────────────────────┘
```

**关键设计决策：**
- 权重状态在前端以 `Map<string, number>` 维护，key 为 `${type}_${id}`，不耦合到 `StageComponentData.components` 数组
- `ComponentWeightsData` 通过 `ComponentsData.componentWeights` 随 `update:modelValue` 传出，保持现有事件总线兼容
- 后端新增 `ComponentWeightItem` DTO 类，与 GanttService 内部已有的 `ComponentWeightEntry` 结构对齐，后续应合并为统一类型
- 不新增 Migration：`ff_stage.component_weights` 列已由 OW-705 Migration 创建

---

## Components and Interfaces

### 后端新增/修改文件

#### 1. `Application.Contracts/Dtos/OW/Stage/ComponentWeightItem.cs`（新建）

```csharp
namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Represents the weight configuration for a single Component instance within a Stage.
    /// Serialized to/from ff_stage.component_weights (JSONB).
    /// </summary>
    public class ComponentWeightItem
    {
        /// <summary>Component type: "fields" | "checklist" | "questionnaire" | "files" | "quickLink"</summary>
        public string Type { get; set; }

        /// <summary>
        /// Instance identifier.
        /// - fields: fixed value "fields"
        /// - checklist / questionnaire / files / quickLink: string-ified snowflake long ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>Display name of the component instance.</summary>
        public string Name { get; set; }

        /// <summary>Weight value 0–100 (integer). Sum of all weights in a Stage must equal 100.</summary>
        public int Weight { get; set; }
    }
}
```

#### 2. `Application.Contracts/Dtos/OW/Stage/StageInputDto.cs`（修改）

在现有字段末尾添加：
```csharp
/// <summary>
/// Component weight configuration. Null = no change to existing weights.
/// When provided and non-empty, all weights must sum to 100.
/// </summary>
public List<ComponentWeightItem>? ComponentWeights { get; set; }
```

#### 3. `Application.Contracts/Dtos/OW/Stage/StageOutputDto.cs`（修改）

在现有字段末尾添加：
```csharp
/// <summary>
/// Component weight configuration deserialized from ff_stage.component_weights.
/// Null when no weights have been configured.
/// </summary>
public List<ComponentWeightItem>? ComponentWeights { get; set; }
```

#### 4. `Application/Services/OW/StageService.cs`（修改）

**UpdateAsync 方法**（在现有校验段之后、`UseTranAsync` 之前插入）：

```csharp
// Validate component weights if provided
if (input.ComponentWeights != null && input.ComponentWeights.Count > 0)
{
    var weightSum = input.ComponentWeights.Sum(w => w.Weight);
    if (weightSum != 100)
        throw new CRMException(ErrorCodeEnum.CustomError,
            $"Component weights must sum to 100. Current sum: {weightSum}");
}
```

**UpdateAsync 事务块内**（`_mapper.Map(input, stageInTransaction)` 之后）：

```csharp
// Persist component weights
if (input.ComponentWeights != null)
{
    stageInTransaction.ComponentWeights = input.ComponentWeights.Count > 0
        ? JsonSerializer.Serialize(input.ComponentWeights, _jsonOptions)
        : null;
}
// null input.ComponentWeights means "do not update" — existing value is preserved by mapper
```

**GetByIdAsync / GetListAsync**（AutoMapper Profile 中）：

```csharp
// In StageMapProfile.cs — Stage → StageOutputDto
.ForMember(dest => dest.ComponentWeights, opt => opt.MapFrom(src =>
    string.IsNullOrWhiteSpace(src.ComponentWeights)
        ? null
        : JsonSerializer.Deserialize<List<ComponentWeightItem>>(src.ComponentWeights,
              new JsonSerializerOptions { PropertyNameCaseInsensitive = true })));
```

#### 5. `Application/Services/OW/GanttService.cs`（修改）

`ParseComponentWeights` 方法改为直接使用 `ComponentWeightItem`（对齐 DTO 类型），保持现有逻辑不变（已支持权重加权计算和平均分配 fallback）。

孤立记录过滤（在 `ComputeCompletionPercentage` 开头）：
```csharp
// Filter orphan weight records: only keep entries whose id exists in current components
var validComponentKeys = new HashSet<string>(
    components.SelectMany(c => c.ChecklistIds?.Select(id => id.ToString())
                                ?? Enumerable.Empty<string>())
    .Concat(components.SelectMany(c => c.QuestionnaireIds?.Select(id => id.ToString())
                                       ?? Enumerable.Empty<string>()))
    .Concat(components.Any(c => c.Key == "fields") ? new[] { "fields" } : Array.Empty<string>())
    .Concat(components.Any(c => c.Key == "files") ? new[] { "files" } : Array.Empty<string>()),
    StringComparer.OrdinalIgnoreCase);
```

---

### 前端新增/修改文件

#### 1. `types/onboard.d.ts`（修改）

新增接口和扩展现有 `ComponentsData`：

```typescript
/** Weight configuration for a single Component instance in a Stage. */
export interface ComponentWeightItem {
    type: 'fields' | 'checklist' | 'questionnaire' | 'files' | 'quickLink';
    id: string;      // "fields" for Fields type; stringified snowflake ID for others
    name: string;
    weight: number;  // integer 0–100
}

// 扩展 ComponentsData
export interface ComponentsData {
    components: StageComponentData[];
    visibleInPortal?: boolean;
    portalPermission?: number;
    attachmentManagementNeeded?: boolean;
    componentWeights?: ComponentWeightItem[];  // 新增
}
```

#### 2. `views/onboard/workflow/components/StageComponentsSelector.vue`（主要改动）

**响应式状态：**
```typescript
// 权重状态：key = `${type}_${id}`
const componentWeights = ref<Map<string, number>>(new Map())
```

**Key 规则：**
```typescript
function getWeightKey(type: string, id: string): string {
    return `${type}_${id}`
}
// fields → "fields_fields"
// checklist id=3001 → "checklist_3001"
// questionnaire id=2001 → "questionnaire_2001"
// quickLink id=QL1 → "quickLink_QL1"
```

**初始化逻辑**（watch `props.modelValue.componentWeights`）：
```typescript
watch(() => props.modelValue.componentWeights, (saved) => {
    if (saved && saved.length > 0) {
        // 加载已保存的权重，过滤孤立记录
        const newMap = new Map<string, number>()
        saved.forEach(item => {
            if (isItemInSelectedList(item.type, item.id)) {
                newMap.set(getWeightKey(item.type, item.id), item.weight)
            }
        })
        componentWeights.value = newMap
    } else {
        // FallbackWeight 初始化
        applyFallbackWeights()
    }
}, { immediate: true })

function applyFallbackWeights() {
    const weightables = getWeightableItems()  // 排除 quickLink
    const newMap = new Map<string, number>()
    // quickLink 固定为 0
    getAllSelectedItems().filter(i => i.type === 'quickLink')
                         .forEach(i => newMap.set(getWeightKey(i.type, i.id), 0))
    if (weightables.length === 0) {
        componentWeights.value = newMap
        return
    }
    const base = Math.floor(100 / weightables.length)
    const remainder = 100 % weightables.length
    weightables.forEach((item, idx) => {
        newMap.set(getWeightKey(item.type, item.id), idx === 0 ? base + remainder : base)
    })
    componentWeights.value = newMap
}
```

**Balance 按钮：**
```typescript
function balanceWeights() {
    const weightables = getWeightableItems()
    if (weightables.length === 0) return
    const base = Math.floor(100 / weightables.length)
    const remainder = 100 % weightables.length
    const newMap = new Map(componentWeights.value)
    weightables.forEach((item, idx) => {
        newMap.set(getWeightKey(item.type, item.id), idx === 0 ? base + remainder : base)
    })
    componentWeights.value = newMap
    emitWeights()
}
```

**合计与预览：**
```typescript
const weightSum = computed(() =>
    getWeightableItems().reduce((sum, item) =>
        sum + (componentWeights.value.get(getWeightKey(item.type, item.id)) ?? 0), 0))

const isWeightSumValid = computed(() =>
    selectedItems.value.length === 0 || weightSum.value === 100)

const completionPreview = computed(() => {
    // 始终使用 100% 作为每个 ComponentInstance 的 ComponentCompletion 假设值
    return selectedItems.value.map(item => {
        const w = componentWeights.value.get(getWeightKey(item.type, item.id)) ?? 0
        return { name: item.name, weight: w, completion: 100, subtotal: w }
    })
})

const totalCompletion = computed(() =>
    Math.round(completionPreview.value.reduce((s, r) => s + r.subtotal, 0) * 10) / 10)
```

**向上传播（emitWeights）：**
```typescript
function emitWeights() {
    const weights: ComponentWeightItem[] = selectedItems.value.map(item => ({
        type: item.type as ComponentWeightItem['type'],
        id: item.type === 'fields' ? 'fields' : item.key,
        name: item.name,
        weight: componentWeights.value.get(getWeightKey(item.type, item.id)) ?? 0,
    }))
    emit('update:modelValue', { ...props.modelValue, componentWeights: weights })
}
```

**WeightEditor 模板（每个 SelectedItem 行内新增的子区域）：**
```html
<!-- 在每个 SelectedItem 卡片内、Portal Access 行之前 -->
<div class="border-t px-3 py-2 flex items-center gap-3">
    <el-icon class="text-gray-500 flex-shrink-0"><Scale /></el-icon>
    <span class="text-xs text-gray-500 flex-shrink-0">Weight</span>
    <el-slider
        :model-value="getWeight(element.type, getItemId(element))"
        :min="0" :max="100" :step="1"
        class="flex-1"
        size="small"
        @input="(v) => setWeight(element.type, getItemId(element), Number(v))"
    />
    <el-input-number
        :model-value="getWeight(element.type, getItemId(element))"
        :min="0" :max="100" :precision="0" :step="1"
        size="small"
        class="w-20 flex-shrink-0"
        @change="(v) => setWeight(element.type, getItemId(element), v ?? 0)"
    />
    <span class="text-xs text-gray-500 flex-shrink-0">%</span>
</div>
```

**Balance 按钮（SelectedItems 区域右上角）：**
```html
<div class="flex items-center justify-between mb-2">
    <label class="text-base font-bold">Selected Items</label>
    <el-button
        size="small"
        :disabled="selectedItems.length === 0"
        @click="balanceWeights"
    >
        <el-icon class="mr-1"><Scale /></el-icon> Balance
    </el-button>
</div>
```

**TotalWeightIndicator（SelectedItems 列表底部）：**
```html
<div class="mt-3 px-2 py-2 rounded-lg flex items-center gap-2 text-sm"
     :class="isWeightSumValid ? 'text-green-600' : 'text-orange-500'">
    <el-icon>
        <CircleCheck v-if="isWeightSumValid" />
        <Warning v-else />
    </el-icon>
    <template v-if="isWeightSumValid">
        <span>Total weight</span>
        <span class="ml-auto font-semibold">100%</span>
    </template>
    <template v-else>
        <span>
            Weights should add up to 100%. Currently
            {{ weightSum < 100
                ? `${100 - weightSum}% short — raise a component's weight to fill the gap.`
                : `${weightSum - 100}% over — lower a component's weight.` }}
        </span>
        <span class="ml-auto font-semibold">{{ weightSum }}%</span>
    </template>
</div>
```

**PreviewPanel（TotalWeightIndicator 下方）：**
```html
<div class="mt-2 border rounded-lg p-3 bg-blue-50 dark:bg-blue-900/10">
    <div class="flex items-center justify-between mb-2">
        <span class="text-sm font-medium text-blue-700">Stage completion preview</span>
        <span class="text-sm font-bold text-blue-700">{{ totalCompletion }}%</span>
    </div>
    <div v-for="row in completionPreview" :key="row.name" class="text-xs text-gray-600 py-0.5">
        {{ row.name }} · 100% done &nbsp;
        <span class="text-gray-400">{{ row.weight }}% × 100% = {{ row.weight }}.0%</span>
    </div>
    <el-progress :percentage="totalCompletion" :show-text="false" class="mt-2" />
</div>
```

#### 3. `views/onboard/workflow/components/StageForm.vue`（修改）

`updateComponentsData` 方法接收 `componentWeights` 并缓存在 `formData`：
```typescript
// 扩展 formData
const formData = ref({
    // ... 现有字段 ...
    componentWeights: [] as ComponentWeightItem[],
})

function updateComponentsData(val: ComponentsData) {
    formData.value.components = val.components
    formData.value.visibleInPortal = val.visibleInPortal ?? false
    if (val.portalPermission !== undefined) {
        formData.value.portalPermission = val.portalPermission
    }
    formData.value.attachmentManagementNeeded = val.attachmentManagementNeeded ?? false
    formData.value.componentWeights = val.componentWeights ?? []  // 新增
}
```

`submitForm` 中在现有权限校验之后、API 调用之前插入：
```typescript
// 权重校验（仅在有 Component 时）
if (formData.value.components.length > 0) {
    const allQuickLink = formData.value.componentWeights.every(w => w.type === 'quickLink')
    const weightSum = formData.value.componentWeights
        .filter(w => w.type !== 'quickLink')
        .reduce((s, w) => s + w.weight, 0)
    if (!allQuickLink && weightSum !== 100) {
        ElMessage.error(
            `Component weights must add up to 100%. Current total: ${weightSum + formData.value.componentWeights.filter(w => w.type === 'quickLink').reduce((s, w) => s + w.weight, 0)}%`
        )
        return
    }
}
```

`submitForm` payload 中新增 `componentWeights`：
```typescript
const payload = {
    ...formData.value,
    componentWeights: formData.value.components.length > 0
        ? formData.value.componentWeights
        : [],
} as any
```

初始化（`onMounted` 中加载 stage）时恢复 `componentWeights`：
```typescript
} else if (key === 'componentWeights') {
    formData.value[key] = (props.stage as any)?.componentWeights ?? []
}
```

#### 4. `apis/ow/index.ts`（修改）

Stage update 请求 body 和 Stage query 返回 body 中添加 `componentWeights` 字段，类型引用 `ComponentWeightItem`（从 `#/onboard` 导入）。

---

## Data Models

### 数据库层

`ff_stage.component_weights`（JSONB，已存在）：

```json
[
  { "type": "fields",        "id": "fields", "name": "Fields",      "weight": 20 },
  { "type": "questionnaire", "id": "2001",   "name": "Intake Form", "weight": 40 },
  { "type": "checklist",     "id": "3001",   "name": "Tasks",       "weight": 40 }
]
```

约束：
- 所有 `weight` 之和为 100（允许为 null 或空数组，代表未配置）
- `id` 对于 `fields` 类型固定为字符串 `"fields"`
- `weight` 为 0–100 整数

### 前端状态模型

```
StageComponentsSelector (componentWeights: Map<string, number>)
  key: `${type}_${id}`
  value: 0–100 integer

selectedItems[]: SelectedItem[]
  id: string           → 显示用（如 "checklist-3001"）
  key: string          → 实例 ID（如 "3001"，fields 类型为 "fields"）
  type: ComponentType
  name: string
```

### API DTO 映射

| 前端字段 | API 请求/响应 | 后端 DTO |
|---|---|---|
| `componentWeights[].type` | `type` | `ComponentWeightItem.Type` |
| `componentWeights[].id` | `id` | `ComponentWeightItem.Id` |
| `componentWeights[].name` | `name` | `ComponentWeightItem.Name` |
| `componentWeights[].weight` | `weight` | `ComponentWeightItem.Weight` |

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Prework Analysis

**Requirement 1 (WeightEditor UI)**

1.1 SelectedItems 渲染 WeightEditor
- Thoughts: 纯 UI 渲染。判断 Element 是否存在。
- Classification: EXAMPLE

1.2 Slider 与数字输入框双向同步
- Thoughts: UI 交互行为，不适合 PBT。
- Classification: EXAMPLE

1.3 输入超出范围自动修正
- Thoughts: 有 range clamp 纯函数。对任意整数输入，clamp(0,100) 后值在 [0,100]。
- Classification: PROPERTY

1.4–1.6 初始化逻辑
- Thoughts: 初始化有分支（已存 vs FallbackWeight）。FallbackWeight 对任意 N 个非 QuickLink 实例，floor(100/N) 分配 + 余数补到第一个，验证合计 = 100。
- Classification: PROPERTY (1.6 FallbackWeight 计算)

**Requirement 2 (Balance 按钮)**

2.1–2.4 Balance 逻辑
- Thoughts: Balance 是对任意 N 个非 QuickLink 实例的均分操作，结果合计 = 100。这是纯函数，100次迭代能发现 N=1,2,3... 等 edge case。
- Classification: PROPERTY

**Requirement 3 (TotalWeightIndicator)**

3.1–3.5 TotalWeightIndicator 状态
- Thoughts: 纯计算 `sum(weights)`，UI 展示。计算本身是 example-based。
- Classification: EXAMPLE

**Requirement 4 (PreviewPanel)**

4.1–4.6 PreviewPanel
- Thoughts: CompletionPercentage 计算 `Σ(weight/100 × 100)` = `Σ weight`（因为假设 completion=100%），等于 weightSum。计算本身平凡。UI 渲染为 EXAMPLE。
- Classification: EXAMPLE

**Requirement 5 (保存校验)**

5.1–5.4 前端保存校验
- Thoughts: 纯 guard clause，example-based 覆盖即可。
- Classification: EXAMPLE

**Requirement 6 (数据持久化)**

6.1–6.5 JSONB 序列化/反序列化
- Thoughts: 这是序列化 round-trip！对任意有效的 `ComponentWeightItem[]`（合计=100），序列化后反序列化应得到等价数组。适合 PBT。
- Classification: PROPERTY

**Requirement 7 (Component 列表联动)**

7.1–7.4 增减 Component 时权重维护
- Thoughts: 状态同步逻辑。孤立记录过滤：对任意 componentWeights 数组和 selectedItems 列表，过滤后结果仅包含两者 ID 的交集。适合 PBT。
- Classification: PROPERTY

**Requirement 8 (Fallback 计算)**

8.1–8.3 后端 ComputeCompletionPercentage fallback
- Thoughts: `ComputeCompletionPercentage` 是纯函数（给定 stage + progress，返回 decimal）。Fallback 路径对 N 个非 QuickLink Component 做平均分配。对任意 N≥1，fallback 结果应在 [0,100]。这是 PBT 的理想场景。
- Classification: PROPERTY

**Property Reflection（去冗余）：**
- Req 1.3（clamp）和 Req 1.6（FallbackWeight 合计）可以合并到 Balance 均分 Property 的输入约束中，因为 Balance 依赖 clamp 正确性
- Req 6（round-trip）和 Req 7（孤立过滤）逻辑独立，保留两个 Property
- Req 8（后端 fallback 范围约束）保留单独 Property

### Property 1: Balance 均分后权重合计为 100

*For any* 非空的 Component 实例列表（至少包含一个非 QuickLink 实例），执行 Balance 操作后，所有非 QuickLink 实例的权重之和 SHALL 等于 100，且 QuickLink 实例权重保持 0 不变。

**Validates: Requirements 2.2**

### Property 2: FallbackWeight 初始化合计为 100

*For any* 包含至少一个非 QuickLink ComponentInstance 的 Selected Items 列表，当 ComponentWeights 为 null 或空时，初始化后所有非 QuickLink 实例的权重之和 SHALL 等于 100，且 Quick Link 类型实例的权重 SHALL 为 0。

**Validates: Requirements 1.6**

### Property 3: ComponentWeights 序列化 round-trip

*For any* 有效的 `List<ComponentWeightItem>`（每条记录的 Type、Id、Name 均非空，Weight 在 0–100 范围内，且合计等于 100），将其序列化为 JSON 字符串后反序列化，所得列表 SHALL 与原列表在字段值上等价。

**Validates: Requirements 6.1, 6.5**

### Property 4: 孤立权重记录过滤

*For any* ComponentWeights 数组和当前 SelectedItems 列表，过滤操作后留存的权重记录 SHALL 仅包含在 SelectedItems 中存在对应实例的记录；所有孤立记录（SelectedItems 中不存在的）SHALL 被移除。

**Validates: Requirements 7.4**

### Property 5: ComputeCompletionPercentage 结果范围

*For any* Stage（含有 N≥0 个 Component）和对应的 OnboardingStageProgress，`ComputeCompletionPercentage` 的返回值 SHALL 在 [0.0, 100.0] 区间内（包含边界），且当 N=0 或所有 Component 均为 QuickLink 时返回值 SHALL 等于 100.0。

**Validates: Requirements 8.1, 8.2**

---

## Error Handling

### 前端错误处理

| 场景 | 处理方式 |
|---|---|
| 保存时 WeightSum ≠ 100（有 Component） | `ElMessage.error(...)` 阻断提交，不发送 API 请求 |
| 用户输入非整数（数字框） | `el-input-number` 内置 precision=0 自动拦截 |
| 用户输入超出 0–100 | `Math.min(100, Math.max(0, v))` 自动修正，不报错 |
| SelectedItems 为空时点 Balance | 按钮 disabled，不可触发 |
| 加载 Stage 时 ComponentWeights 为 null | 触发 FallbackWeight 初始化，无报错 |

### 后端错误处理

| 场景 | HTTP 状态 | 错误信息 |
|---|---|---|
| WeightSum ≠ 100（非 null 非空） | 400 (CustomError) | "Component weights must sum to 100. Current sum: X" |
| Weight 单值超出 0–100 | 400 (CustomError) | "Component weight value must be between 0 and 100" |
| ComponentWeights 反序列化失败 | 忽略（fallback 到 null） | 日志 Warning，不抛出 |
| Stage 不存在 | 404 (DataNotFound) | 现有逻辑（不变） |

### 数据一致性

- `ComponentWeights` 为 null 时，`ComputeCompletionPercentage` 自动退回平均分配，旧数据无需迁移
- `ComponentWeights` 不为 null 但包含孤立记录时，`ComputeCompletionPercentage` 忽略孤立项，仅计算与当前 `Components` 匹配的部分

---

## Testing Strategy

### 单元测试（后端 xUnit）

**`ComponentWeightItemTests`：**
- 序列化 round-trip（对应 Property 3）：用 FsCheck/property-based test 覆盖任意有效权重列表
- 边界值：Weight=0、Weight=100、合计恰好=100

**`StageService_UpdateAsync_Tests`：**
- 权重合计≠100 时抛出 CRMException（EXAMPLE）
- 权重合计=100 时保存成功并序列化到 ComponentWeights 列（EXAMPLE）
- input.ComponentWeights=null 时不更新已有权重值（EXAMPLE）
- 空列表时存储 null（EXAMPLE）

**`GanttService_ComputeCompletionPercentage_Tests`：**
- Property 5（结果范围）：用 FsCheck 生成任意 Stage + Progress 组合
- N=0 Component → 返回 100（EXAMPLE）
- 全 QuickLink Component → 返回 100（EXAMPLE）
- 有配置权重，已知 ComponentCompletion 值 → 期望加权结果（EXAMPLE）
- componentWeights=null → 使用平均分配（EXAMPLE）
- 孤立 Weight 记录被忽略（Property 4，后端视角）

### 单元测试（前端 Jest）

**`balanceWeights` 函数：**
- Property 1（Balance 合计=100）：对 N=1,2,3,5,7,9,11 组 Component，均分后 sum=100（覆盖余数场景）
- QuickLink 权重保持 0（EXAMPLE）
- N=0 时不改变 Map（EXAMPLE）

**`applyFallbackWeights` 函数：**
- Property 2（FallbackWeight 合计=100）：对不同 N 组合验证
- 全为 QuickLink 时 Map 全为 0（EXAMPLE）

**`StageComponentsSelector` 组件：**
- WeightEditor 在 SelectedItems 中渲染（EXAMPLE）
- TotalWeightIndicator 绿色状态（sum=100）（EXAMPLE）
- TotalWeightIndicator 橙色状态（sum<100 / sum>100）（EXAMPLE）
- PreviewPanel 数据行数等于 SelectedItems 数量（EXAMPLE）

**`StageForm.submitForm` 权重校验：**
- WeightSum≠100 时 ElMessage.error 被调用，API 不调用（EXAMPLE）
- WeightSum=100 时正常提交（EXAMPLE）

### Property-Based Testing 配置

后端使用 **FsCheck**（与 xUnit 集成）：
```csharp
// 每个 property test 至少运行 100 次迭代
[Property(MaxTest = 100)]
public Property ComputeCompletionPercentage_AlwaysInRange(...)
```

前端使用 **fast-check**：
```typescript
// jest.config.ts 中引入 fast-check
// Tag format: Feature: stage-component-weights, Property {N}: {property_text}
```

**Property test tag 格式：**
- `Feature: stage-component-weights, Property 1: Balance 均分后权重合计为 100`
- `Feature: stage-component-weights, Property 2: FallbackWeight 初始化合计为 100`
- `Feature: stage-component-weights, Property 3: ComponentWeights 序列化 round-trip`
- `Feature: stage-component-weights, Property 4: 孤立权重记录过滤`
- `Feature: stage-component-weights, Property 5: ComputeCompletionPercentage 结果范围`
