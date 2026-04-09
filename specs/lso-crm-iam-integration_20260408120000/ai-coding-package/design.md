# Design: LSO Parcel CRM & IAM Integration Actions

## 修改文件清单

| # | 文件 | 层 | 说明 |
|---|------|---|------|
| 1 | `src/types/condition.d.ts` | 前端类型 | ConditionAction 增加 integrationId |
| 2 | `Application.Contracts/Dtos/OW/StageCondition/ConditionAction.cs` | 后端DTO | ConditionAction 增加 IntegrationId |
| 3 | `src/app/apis/ow/index.ts` | 前端API | 新增 getActiveIntegrations, getActionsByIntegration |
| 4 | `src/app/views/onboard/workflow/components/condition/ConditionActionForm.vue` | 前端UI | TriggerAction 增加 Integration 下拉框 |
| 5 | `Application/Services/OW/StageCondition/ActionExecutor.cs` | 后端 | 注入 StaticFieldValue + Token + 链式传递 |

---

<!-- 来源: technical-design/design.md -->

## 前端修改

### 1. condition.d.ts — ConditionAction 增加 integrationId

```typescript
export interface ConditionAction {
  type: ConditionActionType;
  targetStageId?: string;
  actionDefinitionId?: string;
  integrationId?: string;       // NEW: Integration ID for token auth
  parameters?: Record<string, any>;
  order: number;
}
```

### 2. ConditionActionForm.vue — TriggerAction 增加 Integration 下拉框

在 Action 下拉框之前增加 Integration 选择：
- 选择 Integration → Action 列表过滤为该 Integration 关联的 Action
- 不选 Integration → 显示所有 Action（原有行为）
- Integration 变化时清空已选 Action
- `integrationId` 存入 actionsJson 的 ConditionAction 顶层

### 3. apis/ow/index.ts — 新增 API

```typescript
export function getActiveIntegrations() {
  return defHttp.get({ url: `${globSetting.apiProName}/integration/${globSetting.apiVersion}/active` });
}
export function getActionsByIntegration(integrationId: string) {
  return defHttp.get({
    url: `${globSetting.apiProName}/action/${globSetting.apiVersion}/definitions`,
    params: { integrationId, pageSize: 100 },
  });
}
```

---

## 后端修改

### 4. ConditionAction.cs — 增加 IntegrationId

```csharp
[JsonProperty("integrationId")]
[JsonConverter(typeof(LongToStringConverter))]
public long? IntegrationId { get; set; }
```

### 5. ActionExecutor.cs — 核心修改

**新增依赖**: `IHttpClientFactory`, `IEncryptionService`

**ExecuteTriggerActionAsync 修改**:
1. 查询 StaticFieldValue → camelCase key 注入 contextData
2. 从 `action.IntegrationId` 读取 Integration ID → 获取 OAuth2 Token → 注入 `integrationToken`
3. 注入 `previousActionResult`（链式传递）
4. contextData 从匿名对象改为 `Dictionary<string, object>`

**ExecuteActionsAsync 修改**:
- 循环中维护 `previousActionResult`，传递给下一个 Action

**新增辅助方法**:
- `GetStaticFieldValuesAsCamelCaseAsync(long onboardingId)`
- `ToCamelCase(string fieldName)` — "Company  State" → "companyState"
- `GetIntegrationTokenAsync(long integrationId)` — OAuth2 Client Credentials

---

## Dynamic Fields 映射

| Field Name | camelCase Key | 用途 |
|---|---|---|
| Company Name | companyName | CRM customerName |
| Tax ID | taxId | CRM taxID |
| Company Street | companyStreet | CRM corporateAddress |
| Company Unit | companyUnit | CRM 地址补充 |
| Company  City | companyCity | CRM corporateCity (双空格) |
| Company  State | companyState | CRM corporateState (双空格) |
| Company Postal Code | companyPostalCode | CRM corporateZipCode |
| Contact First Name | contactFirstName | IAM firstName |
| Contact Last Name | contactLastName | IAM lastName |
| Contact Email | contactEmail | CRM email, IAM email |
| Contact Phone | contactPhone | CRM phone, IAM contactNumber |
| Sales Name/Email | salesName/salesEmail | CRM Sales |
| Bill To * | billTo* | CRM Billto Address |
| Payment Terms | paymentTerms | CRM contractTerms |
| Username | username | IAM userName |
| Password | password | IAM rawPassword |
| Company Code | companyCode | IAM companyCode |

---

## Action Config

### CRM Create Customer
```json
{
  "url": "https://crm-dev.item.pub/crm/customers/v2/company-customers",
  "method": "POST",
  "headers": {
    "Content-Type": "application/json;charset=UTF-8",
    "authorization": "Bearer {{integrationToken}}",
    "application-code": "unis_crm",
    "is-from-crm": "true",
    "x-tenant-id": "LT",
    "time-zone": "Asia/Shanghai"
  },
  "body": "{\"customerName\":\"{{companyName}}\",\"taxID\":\"{{taxId}}\",\"corporateAddress\":\"{{companyStreet}} {{companyUnit}}\",\"corporateCity\":\"{{companyCity}}\",\"corporateState\":\"{{companyState}}\",\"corporateZipCode\":\"{{companyPostalCode}}\",\"corporateCountry\":\"US\",\"registeredAddress\":\"{{companyStreet}} {{companyUnit}}\",\"registeredCity\":\"{{companyCity}}\",\"registeredState\":\"{{companyState}}\",\"registeredZipCode\":\"{{companyPostalCode}}\",\"registeredCountry\":\"US\",\"contractTerms\":\"{{paymentTerms}}\",\"email\":\"{{contactEmail}}\",\"phone\":\"{{contactPhone}}\"}",
  "timeout": 30
}
```

### IAM Create User
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

---

<!-- 来源: test-verification/design.md -->

## 测试用例摘要

| TC ID | 测试内容 | 类型 |
|-------|---------|------|
| TC-001 | ToCamelCase 转换（15 个 case） | 单元测试 |
| TC-002 | StaticFieldValue 查询和解析 | 单元测试 |
| TC-003 | Integration Token 获取 | 单元测试 |
| TC-004 | contextData 注入完整性 | 单元测试 |
| TC-005 | 链式传递 | 单元测试 |
| TC-006 | 前端 Integration 下拉框交互 | UI 测试 |
| TC-007 | CRM/IAM API 端到端 | 集成测试 |

## 风险登记表

| 风险 | 缓解措施 |
|------|---------|
| IAM 接口不支持 Customer 关联参数 | 提前与 IAM 团队确认 |
| ToCamelCase 转换不匹配 | 单元测试覆盖所有 Field Name |
| Integration Token 获取失败 | 日志记录 + 重试 |
| 后端 ConditionAction 反序列化不识别 integrationId | ConditionAction DTO 增加 IntegrationId 属性 |
