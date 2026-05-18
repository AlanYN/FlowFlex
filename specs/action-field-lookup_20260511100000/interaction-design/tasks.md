# Tasks: Field Lookup — 交互设计任务

## 任务概览

| 分组     | 任务数 | 预估工时    |
| -------- | ------ | ----------- |
| 组件开发 | 3      | 0.75 天     |
| 集成改造 | 2      | 0.5 天      |
| **合计** | **5**  | **1.25 天** |

---

## 第一组：组件开发

### TASK-ID-001: 创建 LookupConfigPanel 组件

- **文件**：`packages/flowFlex-common/src/app/components/actionTools/LookupConfigPanel.vue`
- **内容**：
  - Props: `modelValue`（lookup 配置对象）、`integrationId`、`disabled`
  - Emits: `update:modelValue`、`test`
  - 2 列网格布局：endpoint / displayPath / valuePath / responsePath
  - 可折叠的 Custom Headers 区域（Key-Value 对，支持添加/删除多行）
  - Test 按钮（必填字段未填时 disabled）
  - 预览结果区域（el-table 或 el-alert）
  - 支持 dark mode
- **依赖**：无

### TASK-ID-002: 创建 Lookup 预览 API 调用函数

- **文件**：`packages/flowFlex-common/src/app/apis/field-lookup.ts`
- **内容**：
  - `previewLookupOptions(data: LookupPreviewRequest): Promise<LookupPreviewResponse>`
  - 类型定义：`LookupPreviewRequest`、`LookupPreviewResponse`、`OptionItem`
- **依赖**：无

### TASK-ID-003: 创建 Lookup 相关 TypeScript 类型

- **文件**：`packages/flowFlex-common/src/types/action-field-lookup.d.ts`
- **内容**：
  - `LookupConfig`: endpoint, displayPath, valuePath, responsePath, headers, integrationId
  - `LookupPreviewRequest`: integrationId, endpoint, displayPath, valuePath, responsePath, headers
  - `LookupPreviewResponse`: success, options, error, totalCount
  - `OptionItem`: display, value
  - `IFieldMappingItemWithLookup`: 扩展现有 IFieldMappingItem，增加 lookup? 和 lookupEnabled
- **依赖**：无

---

## 第二组：集成改造

### TASK-ID-004: 修改 ActionConfigDialog 的 Field Mapping 表格

- **文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`
- **内容**：
  - `IFieldMappingItem` 接口扩展：增加 `lookup?: LookupConfig` 和 `lookupEnabled?: boolean`
  - 表格新增 Lookup 列（el-switch）
  - 新增 expand 列，展开内容为 LookupConfigPanel
  - Lookup 开关切换时控制行展开/收起
  - 保存时将 lookupEnabled=true 的行的 lookup 配置序列化到 MappingConfig
  - 加载时从 MappingConfig 恢复 lookup 状态
- **依赖**：TASK-ID-001, TASK-ID-003

### TASK-ID-005: 保存/加载 MappingConfig 逻辑

- **文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`（同上文件）
- **内容**：
  - 保存时：将 fieldMappings（含 lookup）序列化为 MappingConfig JSON，调用 API 更新 ActionTriggerMapping
  - 加载时：从 ActionTriggerMapping.MappingConfig 中解析 fieldMappings，恢复 lookup 开关和配置值
  - 验证逻辑：lookupEnabled=true 时，endpoint/displayPath/valuePath 为必填
- **依赖**：TASK-ID-004
