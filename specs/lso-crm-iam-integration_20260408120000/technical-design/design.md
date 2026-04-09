# Technical Design: LSO Parcel CRM & IAM Integration Actions

## 技术栈

- 后端: C# / ASP.NET Core / SqlSugar ORM / PostgreSQL
- 前端: Vue 3 + TypeScript + Element Plus + TailwindCSS
- 外部 API: CRM (crm-dev.item.pub), IAM (id-dev.item.pub)
- 认证: OAuth2 Client Credentials (通过 Integration)

## 已有配置

| 实体 | ID |
|------|-----|
| Workflow | 2042115982671089664 |
| Stage | 2042116465028632576 |
| Stage Condition | 2042118279430017024 |
| 占位 ActionDefinition（需替换） | 2004482114174717952 |
| Integration | 1994239810054787072 |

## 修改文件清单

| # | 文件 | 层 | 说明 |
|---|------|---|------|
| 1 | `src/types/condition.d.ts` | 前端类型 | ConditionAction 增加 integrationId |
| 2 | `src/app/apis/ow/index.ts` | 前端API | 新增获取 Integration 关联 Action 的 API |
| 3 | `src/app/views/onboard/workflow/components/condition/ConditionActionForm.vue` | 前端UI | TriggerAction 增加 Integration 下拉框 |
| 4 | `Application/Services/OW/StageCondition/ActionExecutor.cs` | 后端 | 注入 StaticFieldValue + Token + 链式传递 |

---

## 前端修改设计

### 1. 类型定义 — condition.d.ts

在 `ConditionAction` 接口中增加 `integrationId`：

```typescript
export interface ConditionAction {
  type: ConditionActionType;
  targetStageId?: string;
  actionDefinitionId?: string;
  integrationId?: string;       // NEW: 关联的 Integration ID
  parameters?: Record<string, any>;
  order: number;
}
```

### 2. API — apis/ow/index.ts

新增获取 Integration 列表和 Integration 关联 Action 的 API：

```typescript
// Get active integrations list (for dropdown)
export function getActiveIntegrations(): Promise<ApiResponse<IntegrationOption[]>> {
  return defHttp.get({
    url: `${globSetting.apiProName}/integration/${globSetting.apiVersion}/active`,
  });
}

// Get actions by integration ID (filtered by integration)
export function getActionsByIntegration(integrationId: string): Promise<ApiResponse<ActionOption[]>> {
  return defHttp.get({
    url: `${globSetting.apiProName}/action/${globSetting.apiVersion}/definitions`,
    params: { integrationId, pageSize: 100 },
  });
}
```

### 3. UI — ConditionActionForm.vue

在 TriggerAction 区域，Action 下拉框之前增加 Integration 下拉框：

```vue
<!-- TriggerAction: Integration Selection (NEW) -->
<el-form-item
  v-if="action.type === 'TriggerAction'"
  label="Integration"
  class="action-field"
>
  <el-select
    v-model="action.integrationId"
    placeholder="Select integration (optional)"
    clearable
    @change="(val) => handleIntegrationChange(action, val)"
  >
    <el-option
      v-for="intg in availableIntegrations"
      :key="intg.id"
      :label="intg.name"
      :value="intg.id"
    />
  </el-select>
</el-form-item>

<!-- TriggerAction: Action Definition (existing, filtered by integration) -->
<el-form-item
  v-if="action.type === 'TriggerAction'"
  label="Action"
  class="action-field"
  prop="actionDefinitionId"
>
  <el-select
    v-model="action.actionDefinitionId"
    placeholder="Select action"
  >
    <!-- When integration selected: show integration actions -->
    <!-- When no integration: show all actions grouped by ToolsType -->
    <el-option-group
      v-for="(actions, groupName) in getFilteredActions(action)"
      :key="groupName"
      :label="groupName"
    >
      <el-option
        v-for="act in actions"
        :key="act.id"
        :label="act.name"
        :value="act.id"
      />
    </el-option-group>
  </el-select>
</el-form-item>
```

**新增逻辑**：

```typescript
// Integration 列表
const availableIntegrations = ref<{ id: string; name: string }[]>([]);

// Integration 关联的 Action 缓存
const integrationActionsCache = ref<Record<string, ActionOption[]>>({});

// 加载 Integration 列表
const loadIntegrations = async () => {
  const res = await getActiveIntegrations();
  if (res.code === '200' && res.data) {
    availableIntegrations.value = res.data.map((i: any) => ({
      id: i.id, name: i.name
    }));
  }
};

// Integration 选择变化
const handleIntegrationChange = async (action: ActionFormItem, integrationId: string) => {
  action.actionDefinitionId = undefined; // 清空已选 Action
  if (integrationId && !integrationActionsCache.value[integrationId]) {
    const res = await getActionsByIntegration(integrationId);
    if (res.code === '200' && res.data) {
      integrationActionsCache.value[integrationId] = res.data;
    }
  }
};

// 获取过滤后的 Action 列表
const getFilteredActions = (action: ActionFormItem) => {
  if (action.integrationId && integrationActionsCache.value[action.integrationId]) {
    return { 'Integration Actions': integrationActionsCache.value[action.integrationId] };
  }
  return groupedActions.value; // 无 Integration 时显示全部
};
```

**交互流程**：
1. 用户选择 Action Type = TriggerAction
2. 出现 Integration 下拉框（可选）和 Action 下拉框
3. 用户选择 Integration → Action 下拉框自动过滤为该 Integration 关联的 Action
4. 用户不选 Integration → Action 下拉框显示所有 Action（原有行为）
5. 保存时 `integrationId` 存入 actionsJson

---

## 后端修改设计

### 4. ActionExecutor.cs — ExecuteTriggerActionAsync

**新增依赖注入**：`IHttpClientFactory`、`IEncryptionService`

**修改内容**：

1. 从 `action.Parameters` 或 ConditionAction 的 `integrationId` 字段读取 Integration ID
2. 查询 StaticFieldValue 并以 camelCase key 注入 contextData
3. 通过 Integration ID 获取 OAuth2 Token 注入 contextData
4. 支持 Action 链式数据传递

```csharp
private async Task<ActionExecutionDetail> ExecuteTriggerActionAsync(
    ConditionAction action, ActionExecutionContext context, JToken? previousActionResult = null)
{
    // ... 原有验证逻辑 ...

    // 1. 查询 StaticFieldValue
    var fieldData = await GetStaticFieldValuesAsCamelCaseAsync(context.OnboardingId);

    // 2. 获取 Integration Token（从 action 配置中读取 integrationId）
    long? integrationId = null;
    if (action.Parameters != null && action.Parameters.TryGetValue("integrationId", out var intIdObj))
    {
        if (long.TryParse(intIdObj?.ToString(), out var parsedId))
            integrationId = parsedId;
    }
    string? integrationToken = integrationId.HasValue
        ? await GetIntegrationTokenAsync(integrationId.Value)
        : null;

    // 3. 构建 contextData (Dictionary)
    var contextData = new Dictionary<string, object>
    {
        ["OnboardingId"] = context.OnboardingId,
        ["StageId"] = context.StageId,
        ["ConditionId"] = context.ConditionId,
        ["TenantId"] = context.TenantId,
        ["ActionDefinitionId"] = action.ActionDefinitionId.Value,
        ["ActionName"] = actionDefinition.ActionName,
        ["TriggerSource"] = "StageCondition",
        ["CaseName"] = onboarding?.CaseName ?? "",
        ["CaseCode"] = onboarding?.CaseCode ?? "",
        ["WorkflowId"] = onboarding?.WorkflowId ?? 0
    };

    // 注入 StaticFieldValue (camelCase keys)
    foreach (var kvp in fieldData)
        contextData[kvp.Key] = kvp.Value;

    // 注入 Integration Token
    if (!string.IsNullOrEmpty(integrationToken))
        contextData["integrationToken"] = integrationToken;

    // 注入前一个 Action 的结果
    if (previousActionResult != null)
        contextData["previousActionResult"] = previousActionResult;

    // 执行 Action
    var executionResult = await _actionExecutionService.ExecuteActionAsync(
        action.ActionDefinitionId.Value, contextData, currentUserId);
    // ...
}
```

**新增辅助方法**：

- `GetStaticFieldValuesAsCamelCaseAsync(long onboardingId)` — 查询并转换 StaticFieldValue
- `ToCamelCase(string fieldName)` — "Company  State" → "companyState"
- `GetIntegrationTokenAsync(long integrationId)` — 通过 Integration ID 直接获取 OAuth2 Token

**ExecuteActionsAsync 链式传递**：

```csharp
JToken? previousActionResult = null;
foreach (var action in actions.OrderBy(a => a.Order))
{
    var actionResult = await ExecuteActionAsync(action, context, previousActionResult);
    result.Details.Add(actionResult);
    if (actionResult.Success && actionResult.ResultData.ContainsKey("executionResult"))
        previousActionResult = actionResult.ResultData["executionResult"] as JToken;
}
```

---

## actionsJson 数据结构变更

**变更前**：
```json
[{"type":"TriggerAction","order":0,"actionDefinitionId":"2004482114174717952","parameters":{}}]
```

**变更后**：
```json
[
  {"type":"TriggerAction","order":1,"integrationId":"1994239810054787072","actionDefinitionId":"{CRM_ACTION_ID}","parameters":{}},
  {"type":"TriggerAction","order":2,"integrationId":"1994239810054787072","actionDefinitionId":"{IAM_ACTION_ID}","parameters":{}}
]
```

`integrationId` 存储在 ConditionAction 顶层（与 `actionDefinitionId` 同级），后端从 actionsJson 反序列化时自动映射。

---

## 风险登记表

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| IAM 接口不支持 Customer 关联参数 | User 无法自动关联 Customer | 中 | 提前与 IAM 团队确认 |
| ToCamelCase 转换不匹配 | 占位符替换失败 | 低 | 单元测试覆盖所有 Field Name |
| Integration Token 获取失败 | Action 执行失败 | 中 | 日志记录 + 重试 |
| 后端 ConditionAction 反序列化不识别 integrationId | integrationId 丢失 | 低 | ConditionAction DTO 已有 Parameters 字段可兜底 |
