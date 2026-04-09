# Tasks: LSO Parcel CRM & IAM Integration — Technical Design

## 任务分组与执行顺序

### 第一组：类型定义与 API

| Task ID | 任务名称 | 文件 | 状态 |
|---------|---------|------|------|
| TD-001 | ConditionAction 类型增加 integrationId | `src/types/condition.d.ts` | ⏳ pending |
| TD-002 | 后端 ConditionAction DTO 增加 IntegrationId 属性 | `Application.Contracts/Dtos/OW/StageCondition/ConditionAction.cs` | ⏳ pending |
| TD-003 | 新增前端 API: getActiveIntegrations, getActionsByIntegration | `src/app/apis/ow/index.ts` | ⏳ pending |

### 第二组：后端核心代码修改

| Task ID | 任务名称 | 文件 | 状态 |
|---------|---------|------|------|
| TD-004 | 新增 ToCamelCase 辅助方法 | `ActionExecutor.cs` | ⏳ pending |
| TD-005 | 新增 GetStaticFieldValuesAsCamelCaseAsync 方法 | `ActionExecutor.cs` | ⏳ pending |
| TD-006 | 新增 GetIntegrationTokenAsync 方法 | `ActionExecutor.cs` | ⏳ pending |
| TD-007 | 修改 ExecuteTriggerActionAsync 注入 StaticFieldValue + Token | `ActionExecutor.cs` | ⏳ pending |
| TD-008 | 修改 ExecuteActionsAsync 支持链式数据传递 | `ActionExecutor.cs` | ⏳ pending |
| TD-009 | 新增依赖注入 IHttpClientFactory + IEncryptionService | `ActionExecutor.cs` | ⏳ pending |

### 第三组：前端 UI 修改

| Task ID | 任务名称 | 文件 | 状态 |
|---------|---------|------|------|
| TD-010 | TriggerAction 增加 Integration 下拉框 | `ConditionActionForm.vue` | ⏳ pending |
| TD-011 | Integration 选择后过滤 Action 列表 | `ConditionActionForm.vue` | ⏳ pending |
| TD-012 | 加载 Integration 列表 (onMounted) | `ConditionActionForm.vue` | ⏳ pending |

### 第四组：创建 ActionDefinition 和配置

| Task ID | 任务名称 | 方式 | 状态 |
|---------|---------|------|------|
| TD-013 | 创建 "LSO CRM Create Customer" ActionDefinition | API 调用 | ⏳ pending |
| TD-014 | 创建 "LSO IAM Create User" ActionDefinition | API 调用 | ⏳ pending |
| TD-015 | 创建 IntegrationAction 关联记录 | API/SQL | ⏳ pending |
| TD-016 | 更新 Stage Condition actionsJson（含 integrationId） | API 调用 | ⏳ pending |

### 第五组：测试

| Task ID | 任务名称 | 状态 |
|---------|---------|------|
| TD-017 | 端到端测试 | ⏳ pending |

---

## 详细任务描述

### TD-001: ConditionAction 类型增加 integrationId

**文件**: `packages/flowFlex-common/src/types/condition.d.ts`

```typescript
export interface ConditionAction {
  type: ConditionActionType;
  targetStageId?: string;
  actionDefinitionId?: string;
  integrationId?: string;       // NEW
  parameters?: Record<string, any>;
  order: number;
}
```

### TD-002: 后端 ConditionAction DTO 增加 IntegrationId

**文件**: `packages/flowFlex-backend/Application.Contracts/Dtos/OW/StageCondition/ConditionAction.cs`

```csharp
/// <summary>
/// Integration ID for TriggerAction (used to obtain OAuth2 token)
/// </summary>
[JsonProperty("integrationId")]
[JsonConverter(typeof(LongToStringConverter))]
public long? IntegrationId { get; set; }
```

### TD-003: 新增前端 API

**文件**: `packages/flowFlex-common/src/app/apis/ow/index.ts`

```typescript
// Get active integrations for dropdown
export function getActiveIntegrations() {
  return defHttp.get({
    url: `${globSetting.apiProName}/integration/${globSetting.apiVersion}/active`,
  });
}

// Get action definitions filtered by integration
export function getActionsByIntegration(integrationId: string) {
  return defHttp.get({
    url: `${globSetting.apiProName}/action/${globSetting.apiVersion}/definitions`,
    params: { integrationId, pageSize: 100 },
  });
}
```

### TD-004 ~ TD-009: 后端 ActionExecutor.cs 修改

见 design.md 中的详细代码方案。

### TD-010 ~ TD-012: 前端 ConditionActionForm.vue 修改

见 design.md 中的详细 UI 方案。

### TD-013: 创建 CRM Create Customer ActionDefinition

**API**: `POST /action/v1/definitions`

```json
{
  "actionCode": "LSO-CRM-CREATE-CUSTOMER",
  "actionName": "LSO CRM Create Customer",
  "actionType": "HttpApi",
  "triggerType": "Integration",
  "isEnabled": true,
  "actionConfig": {
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
}
```

### TD-014: 创建 IAM Create User ActionDefinition

**API**: `POST /action/v1/definitions`

```json
{
  "actionCode": "LSO-IAM-CREATE-USER",
  "actionName": "LSO IAM Create User",
  "actionType": "HttpApi",
  "triggerType": "Integration",
  "isEnabled": true,
  "actionConfig": {
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
}
```

### TD-015: 创建 IntegrationAction 关联记录

在 `ff_integration_action` 表中创建两条记录，将 CRM 和 IAM ActionDefinition 关联到 Integration `1994239810054787072`。

### TD-016: 更新 Stage Condition actionsJson

**API**: `PUT /ow/stage-conditions/v1/2042118279430017024`

```json
{
  "actionsJson": [
    {"type":"TriggerAction","order":1,"integrationId":"1994239810054787072","actionDefinitionId":"{CRM_ACTION_ID}","parameters":{}},
    {"type":"TriggerAction","order":2,"integrationId":"1994239810054787072","actionDefinitionId":"{IAM_ACTION_ID}","parameters":{}}
  ]
}
```

### TD-017: 端到端测试

1. 打开 Stage Condition 编辑器，验证 Integration 下拉框显示
2. 选择 Integration，验证 Action 列表过滤正确
3. 保存 Condition，验证 actionsJson 包含 integrationId
4. 创建 Case，填写 Dynamic Fields，Complete Stage
5. 验证 CRM Customer 创建成功
6. 验证 IAM User 创建成功
7. 检查 ActionExecution 日志
