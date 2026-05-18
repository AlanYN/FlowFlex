# Design: Action Field Lookup（第三方选项数据拉取）

## 1. 数据模型设计

### 1.1 MappingConfig 扩展结构

在现有 `ActionTriggerMapping.MappingConfig`（JSONB）中增加 `fieldMappings` 和 `lookupConfig`：

```json
{
  "fieldMappings": [
    {
      "wfeField": "sales_rep_name",
      "apiField": "sales_rep_id",
      "lookup": {
        "endpoint": "/api/users?active=true&role=sales",
        "displayPath": "$.full_name",
        "valuePath": "$.user_id",
        "responsePath": "$.data",
        "headers": {
          "X-Custom-Header": "some-value"
        },
        "integrationId": null
      }
    },
    {
      "wfeField": "company_name",
      "apiField": "company_name"
    }
  ],
  "lookupConfig": {
    "timeoutSeconds": 10,
    "maxOptionsPerField": 200
  }
}
```

### 1.2 Lookup 属性说明

| 属性            | 类型        | 必填 | 说明                                                                                                                                                                                      |
| --------------- | ----------- | ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `endpoint`      | string      | ✅   | 获取选项列表的 API 路径。支持两种格式：相对路径（如 `/api/users`，拼接 Integration.EndpointUrl）或完整 URL（如 `https://api.crm.com/api/users`，直接使用，只从 Integration 取认证 token） |
| `displayPath`   | string      | ✅   | 选项中用于显示/匹配的文本字段 JSONPath                                                                                                                                                    |
| `valuePath`     | string      | ✅   | 选项中要传给目标 API 的值字段 JSONPath                                                                                                                                                    |
| `responsePath`  | string      | ❌   | 响应中选项数组的 JSONPath（默认 `$` 即根级数组）                                                                                                                                          |
| `headers`       | object      | ❌   | 自定义请求头（Key-Value 对），与 Integration 认证 headers 合并，冲突时 lookup headers 优先                                                                                                |
| `integrationId` | string/null | ❌   | 覆盖默认 Integration（跨系统查询时使用）                                                                                                                                                  |

### 1.3 lookupConfig 全局配置

| 属性                 | 类型 | 默认值 | 说明                             |
| -------------------- | ---- | ------ | -------------------------------- |
| `timeoutSeconds`     | int  | 10     | 单个 lookup API 调用超时时间     |
| `maxOptionsPerField` | int  | 200    | 单个字段最大选项数量（超出截断） |

---

## 2. 后端架构设计

### 2.1 新增服务：IntegrationHttpClient

```
Application.Contracts/IServices/Integration/IIntegrationHttpClient.cs
Application/Services/Integration/IntegrationHttpClient.cs
```

**职责**：根据 IntegrationId 自动获取认证信息，发起 HTTP 请求。

**接口定义**：

```csharp
public interface IIntegrationHttpClient : IScopedService
{
    /// <summary>
    /// Send GET request using Integration's base URL and authentication
    /// </summary>
    Task<IntegrationHttpResponse> GetAsync(
        long integrationId,
        string relativePath,
        Dictionary<string, string>? additionalHeaders = null,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send POST request using Integration's base URL and authentication
    /// </summary>
    Task<IntegrationHttpResponse> PostAsync(
        long integrationId,
        string relativePath,
        object? body = null,
        Dictionary<string, string>? additionalHeaders = null,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default);
}
```

**认证处理逻辑**：

```
GetAsync(integrationId, path, additionalHeaders)
  → 查询 Integration 实体（EndpointUrl + AuthMethod + EncryptedCredentials）
  → 解密凭证
  → 根据 AuthMethod 获取认证 token/headers：
      ApiKey     → X-API-Key + Authorization: ApiKey {key}
      BasicAuth  → Authorization: Basic {base64(user:pass)}
      OAuth2     → POST EndpointUrl（token endpoint）获取 access_token → Authorization: Bearer {token}
      BearerToken → Authorization: Bearer {token}
  → 确定请求 URL：
      如果 path 以 http:// 或 https:// 开头 → 直接使用（完整 URL）
      否则 → 拼接 EndpointUrl + path（相对路径）
  → 合并 additionalHeaders（lookup 自定义 headers，冲突时覆盖认证 headers）
  → 发起 HTTP 请求
  → 记录 IntegrationApiLog
  → 返回 IntegrationHttpResponse
```

> **URL 处理规则**：Lookup endpoint 支持两种格式：
>
> - **相对路径**（如 `/api/users`）→ 拼接 Integration.EndpointUrl 作为 base URL（适用于 ApiKey/BasicAuth/BearerToken，EndpointUrl 是 API base URL）
> - **完整 URL**（如 `https://api.crm.com/api/users`）→ 直接使用，只从 Integration 获取认证 token（适用于 OAuth2，EndpointUrl 是 token endpoint）

### 2.2 新增服务：FieldLookupService

```
Application.Contracts/IServices/Action/IFieldLookupService.cs
Application/Services/Action/FieldLookupService.cs
```

**职责**：解析 MappingConfig 中的 lookup 配置，批量拉取选项列表。

**接口定义**：

```csharp
public interface IFieldLookupService : IScopedService
{
    /// <summary>
    /// Fetch option lists for all lookup fields in the mapping config
    /// </summary>
    Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
        long defaultIntegrationId,
        JToken mappingConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview lookup options for a single field (used by frontend test button)
    /// </summary>
    Task<FieldLookupResult> PreviewLookupAsync(
        long integrationId,
        LookupPreviewRequest request,
        CancellationToken cancellationToken = default);
}
```

### 2.3 执行链路集成点

```
ActionTriggerService.ExecuteActionsForTriggerAsync()
  → 遍历 actionMappings
  → ActionExecutionService.ExecuteActionAsync(actionDefinitionId, contextData, userId, triggerMappingId)
      → 获取 ActionDefinition
      → 【新增】如果 triggerMappingId 有值，查询 ActionTriggerMapping.MappingConfig
      → 【新增】解析 MappingConfig 中的 fieldMappings
      → 【新增】对有 lookup 的字段，调用 FieldLookupService.FetchLookupOptionsAsync()
      → 【新增】将选项列表注入到 contextData 或 executionInput 中
      → 创建 Executor 并执行
      → 保存执行记录（含 lookup 结果元数据）
```

### 2.4 数据流图

```
┌─────────────────────────────────────────────────────────────────┐
│                    Action Execution Flow                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Trigger Event                                                    │
│       │                                                           │
│       ▼                                                           │
│  ActionTriggerService                                             │
│       │                                                           │
│       ▼                                                           │
│  ActionExecutionService.ExecuteActionAsync()                      │
│       │                                                           │
│       ├── 读取 ActionTriggerMapping.MappingConfig                 │
│       │                                                           │
│       ├── 分离字段：                                              │
│       │   ├── 普通字段 → 直接映射                                 │
│       │   └── Lookup 字段 → FieldLookupService                   │
│       │                                                           │
│       ▼                                                           │
│  FieldLookupService.FetchLookupOptionsAsync()                     │
│       │                                                           │
│       ├── 并行调用 IntegrationHttpClient.GetAsync(additionalHeaders)│
│       │   ├── Field A: GET /api/users → [options...]             │
│       │   ├── Field B: GET /api/enums/program → [options...]     │
│       │   └── Field C: GET /api/facilities → [options...]        │
│       │                                                           │
│       ▼                                                           │
│  返回 List<FieldLookupResult>                                    │
│       │                                                           │
│       ├── 成功：{ field, options: [{display, value}...] }        │
│       └── 失败：{ field, error, status: "lookup_failed" }        │
│                                                                   │
│       ▼                                                           │
│  【未来扩展点】AI 匹配 / 人工选择                                │
│       │                                                           │
│       ▼                                                           │
│  HttpApiActionExecutor.ExecuteAsync()                             │
│       │                                                           │
│       ▼                                                           │
│  保存 ActionExecution（含 lookup 元数据）                         │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. 前端设计

### 3.1 Mapping 配置 UI 改动（Action 编辑弹窗内）

在 Action 编辑弹窗的字段映射区域，每行增加"Lookup"开关：

```
┌─────────────────────────────────────────────────────────────────┐
│  Field Mapping Configuration                                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  WFE Field          →    API Field           Lookup              │
│  ─────────────────────────────────────────────────────────────   │
│  [company_name ▼]  →    [company_name  ]   ☐                    │
│  [sales_rep    ▼]  →    [sales_rep_id  ]   ☑ Lookup             │
│                         ┌──────────────────────────────────┐    │
│                         │ Options Source: [/api/users      ]│    │
│                         │ Display Field:  [$.full_name     ]│    │
│                         │ Value Field:    [$.user_id       ]│    │
│                         │ Response Path:  [$.data          ]│    │
│                         │                                    │    │
│                         │ ▸ Custom Headers (optional)        │    │
│                         │   Key: [X-Custom]  Value: [xxx]   │    │
│                         │                                    │    │
│                         │ [Test] ← 预览按钮                 │    │
│                         │                                    │    │
│                         │ Preview Results:                   │    │
│                         │ ┌────────────┬──────────┐         │    │
│                         │ │ Display    │ Value    │         │    │
│                         │ ├────────────┼──────────┤         │    │
│                         │ │ Zhang San  │ USER_001 │         │    │
│                         │ │ Li Si      │ USER_002 │         │    │
│                         │ └────────────┴──────────┘         │    │
│                         └──────────────────────────────────┘    │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 前端组件结构

```
src/app/components/actionTools/
├── ActionConfigDialog.vue        ← 已有，修改 Field Mapping 区域
└── LookupConfigPanel.vue         ← 新增，Lookup 配置面板（含 headers + 预览）
```

### 3.3 API 接口

| 方法 | 路径                                                 | 说明                          |
| ---- | ---------------------------------------------------- | ----------------------------- |
| POST | `/ow/actions/v1/lookup/preview`                      | 预览 lookup 选项（Test 按钮） |
| PUT  | `/ow/action-trigger-mappings/v1/{id}/mapping-config` | 保存 MappingConfig            |

---

## 4. 关键设计决策

| 决策                   | 选择                                                                   | 理由                                    |
| ---------------------- | ---------------------------------------------------------------------- | --------------------------------------- |
| lookup endpoint 格式   | 相对路径（相对于 Integration.EndpointUrl）                             | 复用 Integration 认证，配置简洁         |
| IntegrationId 来源     | 默认从 TriggerMapping 关联的 Integration 获取，可选覆盖                | 覆盖 90% 场景，保留灵活性               |
| 自定义 Headers         | 可选配置，与 Integration 认证 headers 合并，冲突时 lookup headers 优先 | 满足特殊字典接口需要额外 headers 的场景 |
| 选项列表存储           | 不持久化，每次实时拉取                                                 | 数据始终最新，无需维护同步              |
| 多字段 lookup 执行方式 | Task.WhenAll 并行                                                      | 减少总延迟                              |
| 失败处理               | 单字段降级，不阻断流程                                                 | 容错优先                                |
| JSONPath 实现          | Newtonsoft.Json 的 SelectToken                                         | 项目已有依赖，无需引入新库              |

---

## 5. 为 AI 匹配预留的扩展点

本阶段的设计已为后续 AI 匹配预留以下扩展点：

1. **FieldLookupResult** 返回结构化的 `List<OptionItem>`，AI 匹配服务可直接消费
2. **ActionExecution.ExecutionOutput** 中记录 lookup 元数据，AI 匹配结果可追加到同一字段
3. **MappingConfig.lookupConfig** 预留 `aiConfig` 扩展位置
4. **FieldLookupService** 返回后，执行链路中有明确的"下一步处理"插入点

当 Amanda 确认 AI 方案后，只需：

- 新增 `FieldMatchingService`（调用 LLM）
- 在 `FieldLookupService` 返回后插入 AI 匹配步骤
- 在 MappingConfig 中启用 `aiConfig`
