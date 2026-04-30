# Tasks: LSO Parcel CRM & IAM Integration Actions

## 任务概览

| Task ID | 任务名称 | 优先级 | 依赖 | 状态 |
|---------|---------|--------|------|------|
| T-001 | 修改 ExecuteTriggerActionAsync 注入 StaticFieldValue | P0 | - | ⏳ pending |
| T-002 | 实现 Integration Token 注入到 contextData | P0 | - | ⏳ pending |
| T-003 | 实现 Action 链式数据传递（前一个 Action 结果传递给后一个） | P0 | - | ⏳ pending |
| T-004 | 创建 CRM Create Customer ActionDefinition | P1 | T-001, T-002 | ⏳ pending |
| T-005 | 创建 IAM Create User ActionDefinition | P1 | T-001, T-002, T-003 | ⏳ pending |
| T-006 | 创建 Workflow 和 Stage 配置 | P1 | - | ⏳ pending |
| T-007 | 在 Stage 上配置 Dynamic Fields（自定义字段） | P1 | T-006 | ⏳ pending |
| T-008 | 配置 Stage Condition（TriggerAction） | P1 | T-004, T-005, T-006 | ⏳ pending |
| T-009 | 创建 IntegrationAction 关联记录 | P1 | T-004, T-005 | ⏳ pending |
| T-010 | 端到端测试 | P2 | T-001 ~ T-009 | ⏳ pending |

---

## 详细任务描述

### T-001: 修改 ExecuteTriggerActionAsync 注入 StaticFieldValue

**文件**: `packages/flowFlex-backend/Application/Services/OW/StageCondition/ActionExecutor.cs`

**操作步骤**:
1. 在 `ExecuteTriggerActionAsync` 方法中，注入 `IStaticFieldValueRepository` 依赖
2. 在构建 contextData 之前，调用 `GetByOnboardingIdAsync(context.OnboardingId)` 获取所有 StaticFieldValue
3. 遍历 StaticFieldValue 列表，解析 `FieldValueJson`，以 `FieldName` 为 key 构建字典
4. 将字典合并到 contextData 中
5. 确保 `HttpApiActionExecutor.ReplacePlaceholders()` 能正确从 contextData 中提取这些值

### T-002: 实现 Integration Token 注入到 contextData

**文件**: `packages/flowFlex-backend/Application/Services/OW/StageCondition/ActionExecutor.cs`

**操作步骤**:
1. 在 `ExecuteTriggerActionAsync` 方法中，注入 `IIntegrationRepository` 和 `IExternalIntegrationService`（或 `IHttpClientFactory` + `IEncryptionService`）
2. 从 ActionDefinition 的 `ActionConfig` 中提取 Integration ID（或从 `IntegrationAction` 表查询）
3. 调用 `GetIntegrationAccessTokenAsync()` 获取 OAuth2 Token
4. 将 Token 以 `integrationToken` 为 key 注入到 contextData 中
5. Action Config 的 Headers 中使用 `Bearer {{integrationToken}}` 引用

### T-003: 实现 Action 链式数据传递

**文件**: `packages/flowFlex-backend/Application/Services/OW/StageCondition/ActionExecutor.cs`

**操作步骤**:
1. 在执行多个 TriggerAction 的循环中，维护一个 `previousActionResults` 字典
2. 每个 Action 执行完成后，将其 `ExecutionOutput` 存入 `previousActionResults`
3. 下一个 Action 的 contextData 中注入 `previousActionResult` 字段
4. IAM Action Config 中可以通过 `{{previousActionResult}}` 引用 CRM 返回的数据

### T-004: 创建 CRM Create Customer ActionDefinition

**操作步骤**:
1. 通过 WFE API `POST /action/v1/definitions` 创建 ActionDefinition
2. action_code: `LSO-CRM-CREATE-CUSTOMER`
3. action_name: `LSO CRM Create Customer`
4. action_type: `HttpApi`
5. trigger_type: `Integration`
6. action_config: CRM API 的 URL、Headers、Body 模板（含 `{{placeholder}}`）
7. 关联到 Integration ID `1994239810054787072`

### T-005: 创建 IAM Create User ActionDefinition

**操作步骤**:
1. 通过 WFE API `POST /action/v1/definitions` 创建 ActionDefinition
2. action_code: `LSO-IAM-CREATE-USER`
3. action_name: `LSO IAM Create User`
4. action_type: `HttpApi`
5. trigger_type: `Integration`
6. action_config: IAM API 的 URL、Headers、Body 模板（含 `{{placeholder}}`）
7. 关联到 Integration ID `1994239810054787072`
8. 待 IAM 团队确认 Customer 关联参数后，补充到 Body 模板中

### T-006: 创建 Workflow 和 Stage 配置

**操作步骤**:
1. 在 LSO Parcel 公司下创建新的 Workflow（或使用已有的 "LSO Customer Onboarding"）
2. 配置 Stage 结构（至少包含一个 Stage 用于收集客户信息）
3. 确保最后一个 Stage 支持 Stage Condition

### T-007: 在 Stage 上配置 Dynamic Fields

**状态**: ✅ 已部分完成（截图显示已创建 15/46 个字段）

**已确认的 Dynamic Fields**（来自截图）：
- `Contact First Name` — Contact - First Name (Single-line Text)
- `Contact Last Name` — Contact - Last Name (Single-line Text)
- `Sales Name` — Sales Name (Single-line Text)
- `Sales Email` — Sales Email (Email)
- `Bill To Name` — Bill To Name (Single-line Text)
- `Bill To Street` — Bill To Street (Single-line Text)
- `Bill To Unit` — Bill To Unit (Single-line Text)
- `Bill To City` — Bill To City (Single-line Text)
- `Bill To State` — Bill To State (Single-line Text)
- `Bill To Postal Code` — Bill To Postal Code (Single-line Text)
- `Payment Terms` — Payment Terms (Single-line Text)
- `Username` — Username (Single-line Text)
- `Password` — Password (Single-line Text, Max Length 100)
- `Verify Password` — Verify Password (Single-line Text)
- `Create Account Notification` — Email address to receive account notifications

**待确认的 Dynamic Fields**（截图未显示，需确认是否已创建）：
- `Company Name`
- `Tax ID / EIN`（或 `Tax ID`）
- `Company Street`
- `Company Unit`
- `Company City`
- `Company State`
- `Company Postal Code`
- `Contact Email`
- `Contact Phone Number`
- `Company Code`（IAM companyCode）

> **注意**: Field Name 在代码中会被转换为 camelCase 作为占位符 key。
> 例如 `Contact First Name` → `{{contactFirstName}}`

### T-008: 配置 Stage Condition（TriggerAction）

**操作步骤**:
1. 在最后一个 Stage 上创建 Stage Condition
2. 配置 Condition 规则：当 Stage 状态变为 Completed 时触发
3. 配置两个 TriggerAction：
   - TriggerAction 1: `actionDefinitionId` = CRM Action ID, `order` = 1
   - TriggerAction 2: `actionDefinitionId` = IAM Action ID, `order` = 2

### T-009: 创建 IntegrationAction 关联记录

**操作步骤**:
1. 在 `ff_integration_action` 表中创建两条记录
2. 关联 CRM ActionDefinition 到 Integration `1994239810054787072`
3. 关联 IAM ActionDefinition 到 Integration `1994239810054787072`

### T-010: 端到端测试

**操作步骤**:
1. 创建测试 Case，关联 LSO Customer Onboarding Workflow
2. 填写所有 Dynamic Fields（StaticFieldValue）
3. 逐步完成所有 Stage
4. 在最后一个 Stage 完成时，验证：
   - CRM Customer 创建成功（检查 CRM 系统）
   - IAM User 创建成功（检查 IAM 系统）
   - Action Execution 日志记录正确（检查 `ff_action_executions` 表）
   - Token 获取正常
   - 字段映射正确
