# Tasks: LSO Parcel CRM & IAM Integration Actions

<!-- 来源: technical-design/tasks.md + test-verification/tasks.md -->

## 执行顺序 Checklist

### 第一组：类型定义与 API

- [ ] **TD-001** `src/types/condition.d.ts` — ConditionAction 增加 `integrationId?: string`
- [ ] **TD-002** `Application.Contracts/Dtos/OW/StageCondition/ConditionAction.cs` — 增加 `IntegrationId` 属性
- [ ] **TD-003** `src/app/apis/ow/index.ts` — 新增 `getActiveIntegrations()` 和 `getActionsByIntegration(id)`

### 第二组：后端核心代码修改

- [ ] **TD-004** `ActionExecutor.cs` — 新增 `ToCamelCase` 辅助方法
- [ ] **TD-005** `ActionExecutor.cs` — 新增 `GetStaticFieldValuesAsCamelCaseAsync` 方法
- [ ] **TD-006** `ActionExecutor.cs` — 新增 `GetIntegrationTokenAsync` 方法
- [ ] **TD-007** `ActionExecutor.cs` — 修改 `ExecuteTriggerActionAsync` 注入 StaticFieldValue + Token + previousActionResult
- [ ] **TD-008** `ActionExecutor.cs` — 修改 `ExecuteActionsAsync` 支持链式数据传递
- [ ] **TD-009** `ActionExecutor.cs` — 新增依赖注入 `IHttpClientFactory` + `IEncryptionService`

### 第三组：前端 UI 修改

- [ ] **TD-010** `ConditionActionForm.vue` — TriggerAction 增加 Integration 下拉框
- [ ] **TD-011** `ConditionActionForm.vue` — Integration 选择后过滤 Action 列表
- [ ] **TD-012** `ConditionActionForm.vue` — 加载 Integration 列表 (onMounted)

### 第四组：创建 ActionDefinition 和配置

- [ ] **TD-013** 创建 "LSO CRM Create Customer" ActionDefinition (POST /action/v1/definitions)
- [ ] **TD-014** 创建 "LSO IAM Create User" ActionDefinition (POST /action/v1/definitions)
- [ ] **TD-015** 创建 IntegrationAction 关联记录 (ff_integration_action)
- [ ] **TD-016** 更新 Stage Condition actionsJson 含 integrationId (PUT /ow/stage-conditions/v1/2042118279430017024)

### 第五组：测试

- [ ] **TV-001** ToCamelCase 单元测试
- [ ] **TV-002** StaticFieldValue 注入单元测试
- [ ] **TV-003** Integration Token 获取单元测试
- [ ] **TV-004** 链式传递单元测试
- [ ] **TV-005** 前端 Integration 下拉框交互测试
- [ ] **TD-017** 端到端测试（Case Complete → CRM + IAM）
