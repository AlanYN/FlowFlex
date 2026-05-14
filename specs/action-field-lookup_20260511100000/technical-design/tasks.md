# Tasks: Action Field Lookup — 技术方案开发任务

## 任务概览

| 分组             | 任务数 | 预估工时    | 说明                                       |
| ---------------- | ------ | ----------- | ------------------------------------------ |
| 第一组：环境搭建 | 1      | 0.25 天     | DTO 和接口定义                             |
| 第二组：数据层   | 2      | 0.5 天      | IntegrationHttpClient + FieldLookupService |
| 第三组：API 层   | 2      | 0.5 天      | Controller endpoints + 执行链路集成        |
| 第四组：UI 层    | 3      | 1.5 天      | 前端组件 + 类型 + API 调用                 |
| **合计**         | **8**  | **2.75 天** |

---

## 第一组：环境搭建

### TASK-TD-001: 创建 DTO 和接口定义

**文件**：

- `packages/flowFlex-backend/Application.Contracts/Dtos/Action/FieldLookupDto.cs`
- `packages/flowFlex-backend/Application.Contracts/IServices/Integration/IIntegrationHttpClient.cs`
- `packages/flowFlex-backend/Application.Contracts/IServices/Action/IFieldLookupService.cs`

**操作步骤**：

1. 创建 `FieldLookupDto.cs`，包含：
   - `MappingConfigModel`（fieldMappings + lookupConfig）
   - `FieldMappingItem`（wfeField + apiField + lookup?）
   - `LookupConfig`（endpoint + displayPath + valuePath + responsePath + headers + integrationId）
   - `LookupGlobalConfig`（timeoutSeconds + maxOptionsPerField）
   - `OptionItem`（display + value）
   - `FieldLookupResult`（apiField + status + options + totalCount + error）
   - `LookupPreviewRequest`（integrationId + endpoint + displayPath + valuePath + responsePath + headers）
   - `LookupPreviewResponse`（success + options + totalCount + error）
   - `IntegrationHttpResponse`（isSuccess + statusCode + body + error + durationMs）
2. 创建 `IIntegrationHttpClient.cs` 接口（GetAsync + PostAsync）
3. 创建 `IFieldLookupService.cs` 接口（FetchLookupOptionsAsync + PreviewLookupAsync）

**依赖**：无

---

## 第二组：数据层

### TASK-TD-002: 实现 IntegrationHttpClient

**文件**：`packages/flowFlex-backend/Application/Services/Integration/IntegrationHttpClient.cs`

**操作步骤**：

1. 创建 `IntegrationHttpClient` 类，实现 `IIntegrationHttpClient, IScopedService`
2. 构造函数注入：`IIntegrationRepository`, `IEncryptionService`, `IIntegrationApiLogService`, `IHttpClientFactory`, `ILogger<IntegrationHttpClient>`
3. 实现 `GetAsync`：
   - 查询 Integration 实体（by Id）
   - 调用 `DecryptCredentials`（复用 IntegrationService 中的逻辑）
   - 根据 AuthMethod 设置认证 Headers
   - 合并 additionalHeaders（后合并的覆盖先设置的）
   - 拼接 URL：`integration.EndpointUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/')`
   - 设置超时：`CancellationTokenSource.CreateLinkedTokenSource` + `TimeSpan.FromSeconds(timeout)`
   - 发起请求，记录 startedAt/completedAt/durationMs
   - 调用 `_logService.LogApiCallAsync()` 记录日志
   - 返回 `IntegrationHttpResponse`
4. 实现 `PostAsync`（类似 GetAsync，body 序列化为 JSON）
5. 私有方法 `DecryptCredentials`：`_encryptionService.Decrypt(encrypted)` → `JsonConvert.Deserialize<Dictionary<string, string>>`
6. 私有方法 `ApplyAuthentication`：根据 AuthMethod 设置 request headers

**依赖**：TASK-TD-001

### TASK-TD-003: 实现 FieldLookupService

**文件**：`packages/flowFlex-backend/Application/Services/Action/FieldLookupService.cs`

**操作步骤**：

1. 创建 `FieldLookupService` 类，实现 `IFieldLookupService, IScopedService`
2. 构造函数注入：`IIntegrationHttpClient`, `ILogger<FieldLookupService>`
3. 实现 `FetchLookupOptionsAsync`：
   - 解析 `mappingConfig.ToObject<MappingConfigModel>()`
   - 筛选有 lookup 的字段
   - 读取全局配置（timeout, maxOptions）
   - `Task.WhenAll` 并行调用 `FetchSingleFieldOptionsAsync`
4. 实现 `FetchSingleFieldOptionsAsync`（私有）：
   - 确定 integrationId（lookup.IntegrationId ?? defaultIntegrationId）
   - 调用 `_integrationHttpClient.GetAsync(integrationId, endpoint, headers, timeout)`
   - 解析响应 JSON：`JToken.Parse(body)`
   - 提取数组：`SelectToken(responsePath)` 或根级
   - 遍历数组，`SelectToken(displayPath/valuePath)` 提取 display/value
   - 截断到 maxOptions
   - try-catch 包裹，失败返回 `FieldLookupResult.Failed`
5. 实现 `PreviewLookupAsync`：
   - 构造临时 `FieldMappingItem` + `LookupConfig`
   - 调用 `FetchSingleFieldOptionsAsync`
   - 返回结果（options 截断到 10 条用于预览）

**依赖**：TASK-TD-002

---

## 第三组：API 层

### TASK-TD-004: 新增 Controller Endpoints

**文件**：`packages/flowFlex-backend/WebApi/Controllers/Action/ActionController.cs`（修改）

**操作步骤**：

1. 在 ActionController 构造函数中注入 `IFieldLookupService`
2. 新增 `POST lookup/preview` endpoint：
   - 接收 `[FromBody] LookupPreviewRequest`
   - 调用 `_fieldLookupService.PreviewLookupAsync`
   - 返回 `LookupPreviewResponse`（options 截断到 10 条）
3. 新增 `PUT trigger-mappings/{id}/mapping-config` endpoint：
   - 接收 `[FromBody] JToken mappingConfig`
   - 调用 `_actionManagementService.UpdateMappingConfigAsync(id, mappingConfig)`
   - 返回 bool
4. 在 `IActionManagementService` 中新增 `UpdateMappingConfigAsync` 方法签名
5. 在 `ActionManagementService` 中实现：查询 TriggerMapping → 更新 MappingConfig → 保存

**依赖**：TASK-TD-003

### TASK-TD-005: 集成到 Action 执行链路

**文件**：`packages/flowFlex-backend/Application/Services/Action/ActionExecutionService.cs`（修改）

**操作步骤**：

1. 在 ActionExecutionService 构造函数中注入 `IFieldLookupService`, `IActionTriggerMappingRepository`
2. 在 `ExecuteActionAsync` 方法中，executor 调用前插入：
   - 检查 `triggerMappingId` 是否有值
   - 查询 `ActionTriggerMapping`
   - 检查 `TriggerType == "Integration"` → 获取 IntegrationId
   - 调用 `_fieldLookupService.FetchLookupOptionsAsync(integrationId, mappingConfig)`
   - 将结果存入 `execution.ExecutionInput`（作为 lookupResults 字段）
3. 确保无 lookup 时不影响现有逻辑（null check + early return）

**依赖**：TASK-TD-003

---

## 第四组：UI 层

### TASK-TD-006: 创建前端类型定义和 API 函数

**文件**：

- `packages/flowFlex-common/src/types/action-field-lookup.d.ts`
- `packages/flowFlex-common/src/app/apis/action-field-lookup.ts`

**操作步骤**：

1. 创建类型定义文件：
   - `LookupConfig`、`FieldMappingItemWithLookup`、`LookupPreviewRequest`、`LookupPreviewResponse`、`OptionItem`
2. 创建 API 函数文件：
   - `previewLookupOptions(data)` → POST `/action/v1/lookup/preview`
   - `updateMappingConfig(mappingId, config)` → PUT `/action/v1/trigger-mappings/{id}/mapping-config`

**依赖**：无

### TASK-TD-007: 创建 LookupConfigPanel 组件

**文件**：`packages/flowFlex-common/src/app/components/actionTools/LookupConfigPanel.vue`

**操作步骤**：

1. 定义 Props：`modelValue: LookupConfig`、`integrationId: string`、`disabled: boolean`
2. 定义 Emits：`update:modelValue`
3. 实现 2 列网格布局：
   - Row 1: Options Source (endpoint) | Display Field (displayPath)
   - Row 2: Value Field (valuePath) | Response Path (responsePath)
4. 实现可折叠 Custom Headers 区域：
   - 标题行："▸ Custom Headers (optional)"，点击展开/收起
   - 展开后：Key-Value 输入行列表 + "Add Header" 按钮 + 每行删除按钮
5. 实现 Test 按钮：
   - 必填字段（endpoint/displayPath/valuePath）未填时 disabled
   - 点击调用 `previewLookupOptions` API
   - 成功：显示 el-table（Display | Value），上方提示 "Showing X of Y options"
   - 失败：显示 el-alert type="error"
6. 支持 dark mode（TailwindCSS dark: 前缀）
7. 面板样式：`bg-gray-50 dark:bg-gray-800 rounded-lg p-4 border-l-4 border-primary`

**依赖**：TASK-TD-006

### TASK-TD-008: 修改 ActionConfigDialog Field Mapping 区域

**文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`（修改）

**操作步骤**：

1. 扩展 `IFieldMappingItem` 接口：增加 `lookupEnabled?: boolean` 和 `lookup?: LookupConfig`
2. 在 el-table 中新增 Lookup 列：
   - 使用 `el-switch` size="small"
   - v-model 绑定 `row.lookupEnabled`
3. 新增 expand 列（type="expand"）：
   - 展开内容为 `<LookupConfigPanel>`
   - 通过 `expand-row-keys` 控制展开状态
   - Lookup 开关切换时自动展开/收起
4. 保存逻辑修改：
   - 构建 MappingConfig JSON（fieldMappings 数组，含 lookup）
   - 如果有 triggerMappingId，调用 `updateMappingConfig` API
5. 加载逻辑修改：
   - 从 ActionTriggerMapping 的 MappingConfig 中解析 fieldMappings
   - 恢复 lookupEnabled 状态和 lookup 配置值
6. 验证逻辑：lookupEnabled=true 时，endpoint/displayPath/valuePath 必填

**依赖**：TASK-TD-007

---

## 补足：Outbound Field Lookup 位置修正

> **背景**：原设计将 Lookup 配置放在了现有的 Field Mapping 表格（返回值映射）中，但 Lookup 实际是给 Action **入参**（Outbound）用的。现有 Field Mapping（InboundFieldMapping）是映射 Action 返回值 → WFE 字段的，方向相反。需要将 Lookup 配置移到正确的位置。

### 修正方案

- Lookup 配置从现有 Field Mapping 表格中移除
- 新增独立的"Outbound Field Lookup"区域，放在 HTTP Config 下方、现有 Field Mapping 上方
- MappingConfig 中的 `fieldMappings` 结构不变，但语义明确为"出站参数转换映射"
- 执行链路中，Lookup 结果用来替换 Action 请求参数中的值（Params 或 Body JSON 中的对应字段）

### UI 结构调整

```
Action 编辑弹窗：
├── Basic Info (Name / Condition / Description / Type)
├── HTTP Configuration (URL / Params / Headers / Body)
├── ▼ Outbound Field Lookup (新增独立区域) ← Lookup 配置放这里
│   ┌────────────────┬──────────────┬────────┐
│   │ Source Field   │ Target Param │ Lookup │
│   │ (WFE 占位符)   │ (API 参数名)  │        │
│   ├────────────────┼──────────────┼────────┤
│   │ {{salesRep}}   │ sales_rep_id │ ☑ [配置]│
│   │ {{location}}   │ facility_code│ ☑ [配置]│
│   └────────────────┴──────────────┴────────┘
├── ▼ Field Mapping (现有，返回值映射，不动)
└── Footer (Cancel / Save)
```

---

## 第五组：补足任务

### TASK-TD-009: 前端 — 将 Lookup 配置从 Field Mapping 移到独立区域

**文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`（修改）

**操作步骤**：

1. 从现有 Field Mapping 表格中移除 Lookup 列和 expand 逻辑
2. 在 HTTP Config 区域下方、Field Mapping 区域上方，新增"Outbound Field Lookup"折叠区域
3. 该区域包含一个表格：
   - Source Field 列：显示 WFE 占位符名（如 `{{salesRep}}`），用户手动输入或从 Params/Body 中自动提取
   - Target Param 列：目标 API 参数名（如 `sales_rep_id`），用户手动输入
   - Lookup 列：el-switch + 展开 LookupConfigPanel
4. 支持添加/删除行
5. 保存时写入 MappingConfig.fieldMappings
6. 加载时从 MappingConfig 回显

**依赖**：无（基于已有代码修改）

### TASK-TD-010: 后端 — 执行链路中 Lookup 结果替换请求参数

**文件**：`packages/flowFlex-backend/Application/Services/Action/ActionExecutionService.cs`（修改）

**操作步骤**：

1. 在 Lookup 获取选项列表之后、Executor 执行之前：
   - 遍历 Lookup 结果
   - 对每个成功的 Lookup 字段，从 contextData 中取出 Source Field 的原始值（占位符替换后的值）
   - 用原始值在选项列表中匹配（精确匹配 display 字段）
   - 如果匹配到，将 ActionConfig 中对应 Target Param 的值替换为匹配到的 value
2. 替换逻辑需要覆盖两种情况：
   - **Params 类型**：直接替换 `config.Params[targetParam]` 的值
   - **JSON Body 类型**：解析 Body JSON → 找到 targetParam 对应的字段 → 替换值 → 重新序列化
3. 如果 Lookup 失败或未匹配到，保留原始值不替换（降级为直接传原文本）

**依赖**：TASK-TD-009

### TASK-TD-011: 后端 — 支持 Lookup endpoint 中的占位符替换

**文件**：`packages/flowFlex-backend/Application/Services/Action/FieldLookupService.cs`（修改）

**操作步骤**：

1. 在 `FetchSingleFieldOptionsAsync` 中，调用 IntegrationHttpClient 之前：
   - 对 `lookup.endpoint` 执行占位符替换（使用 contextData）
   - 例如：`https://api.crm.com/users?keyword={{salesRep}}` → `https://api.crm.com/users?keyword=张三`
2. 复用 `HttpApiActionExecutor` 中已有的 `ReplacePlaceholders` 逻辑（提取为公共 Helper 或直接实现）
3. 注意：Preview（Test 按钮）时没有 contextData，占位符保持原样或提示用户

**依赖**：无

### TASK-TD-012: 前端 — 自动从 Params/Body 提取可配置字段（可选优化）

**文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`（修改）

**操作步骤**：

1. 当用户在 Params 或 Body 中使用了 `{{xxx}}` 占位符时，自动在 Outbound Field Lookup 区域提示可配置的字段
2. 实现方式：监听 Params/Body 变化，用正则 `\{\{(\w+)\}\}` 提取所有占位符
3. 在 Outbound Field Lookup 区域上方显示提示："Detected fields: {{salesRep}}, {{location}} — Add lookup?"
4. 点击后自动添加对应行到表格中
5. 此任务为可选优化，不阻塞核心功能

**依赖**：TASK-TD-009

---

## 第六组：Lookup 存储位置迁移（从 TriggerMapping 迁移到 ActionConfig）

> **背景**：经讨论确认，Lookup 配置应属于 Action 本身，而不是 TriggerMapping。同一个 Action 不管从哪里触发，Lookup 配置都是一样的。因此将 Lookup 数据从 `ActionTriggerMapping.MappingConfig` 迁移到 `ActionDefinition.ActionConfig` 中。

### 迁移方案

**存储位置变更**：

| 项目     | 迁移前                                      | 迁移后                                |
| -------- | ------------------------------------------- | ------------------------------------- |
| 存储表   | `ff_action_trigger_mappings.mapping_config` | `ff_action_definitions.action_config` |
| 字段路径 | `mappingConfig.fieldMappings`               | `actionConfig.lookupMappings`         |
| 归属关系 | 属于 TriggerMapping（每次触发可能不同）     | 属于 Action（全局唯一）               |

**ActionConfig 新结构**：

```json
{
  "url": "https://crm-dev.item.pub/crm/customers/v2/company-customers",
  "method": "POST",
  "headers": { ... },
  "body": "...",
  "params": {},
  "timeout": 30,
  "lookupMappings": [
    {
      "wfeField": "{{contactName}}",
      "apiField": "contactName",
      "lookup": {
        "endpoint": "https://crm-dev.item.pub/crm/dropdown-resources/v2/programs",
        "displayPath": "name",
        "valuePath": "id",
        "responsePath": "data",
        "headers": { "x-tenant-id": "FUZE" },
        "integrationId": null
      }
    }
  ]
}
```

---

### TASK-TD-013: 前端 — 保存/加载 Lookup 改为读写 ActionConfig

**文件**：`packages/flowFlex-common/src/app/components/actionTools/ActionConfigDialog.vue`（修改）

**操作步骤**：

1. **保存逻辑修改**：
   - 删除保存时调用 `updateMappingConfig(triggerMappingId, ...)` 的逻辑
   - 改为将 `outboundLookups` 数据写入 `formData.actionConfig.lookupMappings`
   - 保存 Action 时 `lookupMappings` 会随 `actionConfig` 一起提交到 `PUT /definitions/{id}`
2. **加载逻辑修改**：
   - 删除从 `actionDetail.triggerMappings[0].mappingConfig.fieldMappings` 恢复的逻辑
   - 改为从 `formData.actionConfig.lookupMappings` 中恢复 outboundLookups
   - 在 `loadActionDetail` 中，`JSON.parse(actionConfig)` 后直接读取 `lookupMappings`
3. **删除不再需要的代码**：
   - 删除 `import { updateMappingConfig } from '@/apis/action/field-lookup'`
   - 删除保存后调用 `updateMappingConfig` 的 try-catch 块
   - 删除加载时从 triggerMappings 恢复 lookup 的逻辑

**依赖**：无

### TASK-TD-014: 后端 — 执行链路从 ActionConfig 读取 Lookup 配置

**文件**：`packages/flowFlex-backend/Application/Services/Action/ActionExecutionService.cs`（修改）

**操作步骤**：

1. **修改 Lookup 读取位置**：
   - 删除从 `ActionTriggerMapping.MappingConfig` 读取 lookup 配置的逻辑
   - 改为从 `actionDefinition.ActionConfig` 中读取 `lookupMappings` 字段
   - 解析方式：`actionDefinition.ActionConfig["lookupMappings"]?.ToObject<List<FieldMappingItem>>()`
2. **IntegrationId 获取方式不变**：
   - 仍然通过 TriggerMapping（TriggerType="Integration", TriggerSourceId）获取 IntegrationId
   - 只是 Lookup 配置的来源从 TriggerMapping 改为 ActionConfig
3. **执行流程**：
   ```
   获取 ActionDefinition → 从 ActionConfig 读取 lookupMappings
   → 如果有 lookupMappings 且 triggerMappingId 有值
   → 从 TriggerMapping 获取 IntegrationId
   → 调用 FieldLookupService.FetchLookupOptionsAsync(integrationId, lookupMappings)
   → 后续逻辑不变
   ```

**依赖**：TASK-TD-013

### TASK-TD-015: 后端 — 调整 FieldLookupService 入参

**文件**：`packages/flowFlex-backend/Application/Services/Action/FieldLookupService.cs`（修改）

**操作步骤**：

1. 修改 `FetchLookupOptionsAsync` 方法签名：
   - 原：`FetchLookupOptionsAsync(long defaultIntegrationId, JToken mappingConfig, ...)`
   - 新：`FetchLookupOptionsAsync(long integrationId, List<FieldMappingItem> lookupMappings, object? contextData = null, ...)`
2. 删除从 `mappingConfig` 解析 `MappingConfigModel` 的逻辑（不再需要）
3. 直接使用传入的 `lookupMappings` 列表
4. 其余逻辑不变（并行调用、JSONPath 提取、降级处理）

**依赖**：TASK-TD-014

### TASK-TD-016: 清理 — 删除不再需要的 MappingConfig 相关代码

**文件**：多个文件

**操作步骤**：

1. **后端**：
   - 删除 `ActionController` 中的 `PUT trigger-mappings/{id}/mapping-config` endpoint
   - 删除 `IActionManagementService.UpdateMappingConfigAsync` 方法
   - 删除 `ActionManagementService.UpdateMappingConfigAsync` 实现
   - 可选：回退 `ActionTriggerMappingWithDetails` 和 `ActionTriggerMappingInfo` 中新增的 `MappingConfig` 字段（如果不再需要）
   - 可选：回退 Repository 中 UnionAll query 里新增的 `MappingConfig = m.MappingConfig`
2. **前端**：
   - 删除 `apis/action/field-lookup.ts` 中的 `updateMappingConfig` 函数（保留 `previewLookupOptions`）
   - 确认 `ActionConfigDialog` 中不再引用 `updateMappingConfig`
3. **数据库**：
   - 已保存在 `ff_action_trigger_mappings.mapping_config` 中的 lookup 数据需要迁移到对应 Action 的 `action_config.lookupMappings` 中
   - 迁移 SQL（一次性执行）：

   ```sql
   -- 查看需要迁移的数据
   SELECT atm.id, atm.action_definition_id, atm.mapping_config
   FROM ff_action_trigger_mappings atm
   WHERE atm.mapping_config::text != '{}'
   AND atm.mapping_config::text != 'null'
   AND atm.is_valid = true;

   -- 迁移后清空 mapping_config（可选）
   -- UPDATE ff_action_trigger_mappings SET mapping_config = '{}' WHERE ...
   ```

**依赖**：TASK-TD-013, TASK-TD-014, TASK-TD-015

---

### 执行顺序

```
TASK-TD-013 (前端保存/加载改为 ActionConfig)
    ↓
TASK-TD-014 (后端执行链路从 ActionConfig 读取)
    ↓
TASK-TD-015 (FieldLookupService 入参调整)
    ↓
TASK-TD-016 (清理旧代码 + 数据迁移)
```

---

## 第七组：Lookup 认证简化（使用 contextData 中的 token + 占位符替换）

> **背景**：调试发现 `contextData` 中已经包含了 `integrationToken`（由系统在触发 Action 前自动获取并注入）。因此 Lookup 的认证不需要再通过 IntegrationId → 查 Integration → 解密凭证 → 获取 token 这条链路。用户只需在 Lookup 的 Custom Headers 中配置 `Authorization: Bearer {{integrationToken}}`，执行时通过占位符替换即可自动获得认证。

### 简化方案

**核心变更**：

- Lookup 执行时，对 `lookup.headers` 中的值也做 `{{placeholder}}` 占位符替换（使用 contextData）
- 这样 `Authorization: Bearer {{integrationToken}}` 会被替换为实际的 token 值
- IntegrationId 变为完全可选——不再是 Lookup 执行的必要条件
- `IntegrationHttpClient` 在 Lookup 场景下退化为普通的 HTTP 客户端（不需要查 Integration 认证）

**执行流程变更**：

```
之前：
  ActionConfig.lookupMappings → 需要 IntegrationId → 查 Integration → 获取认证 → 调 Lookup API

之后：
  ActionConfig.lookupMappings → 对 endpoint 和 headers 做占位符替换（用 contextData）→ 直接调 Lookup API
  （headers 中的 {{integrationToken}} 被替换为实际 token，认证自动完成）
```

---

### TASK-TD-017: 后端 — Lookup headers 支持占位符替换

**文件**：`packages/flowFlex-backend/Application/Services/Action/FieldLookupService.cs`（修改）

**操作步骤**：

1. 在 `FetchSingleFieldOptionsAsync` 中，调用 HTTP 请求之前：
   - 对 `lookup.endpoint` 执行占位符替换（已有逻辑，确认 contextData 传入）
   - **新增**：对 `lookup.headers` 中每个 value 也执行占位符替换
   - 例如：`{"Authorization": "Bearer {{integrationToken}}"}` → `{"Authorization": "Bearer eyJ..."}`
2. 占位符替换逻辑：使用正则 `\{\{(\w+)\}\}` 匹配，从 contextData 中取对应字段值替换
3. 如果 contextData 中找不到对应占位符，保留原样（不替换）

**依赖**：无

### TASK-TD-018: 后端 — 移除 Lookup 对 IntegrationId 的强依赖

**文件**：`packages/flowFlex-backend/Application/Services/Action/ActionExecutionService.cs`（修改）

**操作步骤**：

1. 修改 Lookup 执行条件：
   - **之前**：`if (integrationId.HasValue)` → 没有 IntegrationId 就不执行 Lookup
   - **之后**：只要有 `lookupMappings` 就执行 Lookup，IntegrationId 变为可选参数
2. 修改 `FetchLookupOptionsAsync` 调用：
   - 如果有 IntegrationId → 传入（用于 IntegrationHttpClient 的认证，作为备选方案）
   - 如果没有 IntegrationId → 传 0 或 null，FieldLookupService 内部直接用 headers 中的占位符替换后的认证
3. 简化后的执行流程：
   ```csharp
   var lookupMappingsToken = actionDefinition.ActionConfig?["lookupMappings"];
   if (lookupMappingsToken != null && lookupMappingsToken.Type == JTokenType.Array && lookupMappingsToken.Any())
   {
       var lookupMappings = lookupMappingsToken.ToObject<List<FieldMappingItem>>();
       if (lookupMappings != null && lookupMappings.Any())
       {
           lookupResults = await _fieldLookupService.FetchLookupOptionsAsync(
               lookupMappings, contextData, cancellationToken);
       }
   }
   ```

**依赖**：TASK-TD-017

### TASK-TD-019: 后端 — FieldLookupService 支持无 IntegrationId 的直接 HTTP 调用

**文件**：

- `packages/flowFlex-backend/Application/Services/Action/FieldLookupService.cs`（修改）
- `packages/flowFlex-backend/Application.Contracts/IServices/Action/IFieldLookupService.cs`（修改）

**操作步骤**：

1. 新增 `FetchLookupOptionsAsync` 重载（不需要 integrationId）：
   ```csharp
   Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
       List<FieldMappingItem> lookupMappings,
       object? contextData = null,
       CancellationToken cancellationToken = default);
   ```
2. 实现逻辑：
   - 对每个 lookup 字段：
     - 对 `endpoint` 做占位符替换
     - 对 `headers` 中每个 value 做占位符替换
     - 如果 `lookup.integrationId` 有值 → 使用 IntegrationHttpClient（带认证）
     - 如果 `lookup.integrationId` 为 null → 使用普通 HttpClient 直接请求（headers 已包含认证信息）
3. 新增私有方法 `FetchWithDirectHttpAsync`：
   - 使用 `IHttpClientFactory.CreateClient()` 创建普通 HTTP 客户端
   - 设置替换后的 headers
   - 发起 GET 请求
   - 返回响应 body
4. 在构造函数中注入 `IHttpClientFactory`（如果尚未注入）

**依赖**：TASK-TD-018

### TASK-TD-020: 前端 — LookupConfigPanel 移除 integrationId 强依赖

**文件**：`packages/flowFlex-common/src/app/components/actionTools/LookupConfigPanel.vue`（修改）

**操作步骤**：

1. 修改 `canTest` computed：
   - **之前**：`localConfig.endpoint?.trim() && ... && props.integrationId`
   - **之后**：`localConfig.endpoint?.trim() && localConfig.displayPath?.trim() && localConfig.valuePath?.trim()`
   - 移除对 `props.integrationId` 的依赖（Test 按钮不再要求有 integrationId）
2. 修改 `previewLookupOptions` 调用：
   - `integrationId` 参数改为可选：传 `props.integrationId || '0'`
3. 后端 Preview API 也需要适配：当 integrationId 为 0 时，使用直接 HTTP 调用（同 TASK-TD-019 的逻辑）

**依赖**：TASK-TD-019

---

### 执行顺序

```
TASK-TD-017 (headers 占位符替换)
    ↓
TASK-TD-018 (移除 IntegrationId 强依赖)
    ↓
TASK-TD-019 (FieldLookupService 支持直接 HTTP 调用)
    ↓
TASK-TD-020 (前端 LookupConfigPanel 移除 integrationId 依赖)
```

### 用户配置示例

配置 Lookup 时，Custom Headers 中填写：

```
Key: Authorization
Value: Bearer {{integrationToken}}

Key: x-tenant-id
Value: FUZE
```

执行时 `{{integrationToken}}` 会被自动替换为 contextData 中的实际 token 值。
