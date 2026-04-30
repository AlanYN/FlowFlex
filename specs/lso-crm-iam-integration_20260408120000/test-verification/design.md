# Test Design: LSO Parcel CRM & IAM Integration Actions

## 测试用例

### TC-001: ToCamelCase 基本转换

| 输入 | 期望输出 | 说明 |
|------|---------|------|
| `"Company Name"` | `"companyName"` | 标准双词 |
| `"Tax ID"` | `"taxId"` | 全大写词 |
| `"Contact First Name"` | `"contactFirstName"` | 三词 |
| `"Company  State"` | `"companyState"` | ⚠️ 双空格 |
| `"Company  City"` | `"companyCity"` | ⚠️ 双空格 |
| `"Company Postal Code"` | `"companyPostalCode"` | 三词 |
| `"Password"` | `"password"` | 单词 |
| `"Username"` | `"username"` | 单词 |
| `"Bill To Name"` | `"billToName"` | 三词 |
| `"Bill To Postal Code"` | `"billToPostalCode"` | 四词 |
| `"Contact Email"` | `"contactEmail"` | 系统预定义字段 |
| `"Contact Phone"` | `"contactPhone"` | 系统预定义字段 |
| `"Company Code"` | `"companyCode"` | |
| `""` | `""` | 空字符串 |
| `null` | `null` | null |

### TC-002: GetStaticFieldValuesAsCamelCaseAsync

**TC-002-1: 正常场景 — 多个字段值**
- 前置: Onboarding 有 5 个 StaticFieldValue（Company Name, Tax ID, Password, Contact Email, Company  State）
- 操作: 调用 GetStaticFieldValuesAsCamelCaseAsync(onboardingId)
- 期望: 返回 Dictionary 包含 5 个 camelCase key，值正确解析

**TC-002-2: FieldValueJson 为单元素数组**
- 前置: FieldValueJson = `["1935628523372941312"]`
- 期望: 解析为字符串 `"1935628523372941312"`

**TC-002-3: FieldValueJson 为普通字符串**
- 前置: FieldValueJson = `"Acme Corp"`
- 期望: 解析为字符串 `"Acme Corp"`

**TC-002-4: FieldValueJson 为空**
- 前置: FieldValueJson = `""`
- 期望: 该字段不注入到 Dictionary 中

**TC-002-5: Onboarding 无 StaticFieldValue**
- 前置: Onboarding 没有任何 StaticFieldValue
- 期望: 返回空 Dictionary

### TC-003: GetIntegrationTokenForActionAsync

**TC-003-1: 正常场景 — OAuth2 Token 获取成功**
- 前置: ActionDefinition 关联 IntegrationAction → Integration（OAuth2, 有效 credentials）
- 期望: 返回有效的 access_token 字符串

**TC-003-2: 无 IntegrationAction 关联**
- 前置: ActionDefinition 没有关联的 IntegrationAction
- 期望: 返回 null，不抛异常

**TC-003-3: Integration 非 OAuth2 认证**
- 前置: Integration 的 AuthMethod 不是 OAuth2
- 期望: 返回 null

**TC-003-4: Token 端点返回错误**
- 前置: OAuth2 端点返回 401
- 期望: 返回 null，记录 Warning 日志

### TC-004: ExecuteTriggerActionAsync 注入完整性

**TC-004-1: contextData 包含 StaticFieldValue**
- 前置: Onboarding 有 StaticFieldValue（Company Name = "Acme Corp"）
- 操作: 触发 TriggerAction
- 期望: contextData 中包含 `companyName: "Acme Corp"`

**TC-004-2: contextData 包含 integrationToken**
- 前置: ActionDefinition 关联有效的 Integration
- 操作: 触发 TriggerAction
- 期望: contextData 中包含 `integrationToken: "eyJhbG..."`

**TC-004-3: contextData 包含 previousActionResult**
- 前置: 前一个 Action 执行成功，返回 `{customerId: 123}`
- 操作: 触发第二个 TriggerAction
- 期望: contextData 中包含 `previousActionResult: {customerId: 123}`

**TC-004-4: StaticFieldValue 查询失败不阻塞**
- 前置: StaticFieldValue 查询抛异常
- 期望: contextData 仍包含基本字段，Action 继续执行

### TC-005: ExecuteActionsAsync 链式传递

**TC-005-1: 两个 TriggerAction 顺序执行**
- 前置: actionsJson 包含两个 TriggerAction（Order=1, Order=2）
- 期望: Order=1 先执行，Order=2 后执行

**TC-005-2: 第一个 Action 失败不阻塞第二个**
- 前置: 第一个 TriggerAction 执行失败
- 期望: 第二个 TriggerAction 仍然执行，previousActionResult 为 null

**TC-005-3: 链式传递数据正确**
- 前置: 第一个 Action 返回 `{success: true, response: "{\"id\": 123}"}`
- 期望: 第二个 Action 的 contextData 中 previousActionResult 包含该结果

### TC-006: 端到端 — CRM Create Customer

**TC-006-1: CRM API 调用成功**
- 前置: 所有 Dynamic Fields 已填写，Integration Token 有效
- 操作: Complete Stage 触发 Stage Condition
- 期望: CRM API 收到正确的 JSON body，返回 201，ActionExecution 状态为 Completed

**TC-006-2: CRM API 调用失败（401 Token 过期）**
- 前置: Integration Token 无效
- 期望: ActionExecution 状态为 Failed，错误信息包含 HTTP 401

### TC-007: 端到端 — IAM Create User

**TC-007-1: IAM API 调用成功**
- 前置: 所有 Dynamic Fields 已填写，Integration Token 有效
- 操作: CRM Action 执行后，IAM Action 执行
- 期望: IAM API 收到正确的 JSON body，返回 201，ActionExecution 状态为 Completed

**TC-007-2: IAM API 调用失败（400 参数错误）**
- 前置: Username 字段为空
- 期望: ActionExecution 状态为 Failed，错误信息包含 HTTP 400

## 问题清单

| # | 问题 | 严重度 | 状态 |
|---|------|--------|------|
| ISS-001 | IAM 接口是否支持 Customer 关联参数 | 🟡 中 | ⏳ 待 IAM 团队确认 |
| ISS-002 | CRM API 返回的 Customer ID 字段名是什么（id? customerId?） | 🟢 低 | ⏳ 待测试确认 |
| ISS-003 | Integration Token 有效期是否足够覆盖两个 Action 的执行时间 | 🟢 低 | ⏳ 待测试确认 |
