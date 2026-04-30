# Design: LSO Parcel CRM & IAM Integration Actions

## 系统架构概述

```
Case Complete (最后一个 Stage Completed)
    │
    ▼
Stage Condition 评估 (EvaluateAndExecuteStageConditionAsync)
    │
    ▼
TriggerAction (ConditionActionType = 6)
    │
    ├── Action 1 (Order=1): CRM 创建 Customer
    │   ├── 获取 Integration Token (OAuth2 Client Credentials)
    │   ├── 查询 StaticFieldValue 注入 contextData
    │   └── HttpApiActionExecutor → POST crm-dev.item.pub/crm/customers/v2/company-customers
    │   └── 返回 CRM Customer ID → 存入 Action 链式上下文
    │
    └── Action 2 (Order=2): IAM 创建 User
        ├── 获取 Integration Token (OAuth2 Client Credentials)
        ├── 从 Action 链式上下文获取 CRM Customer ID
        └── HttpApiActionExecutor → POST id-dev.item.pub/platform/v1/users
```

## 数据流设计

### 触发链路

1. 用户点击 Case Complete → `CompleteCurrentStageAsync()` 被调用
2. 最后一个 Stage 完成后 → `EvaluateAndExecuteStageConditionAsync()` 评估 Stage Condition
3. Stage Condition 匹配 → 执行 `TriggerAction` 类型的 ConditionAction
4. `ExecuteTriggerActionAsync()`:
   - 查询该 Onboarding 的所有 StaticFieldValue
   - 将字段值注入到 contextData 中（key = fieldName, value = fieldValueJson 解析后的值）
   - 获取 Integration Token
   - 将 Token 注入到 contextData 中
5. `ActionExecutionService.ExecuteActionAsync()` → 创建 `HttpApiActionExecutor`
6. `HttpApiActionExecutor.ExecuteAsync()` → `ReplacePlaceholders()` 替换 `{{placeholder}}` → 发送 HTTP 请求

### 字段数据流

```
StaticFieldValue (ff_static_field_values)
    │ field_name → field_value_json (解析为实际值)
    ▼
contextData = {
    OnboardingId, StageId, ConditionId, TenantId,
    // 注入的 StaticFieldValue 字段
    companyName: "Acme Corp",
    taxId: "12-3456789",
    companyStreet: "123 Main St",
    ...
    // 注入的 Integration Token
    integrationToken: "eyJhbG...",
    // Action 链式上下文（Action 2 可用）
    previousActionResult: { customerId: 12345 }
}
    │
    ▼
HttpApiActionExecutor.ReplacePlaceholders()
    │ {{companyName}} → "Acme Corp"
    │ {{integrationToken}} → "eyJhbG..."
    ▼
HTTP Request Body (JSON)
    │
    ▼
CRM API / IAM API
```

## 代码修改设计

### 修改 1: 扩展 ExecuteTriggerActionAsync 的 contextData

**文件**: `packages/flowFlex-backend/Application/Services/OW/StageCondition/ActionExecutor.cs`

**修改内容**: 在 `ExecuteTriggerActionAsync` 方法中，构建 contextData 时：

1. 查询该 Onboarding 的所有 StaticFieldValue（通过 `IStaticFieldValueRepository.GetByOnboardingIdAsync`）
2. 解析每个 StaticFieldValue 的 `FieldValueJson`，提取实际值
3. **将 Field Name 转换为 camelCase 后**以其为 key 注入到 contextData 中
4. 获取 Integration Token（通过 `IExternalIntegrationService` 或直接调用 OAuth2 端点）
5. 将 Token 以 `integrationToken` 为 key 注入到 contextData 中

> **重要**: Dynamic Fields 的 Field Name 使用带空格的 Title Case（如 "Contact First Name"、"Bill To Street"），
> 但 `HttpApiActionExecutor.ReplacePlaceholders()` 的正则 `\{\{(\w+)\}\}` 不支持空格。
> 因此注入时必须将 Field Name 转换为 camelCase：`Contact First Name` → `contactFirstName`。

**Field Name 转换映射表**（基于 API 返回的实际 Dynamic Fields 数据）：

> ⚠️ 注意：部分 Field Name 中存在双空格（如 "Company  State"、"Company  City"），ToCamelCase 转换时需要正确处理。

| Dynamic Field Name (实际) | Field ID | camelCase Key | Action Config 占位符 | 备注 |
|---|---|---|---|---|
| Company Name | 2042113396144476160 | companyName | `{{companyName}}` | |
| Tax ID | 2042113460464128000 | taxId | `{{taxId}}` | 注意：不是 "Tax ID / EIN" |
| Company Street | 2042113538813726720 | companyStreet | `{{companyStreet}}` | |
| Company  City | 2042113657667719168 | companyCity | `{{companyCity}}` | ⚠️ 双空格 |
| Company  State | 2042113698792869888 | companyState | `{{companyState}}` | ⚠️ 双空格 |
| Company Postal Code | 2042113822914908160 | companyPostalCode | `{{companyPostalCode}}` | |
| Contact First Name | (截图可见) | contactFirstName | `{{contactFirstName}}` | |
| Contact Last Name | (截图可见) | contactLastName | `{{contactLastName}}` | |
| Contact Email | 2004391300769648640 | contactEmail | `{{contactEmail}}` | isStatic=true, 系统预定义 |
| Contact Phone | 2004391301629480960 | contactPhone | `{{contactPhone}}` | isStatic=true, 系统预定义。注意：不是 "Contact Phone Number" |
| Sales Name | (截图可见) | salesName | `{{salesName}}` | |
| Sales Email | (截图可见) | salesEmail | `{{salesEmail}}` | |
| Bill To Name | (截图可见) | billToName | `{{billToName}}` | |
| Bill To Street | (截图可见) | billToStreet | `{{billToStreet}}` | |
| Bill To Unit | (截图可见) | billToUnit | `{{billToUnit}}` | |
| Bill To City | (截图可见) | billToCity | `{{billToCity}}` | |
| Bill To State | (截图可见) | billToState | `{{billToState}}` | |
| Bill To Postal Code | (截图可见) | billToPostalCode | `{{billToPostalCode}}` | |
| Payment Terms | (截图可见) | paymentTerms | `{{paymentTerms}}` | |
| Username | (截图可见) | username | `{{username}}` | |
| Password | 2042074506675228672 | password | `{{password}}` | 明文存储，MaxLength=100 |
| Verify Password | (截图可见) | - | (不传递给 API) | 仅 UI 验证用 |
| Create Account Notification | (截图可见) | - | (不传递给 API) | 仅通知用 |

> 注：以上为已确认的字段。总共 46 个 Dynamic Fields，其余字段待确认。
> "Verify Password" 和 "Create Account Notification" 不需要传递给 CRM/IAM API。

**关键发现**: 
1. `ComponentDataService.GetFieldsDataAsync()` 已经实现了将 StaticFieldValue 转换为 Dictionary 的逻辑，可以直接复用
2. 它使用原始 FieldName 作为 key（如 "Contact First Name"），而 `ReplacePlaceholders` 的正则 `\{\{(\w+)\}\}` 不支持空格
3. 因此需要额外做 camelCase 转换
4. **"Company  State" 和 "Company  City" 存在双空格**，ToCamelCase 方法使用 `StringSplitOptions.RemoveEmptyEntries` 可以正确处理

```csharp
// 伪代码
var staticFieldValues = await _staticFieldValueRepository.GetByOnboardingIdAsync(context.OnboardingId);
var fieldData = new Dictionary<string, object>();
foreach (var field in staticFieldValues.Where(f => f.IsLatest && f.IsValid))
{
    var value = ParseFieldValueJson(field.FieldValueJson);
    // Convert "Contact First Name" → "contactFirstName"
    var camelCaseKey = ToCamelCase(field.FieldName);
    fieldData[camelCaseKey] = value;
}

// 获取 Integration Token
var integration = await _integrationRepository.GetByIdAsync(integrationId);
var token = await GetIntegrationAccessTokenAsync(integration);

// 构建 contextData（使用 Dictionary 以支持动态 key）
var contextData = new Dictionary<string, object>
{
    ["OnboardingId"] = context.OnboardingId,
    ["StageId"] = context.StageId,
    ["ConditionId"] = context.ConditionId,
    ["TenantId"] = context.TenantId,
    ["ActionDefinitionId"] = action.ActionDefinitionId.Value,
    ["ActionName"] = actionDefinition.ActionName,
    ["TriggerSource"] = "StageCondition",
    ["CaseName"] = onboarding?.CaseName,
    ["CaseCode"] = onboarding?.CaseCode,
    ["WorkflowId"] = onboarding?.WorkflowId,
    ["integrationToken"] = token
};

// 注入 StaticFieldValue（camelCase key）
foreach (var kvp in fieldData)
{
    contextData[kvp.Key] = kvp.Value;
}
```

**ToCamelCase 辅助方法**:
```csharp
private static string ToCamelCase(string fieldName)
{
    if (string.IsNullOrEmpty(fieldName)) return fieldName;
    
    // Split by spaces, slashes, hyphens
    var words = fieldName.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 0) return fieldName;
    
    // First word lowercase, rest PascalCase
    var result = words[0].ToLowerInvariant();
    for (int i = 1; i < words.Length; i++)
    {
        if (words[i].Length > 0)
        {
            result += char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
        }
    }
    return result;
}
```

### 修改 2: 支持 Action 链式数据传递

**问题**: CRM Action 返回的 Customer ID 需要传递给 IAM Action。

**方案**: 在 `ConditionActionExecutor` 中，当执行多个 TriggerAction 时，将前一个 Action 的执行结果存入共享上下文，后续 Action 可以通过 `{{previousActionResult.xxx}}` 引用。

**实现**: 修改 `ExecuteTriggerActionAsync`，接收一个可选的 `previousResults` 参数，将前一个 Action 的 `ExecutionOutput` 注入到 contextData 中。

### 修改 3: Integration Token 获取

**方案**: 复用 `ExternalIntegrationService.GetIntegrationAccessTokenAsync()` 的逻辑，在 TriggerAction 执行前获取 Token。

Integration（ID: `1994239810054787072`）配置了 OAuth2 认证方式，EndpointUrl 指向 Token 端点，Credentials 中存储了 `clientId` 和 `clientSecret`。

## Action Config 设计

### CRM Create Customer Action Config

```json
{
  "url": "https://crm-dev.item.pub/crm/customers/v2/company-customers",
  "method": "POST",
  "headers": {
    "Content-Type": "application/json;charset=UTF-8",
    "accept": "application/json, text/plain, */*",
    "application-code": "unis_crm",
    "authorization": "Bearer {{integrationToken}}",
    "is-from-crm": "true",
    "x-tenant-id": "LT",
    "time-zone": "Asia/Shanghai"
  },
  "body": "{\"customerName\":\"{{companyName}}\",\"taxID\":\"{{taxId}}\",\"corporateAddress\":\"{{companyStreet}}\",\"corporateCity\":\"{{companyCity}}\",\"corporateState\":\"{{companyState}}\",\"corporateZipCode\":\"{{companyPostalCode}}\",\"corporateCountry\":\"US\",\"registeredAddress\":\"{{companyStreet}}\",\"registeredCity\":\"{{companyCity}}\",\"registeredState\":\"{{companyState}}\",\"registeredZipCode\":\"{{companyPostalCode}}\",\"registeredCountry\":\"US\",\"contractTerms\":\"{{paymentTerms}}\",\"email\":\"{{contactEmail}}\",\"phone\":\"{{contactPhone}}\"}",
  "timeout": 30
}
```

### IAM Create User Action Config

```json
{
  "url": "https://id-dev.item.pub/platform/v1/users",
  "method": "POST",
  "headers": {
    "Content-Type": "application/json",
    "authorization": "Bearer {{integrationToken}}",
    "x-app-code": "default",
    "x-app-id": "5",
    "time-zone": "Asia/Shanghai"
  },
  "body": "{\"userName\":\"{{username}}\",\"rawPassword\":\"{{password}}\",\"companyCode\":\"{{companyCode}}\",\"firstName\":\"{{contactFirstName}}\",\"lastName\":\"{{contactLastName}}\",\"email\":\"{{contactEmail}}\",\"contactNumber\":\"{{contactPhone}}\",\"userTags\":[\"Admin\"],\"roles\":[\"Admin\"],\"userType\":0}",
  "timeout": 30
}
```

> **注意**: IAM Action Config 中的 Customer 关联参数待 IAM 团队确认接口支持后补充。

## 设计决策记录

### DD-001: StaticFieldValue 注入方式 ✅ 已确认

在 `ExecuteTriggerActionAsync` 触发时查询该 Case 的所有 StaticFieldValue 并注入到 contextData。

### DD-002: Token 管理 ✅ 已确认

通过 Integration 的 OAuth2 Client Credentials 获取 Token，复用 `ExternalIntegrationService.GetIntegrationAccessTokenAsync()` 逻辑。

### DD-003: Action 执行顺序和数据传递 ✅ 已确认

CRM Action 先执行（Order=1），IAM Action 后执行（Order=2）。需要实现 Action 链式数据传递，将 CRM 返回的 Customer ID 传递给 IAM Action。

### DD-004: IAM User 关联 CRM Customer ⏳ 待确认

IAM 页面上 External 类型用户可以配置 Customer 关联。需要确认 IAM 的 `POST /platform/v1/users` 接口是否支持传递 Customer 关联参数。如不支持，需要 IAM 团队添加。

## 风险项

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| IAM 接口不支持 Customer 关联参数 | IAM User 无法自动关联 CRM Customer | 🟡 中 | 提前与 IAM 团队确认，必要时推动接口升级 |
| Integration Token 过期（执行两个 Action 之间） | 第二个 Action 执行失败 | 🟢 低 | Token 有效期通常较长（1小时），两个 Action 执行间隔很短 |
| StaticFieldValue 字段名不匹配 | 占位符替换失败，API 请求参数为空 | 🟡 中 | 在配置 Dynamic Fields 时确保 fieldName 与 Action Config 中的占位符一致 |
| CRM API 返回格式变化 | 无法提取 Customer ID | 🟢 低 | 记录完整的 API 响应到 ActionExecution 日志 |
