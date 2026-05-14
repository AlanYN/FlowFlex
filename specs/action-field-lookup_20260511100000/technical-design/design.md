# Design: Action Field Lookup — 技术方案设计

## 1. 技术栈确认

| 层          | 技术                              | 说明                         |
| ----------- | --------------------------------- | ---------------------------- |
| 后端        | C# / ASP.NET Core                 | 现有项目技术栈               |
| ORM         | SqlSugar                          | 现有项目 ORM                 |
| JSON        | Newtonsoft.Json                   | JSONPath 支持（SelectToken） |
| HTTP        | IHttpClientFactory                | 现有 HTTP 客户端工厂         |
| 加密        | IEncryptionService                | 现有凭证加密服务             |
| 前端        | Vue 3 + TypeScript + Element Plus | 现有前端技术栈               |
| HTTP 客户端 | defHttp (Axios wrapper)           | 现有前端 HTTP 封装           |

---

## 2. 后端架构设计

### 2.1 新增文件清单

```
Application.Contracts/
├── Dtos/Action/FieldLookupDto.cs                    ← 新增：Lookup 相关 DTO
├── IServices/Action/IFieldLookupService.cs          ← 新增：FieldLookup 服务接口
└── IServices/Integration/IIntegrationHttpClient.cs  ← 新增：Integration HTTP 客户端接口

Application/
└── Services/
    ├── Action/FieldLookupService.cs                 ← 新增：FieldLookup 服务实现
    └── Integration/IntegrationHttpClient.cs         ← 新增：Integration HTTP 客户端实现

WebApi/
└── Controllers/Action/ActionController.cs           ← 修改：新增 preview + mapping-config endpoints
```

### 2.2 IntegrationHttpClient 详细设计

```csharp
public interface IIntegrationHttpClient : IScopedService
{
    Task<IntegrationHttpResponse> GetAsync(
        long integrationId,
        string relativePath,
        Dictionary<string, string>? additionalHeaders = null,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default);

    Task<IntegrationHttpResponse> PostAsync(
        long integrationId,
        string relativePath,
        object? body = null,
        Dictionary<string, string>? additionalHeaders = null,
        int timeoutSeconds = 10,
        CancellationToken cancellationToken = default);
}

public class IntegrationHttpResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}
```

**实现要点**：

1. 注入 `IIntegrationRepository`、`IEncryptionService`、`IIntegrationApiLogService`、`IHttpClientFactory`
2. 认证逻辑复用 `IntegrationService.PerformConnectionTestAsync` 中的模式：
   - ApiKey: `X-API-Key` + `Authorization: ApiKey {key}`
   - BasicAuth: `Authorization: Basic {base64(username:password)}`
   - OAuth2: POST EndpointUrl（token endpoint）获取 access_token → `Authorization: Bearer {access_token}`
   - BearerToken: `Authorization: Bearer {token}`
3. **URL 处理规则**：
   - 如果 `relativePath` 以 `http://` 或 `https://` 开头 → 直接作为请求 URL（完整 URL 模式）
   - 否则 → 拼接 `integration.EndpointUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/')`
   - OAuth2 场景下，EndpointUrl 是 token endpoint，Lookup endpoint 通常是完整 URL
4. additionalHeaders 在认证 headers 之后合并（覆盖同名 key）
5. 使用 `IIntegrationApiLogService.LogApiCallAsync()` 记录日志
6. 超时使用 `CancellationTokenSource` + `TimeSpan.FromSeconds(timeoutSeconds)`

### 2.3 FieldLookupService 详细设计

```csharp
public interface IFieldLookupService : IScopedService
{
    Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
        long defaultIntegrationId,
        JToken mappingConfig,
        CancellationToken cancellationToken = default);

    Task<FieldLookupResult> PreviewLookupAsync(
        long integrationId,
        LookupPreviewRequest request,
        CancellationToken cancellationToken = default);
}
```

**核心逻辑**：

```csharp
public async Task<List<FieldLookupResult>> FetchLookupOptionsAsync(...)
{
    // 1. 解析 MappingConfig
    var config = mappingConfig.ToObject<MappingConfigModel>();
    if (config?.FieldMappings == null) return new List<FieldLookupResult>();

    // 2. 筛选有 lookup 的字段
    var lookupFields = config.FieldMappings.Where(f => f.Lookup != null).ToList();
    if (!lookupFields.Any()) return new List<FieldLookupResult>();

    // 3. 获取全局配置
    var timeout = config.LookupConfig?.TimeoutSeconds ?? 10;
    var maxOptions = config.LookupConfig?.MaxOptionsPerField ?? 200;

    // 4. 并行获取选项列表
    var tasks = lookupFields.Select(field => FetchSingleFieldOptionsAsync(
        field, defaultIntegrationId, timeout, maxOptions, cancellationToken));

    var results = await Task.WhenAll(tasks);
    return results.ToList();
}

private async Task<FieldLookupResult> FetchSingleFieldOptionsAsync(...)
{
    try
    {
        var integrationId = field.Lookup.IntegrationId ?? defaultIntegrationId;
        var response = await _integrationHttpClient.GetAsync(
            integrationId, field.Lookup.Endpoint, field.Lookup.Headers, timeout, cancellationToken);

        if (!response.IsSuccess)
            return FieldLookupResult.Failed(field.ApiField, response.Error);

        // 解析响应
        var json = JToken.Parse(response.Body);
        var array = string.IsNullOrEmpty(field.Lookup.ResponsePath)
            ? json : json.SelectToken(field.Lookup.ResponsePath);

        if (array == null || array.Type != JTokenType.Array)
            return FieldLookupResult.Failed(field.ApiField, "Response path did not resolve to an array");

        var options = array.Children()
            .Take(maxOptions)
            .Select(item => new OptionItem
            {
                Display = item.SelectToken(field.Lookup.DisplayPath)?.ToString() ?? "",
                Value = item.SelectToken(field.Lookup.ValuePath)?.ToString() ?? ""
            })
            .ToList();

        return FieldLookupResult.Success(field.ApiField, options, array.Count());
    }
    catch (Exception ex)
    {
        return FieldLookupResult.Failed(field.ApiField, ex.Message);
    }
}
```

### 2.4 DTO 定义

```csharp
// FieldLookupDto.cs

public class MappingConfigModel
{
    public List<FieldMappingItem>? FieldMappings { get; set; }
    public LookupGlobalConfig? LookupConfig { get; set; }
}

public class FieldMappingItem
{
    public string WfeField { get; set; } = string.Empty;
    public string ApiField { get; set; } = string.Empty;
    public LookupConfig? Lookup { get; set; }
}

public class LookupConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public string ValuePath { get; set; } = string.Empty;
    public string? ResponsePath { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public long? IntegrationId { get; set; }
}

public class LookupGlobalConfig
{
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxOptionsPerField { get; set; } = 200;
}

public class OptionItem
{
    public string Display { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class FieldLookupResult
{
    public string ApiField { get; set; } = string.Empty;
    public string Status { get; set; } = "success"; // "success" | "lookup_failed"
    public List<OptionItem> Options { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }

    public static FieldLookupResult Success(string apiField, List<OptionItem> options, int totalCount)
        => new() { ApiField = apiField, Status = "success", Options = options, TotalCount = totalCount };

    public static FieldLookupResult Failed(string apiField, string? error)
        => new() { ApiField = apiField, Status = "lookup_failed", Error = error };
}

public class LookupPreviewRequest
{
    [Required] public long IntegrationId { get; set; }
    [Required] public string Endpoint { get; set; } = string.Empty;
    [Required] public string DisplayPath { get; set; } = string.Empty;
    [Required] public string ValuePath { get; set; } = string.Empty;
    public string? ResponsePath { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

public class LookupPreviewResponse
{
    public bool Success { get; set; }
    public List<OptionItem> Options { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }
}
```

### 2.5 API Endpoints

```csharp
// ActionController.cs 新增

/// <summary>
/// Preview lookup options for a field configuration
/// </summary>
[HttpPost("lookup/preview")]
[WFEAuthorize(PermissionConsts.Tool.Read)]
[ProducesResponseType<SuccessResponse<LookupPreviewResponse>>((int)HttpStatusCode.OK)]
public async Task<IActionResult> PreviewLookup([FromBody] LookupPreviewRequest request)
{
    var result = await _fieldLookupService.PreviewLookupAsync(request.IntegrationId, request);
    return Success(new LookupPreviewResponse
    {
        Success = result.Status == "success",
        Options = result.Options.Take(10).ToList(),
        TotalCount = result.TotalCount,
        Error = result.Error
    });
}

/// <summary>
/// Update mapping config for an action trigger mapping
/// </summary>
[HttpPut("trigger-mappings/{id}/mapping-config")]
[WFEAuthorize(PermissionConsts.Tool.Update)]
[ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
public async Task<IActionResult> UpdateMappingConfig(long id, [FromBody] JToken mappingConfig)
{
    var result = await _actionManagementService.UpdateMappingConfigAsync(id, mappingConfig);
    return Success(result);
}
```

---

## 3. 执行链路集成

### 3.1 修改 ActionExecutionService

在 `ExecuteActionAsync` 方法中，executor 调用前插入 lookup 逻辑：

```csharp
// 在获取 actionDefinition 之后，创建 executor 之前：

// Fetch lookup options if trigger mapping has lookup config
List<FieldLookupResult>? lookupResults = null;
if (triggerMappingId.HasValue)
{
    var triggerMapping = await _actionTriggerMappingRepository.GetByIdAsync(triggerMappingId.Value);
    if (triggerMapping?.MappingConfig != null && triggerMapping.MappingConfig.Type != JTokenType.Null)
    {
        // Determine IntegrationId from trigger mapping
        long? integrationId = null;
        if (triggerMapping.TriggerType == "Integration")
        {
            integrationId = triggerMapping.TriggerSourceId;
        }

        if (integrationId.HasValue)
        {
            lookupResults = await _fieldLookupService.FetchLookupOptionsAsync(
                integrationId.Value, triggerMapping.MappingConfig, cancellationToken);
        }
    }
}

// Store lookup metadata in execution output
if (lookupResults != null && lookupResults.Any())
{
    execution.ExecutionInput = JToken.FromObject(new { lookupResults });
}
```

### 3.2 不影响现有逻辑的保证

- `triggerMappingId` 为 null 时（直接执行），完全跳过 lookup 逻辑
- MappingConfig 为空 JObject 时（无 fieldMappings），`FetchLookupOptionsAsync` 返回空列表
- lookup 失败不抛异常，只标记 `lookup_failed`
- executor 调用逻辑不变

---

## 4. 前端技术设计

### 4.1 新增文件

```
src/app/
├── apis/action-field-lookup.ts              ← API 调用函数
├── components/actionTools/
│   └── LookupConfigPanel.vue                ← Lookup 配置面板组件
└── types/action-field-lookup.d.ts           ← TypeScript 类型定义
```

### 4.2 TypeScript 类型

```typescript
// types/action-field-lookup.d.ts

export interface LookupConfig {
  endpoint: string;
  displayPath: string;
  valuePath: string;
  responsePath?: string;
  headers?: Record<string, string>;
  integrationId?: string | null;
}

export interface FieldMappingItemWithLookup {
  externalFieldName: string;
  wfeFieldId: string;
  fieldType: number;
  syncDirection: number;
  lookupEnabled?: boolean;
  lookup?: LookupConfig;
}

export interface LookupPreviewRequest {
  integrationId: string;
  endpoint: string;
  displayPath: string;
  valuePath: string;
  responsePath?: string;
  headers?: Record<string, string>;
}

export interface LookupPreviewResponse {
  success: boolean;
  options: OptionItem[];
  totalCount: number;
  error?: string;
}

export interface OptionItem {
  display: string;
  value: string;
}
```

### 4.3 API 函数

```typescript
// apis/action-field-lookup.ts
import { defHttp } from "#/utils/http";
import { useGlobSetting } from "@/hooks/setting";

const globSetting = useGlobSetting();

const Api = () => ({
  preview: `/action/${globSetting.apiVersion}/lookup/preview`,
  mappingConfig: (id: string) =>
    `/action/${globSetting.apiVersion}/trigger-mappings/${id}/mapping-config`,
});

export function previewLookupOptions(data: LookupPreviewRequest) {
  return defHttp.post<LookupPreviewResponse>({ url: Api().preview }, { data });
}

export function updateMappingConfig(mappingId: string, config: object) {
  return defHttp.put<boolean>(
    { url: Api().mappingConfig(mappingId) },
    { data: config },
  );
}
```

### 4.4 LookupConfigPanel 组件设计

```vue
<!-- 核心 Props/Emits -->
<script setup lang="ts">
interface Props {
  modelValue: LookupConfig;
  integrationId: string;
  disabled?: boolean;
}

const emit = defineEmits<{
  (e: "update:modelValue", value: LookupConfig): void;
}>();
</script>
```

**组件内部状态**：

- `previewLoading`: Test 按钮 loading
- `previewResult`: 预览结果（options[] 或 error）
- `headersExpanded`: Custom Headers 折叠状态
- `headersList`: Key-Value 对数组

### 4.5 ActionConfigDialog 修改要点

1. 扩展 `IFieldMappingItem` 接口，增加 `lookupEnabled` 和 `lookup`
2. 表格新增 Lookup 列（`el-switch`）
3. 使用 `el-table` 的 expand 功能展开 `LookupConfigPanel`
4. 保存时：将 `fieldMappings`（含 lookup）构建为 MappingConfig JSON，调用 `updateMappingConfig` API
5. 加载时：从 ActionTriggerMapping 的 MappingConfig 中解析并回显

---

## 5. 风险登记表

| 风险                      | 影响                            | 概率 | 缓解措施                                                  |
| ------------------------- | ------------------------------- | ---- | --------------------------------------------------------- |
| OAuth2 token 过期         | lookup 调用失败                 | 中   | 降级为 lookup_failed，不阻断流程；后续可加 token 刷新逻辑 |
| 第三方 API 响应格式不一致 | JSONPath 提取失败               | 中   | 前端 Test 按钮可提前验证；错误信息明确提示 path 问题      |
| 大选项列表（>200 条）     | Token 消耗大（AI 阶段）/ 响应慢 | 低   | maxOptionsPerField 截断；当前阶段不涉及 AI                |
| 并行 lookup 调用过多      | 第三方 API 限流                 | 低   | 实际场景中单个 Action 通常 1-3 个 lookup 字段             |
| MappingConfig 结构变更    | 旧数据不兼容                    | 极低 | 当前 MappingConfig 为空 JObject，无历史数据               |
