# Requirements Document

## Introduction

为 FlowFlex WFE 系统的 Stage 编辑界面增加 Component 权重配置功能。

每个 Workflow Stage 可包含多个 Component（Fields、Questionnaire、Checklist、Files、Quick Link）。当前系统对 Stage 完成度的计算没有加权支持，无法区分各 Component 的重要程度。甘特图功能（OW-705）要求基于权重来计算 Stage Completion %。

本功能在 Edit Stage → Components Tab → Selected Items 列表中为每个 Component 实例新增权重滑块配置，并提供实时完成度预览和合计校验，让用户能直观地为每个 Component 分配权重占比。权重数据写入 `ff_stage.component_weights`（JSONB 列），供甘特图及后续计算复用。

---

## Glossary

- **Stage**：Workflow 中的一个步骤节点，对应 `ff_stage` 表中的记录
- **Component**：附加在 Stage 上的可配置项，类型包括 Fields、Questionnaire、Checklist、Files、Quick Link
- **ComponentInstance**：某个 Stage 内已选中的一个具体 Component 条目（同类型可有多个实例，如两个不同问卷）
- **ComponentWeight**：一个 ComponentInstance 在 Stage 完成度计算中的权重值，整数，范围 0–100，代表百分比
- **ComponentWeights**：Stage 上所有 ComponentInstance 的权重配置数组，存储于 `ff_stage.component_weights`（JSONB）
- **WeightSum**：当前 Stage 所有 ComponentInstance 的 `ComponentWeight` 之和
- **WeightBalance**：Balance 操作，将 100 均分到所有 ComponentInstance，整除余数补到第一个实例
- **CompletionPercentage**：Stage 完成度百分比（0–100），按 ComponentWeights 加权计算：`Σ (ComponentWeight% × ComponentCompletion%)`
- **ComponentCompletion**：单个 ComponentInstance 的完成度（0–100），各类型有独立计算规则
- **StageCompletionPreview**：在 Stage 编辑 UI 内实时计算并展示的 CompletionPercentage 预览，不持久化
- **SelectedItems**：StageComponentsSelector 组件右侧已选 Component 列表
- **WeightEditor**：每个 ComponentInstance 条目下方的权重配置行，包含 Slider 和数字输入框
- **TotalWeightIndicator**：SelectedItems 底部的权重合计状态行
- **PreviewPanel**：SelectedItems 底部的 StageCompletionPreview 展示区域
- **FallbackWeight**：当 ComponentWeights 为 null 或空时，所有 ComponentInstance 平均分配的默认权重值

---

## Requirements

### Requirement 1：WeightEditor UI 渲染

**User Story：** 作为 Workflow 管理员，我想在 Stage 的 Selected Items 列表中为每个 Component 配置权重，以便控制各 Component 对 Stage 完成度的影响比重。

#### Acceptance Criteria

1. WHEN 用户进入 Edit Stage → Components Tab，THE StageComponentsSelector SHALL 在 SelectedItems 列表中每个 ComponentInstance 条目内渲染一个 WeightEditor 行
2. THE WeightEditor SHALL 包含以下元素：权重图标（⚖）、"Weight" 标签、范围 0–100 的 Slider、整数数字输入框、"%" 符号
3. THE WeightEditor SHALL 将 Slider 与数字输入框保持双向同步：拖动 Slider 时数字输入框实时更新，修改数字输入框时 Slider 实时更新
4. THE WeightEditor SHALL 限制数字输入框只接受 0–100 的整数输入，输入超出范围的值时 THE WeightEditor SHALL 自动修正为边界值
5. WHEN ComponentWeights 数据已存在（编辑已配置过权重的 Stage），THE WeightEditor SHALL 初始化显示对应的已保存权重值
6. WHEN ComponentWeights 为 null 或空（新 Stage 或旧 Stage 无权重记录），THE WeightEditor SHALL 初始化显示各实例权重：Quick Link 类型的 ComponentInstance 固定初始化为 0，不参与均分；非 Quick Link 类型使用 FallbackWeight（= floor(100 / nonQuickLinkCount)，余数补到第一个非 Quick Link 实例）

---

### Requirement 2：Balance 按钮

**User Story：** 作为 Workflow 管理员，我想一键将权重均分到所有 Component，以便快速完成初始配置。

#### Acceptance Criteria

1. THE StageComponentsSelector SHALL 在 SelectedItems 区域右上角渲染一个 "Balance" 按钮
2. WHEN 用户点击 Balance 按钮，THE StageComponentsSelector SHALL 将 100 均分到所有非 Quick Link 类型的 ComponentInstance：每个非 Quick Link 实例权重 = floor(100 / nonQuickLinkCount)，整除余数（100 mod nonQuickLinkCount）补加到第一个非 Quick Link 实例；Quick Link 类型的 ComponentInstance 权重保持为 0 不变
3. WHEN SelectedItems 列表为空，THE Balance 按钮 SHALL 保持禁用状态
4. WHEN 用户点击 Balance 按钮后增减 Component，THE WeightEditor SHALL 保持各实例已有权重值不变（Balance 操作不自动重触发）

---

### Requirement 3：TotalWeightIndicator 实时反馈

**User Story：** 作为 Workflow 管理员，我想实时看到当前权重合计是否等于 100%，以便在保存前发现并修正错误。

#### Acceptance Criteria

1. THE StageComponentsSelector SHALL 在 SelectedItems 列表底部固定显示 TotalWeightIndicator
2. WHEN WeightSum 等于 100，THE TotalWeightIndicator SHALL 显示绿色勾图标和 "100%" 文字，表示合法状态
3. WHEN WeightSum 不等于 100，THE TotalWeightIndicator SHALL 显示橙色警告图标、当前 WeightSum 值，以及提示文字："Weights should add up to 100%. Currently [diff]% short — raise a component's weight to fill the gap."（diff = 100 - WeightSum；WeightSum > 100 时提示文字改为 "Currently [excess]% over — lower a component's weight."）
4. THE TotalWeightIndicator SHALL 在每次 WeightEditor 值变更后同步刷新，无需用户手动触发
5. WHEN SelectedItems 为空，THE TotalWeightIndicator SHALL 显示为合法状态（WeightSum 视为 100）

---

### Requirement 4：StageCompletionPreview 实时预览

**User Story：** 作为 Workflow 管理员，我想在配置权重时预览 Stage 完成度计算结果，以便直观验证权重配置的合理性。

#### Acceptance Criteria

1. THE StageComponentsSelector SHALL 在 TotalWeightIndicator 下方渲染 PreviewPanel，标题为 "Stage completion preview"
2. THE PreviewPanel SHALL 为每个 ComponentInstance 渲染一行，格式为：`[Component 名称] · [ComponentCompletion]% done  [Weight]% × [ComponentCompletion]% = [subtotal]%`
3. THE PreviewPanel SHALL 在底部显示蓝色加粗的 CompletionPercentage 合计值，格式为 "Stage completion preview XX.X%"，以及一条进度条反映该值
4. THE PreviewPanel SHALL 在每次 WeightEditor 值变更后同步刷新，无需用户手动触发
5. THE PreviewPanel SHALL 始终对每个 ComponentInstance 使用 100% 作为 ComponentCompletion 的假设值（PreviewPanel 为预览行为，不读取真实 Case 数据）
6. THE CompletionPercentage 计算公式 SHALL 为：`Σ (ComponentWeight / 100 × ComponentCompletion)`，结果四舍五入到一位小数

---

### Requirement 5：保存时权重校验

**User Story：** 作为系统，我需要在用户保存 Stage 时强制校验权重合计，防止无效数据写入数据库。

#### Acceptance Criteria

1. WHEN 用户点击 Stage 的保存按钮且 WeightSum 不等于 100，THE System SHALL 阻止保存并显示错误提示："Component weights must add up to 100%. Current total: [WeightSum]%"
2. WHEN 用户点击 Stage 的保存按钮且 SelectedItems 列表为空，THE System SHALL 允许保存并将 ComponentWeights 存储为空数组
3. WHEN 用户点击 Stage 的保存按钮且 WeightSum 等于 100，THE System SHALL 继续执行原有保存流程，不阻断
4. THE System SHALL 在前端完成权重校验，不依赖后端返回错误来阻断保存操作

---

### Requirement 6：ComponentWeights 数据持久化

**User Story：** 作为系统，我需要将用户配置的权重数据保存到数据库，并在下次编辑时正确还原。

#### Acceptance Criteria

1. WHEN Stage 保存成功，THE System SHALL 将 ComponentWeights 序列化为 JSONB 数组写入 `ff_stage.component_weights` 列，每条记录格式为 `{ "type": string, "id": string, "name": string, "weight": integer }`
2. THE System SHALL 对 Fields 类型使用固定值 `"id": "fields"` 作为标识符，不按 field 实例拆分
3. THE System SHALL 在 Stage Update API 的请求 DTO 中新增 `ComponentWeights` 字段，类型为 `List<ComponentWeightItem>`，允许为 null（null 表示无权重配置，后端不更新该列）
4. WHEN 后端接收到 `ComponentWeights` 不为 null 且 WeightSum 不等于 100，THE System SHALL 返回 400 错误，错误信息为 "Component weights must sum to 100"
5. WHEN Stage 被加载用于编辑，THE System SHALL 将 `ff_stage.component_weights` 中的数组反序列化并映射到前端 WeightEditor 的初始值

---

### Requirement 7：与现有 Component 列表的联动

**User Story：** 作为系统，我需要在用户增减 Component 时正确维护权重配置，防止权重数据与当前 Component 列表不一致。

#### Acceptance Criteria

1. WHEN 用户向 SelectedItems 添加一个新 ComponentInstance，THE StageComponentsSelector SHALL 为该实例初始化权重为 0，并触发 TotalWeightIndicator 刷新
2. WHEN 用户从 SelectedItems 删除一个 ComponentInstance，THE StageComponentsSelector SHALL 移除该实例对应的权重记录，并触发 TotalWeightIndicator 刷新
3. WHEN 用户通过拖拽改变 SelectedItems 中 ComponentInstance 的顺序，THE StageComponentsSelector SHALL 保持各实例的权重值不变，仅更新顺序
4. IF ComponentWeights 数据中存在 SelectedItems 里不再存在的实例记录（数据不一致），THEN THE System SHALL 在加载时过滤掉这些孤立记录，仅保留与当前 SelectedItems 匹配的权重数据

---

### Requirement 8：无权重配置时的 Fallback 计算

**User Story：** 作为系统，我需要在 Stage 没有配置权重时提供合理的默认完成度计算，确保旧数据的向后兼容。

#### Acceptance Criteria

1. WHEN `ff_stage.component_weights` 为 null 或空数组，THE System SHALL 在完成度计算时对所有 ComponentInstance 使用平均权重（FallbackWeight = 100 / componentCount，浮点均分）
2. WHEN Stage 有 0 个 ComponentInstance，THE System SHALL 将该 Stage 的 CompletionPercentage 视为 100%
3. THE FallbackWeight 逻辑 SHALL 仅在计算时生效，不写入数据库，`ff_stage.component_weights` 列保持 null
