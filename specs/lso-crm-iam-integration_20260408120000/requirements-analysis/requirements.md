# Requirements: LSO Parcel CRM & IAM Integration Actions

## 需求概述

在 WFE（Workflow Engine）的 LSO Parcel 公司下，当 Case 选中 "LSO Customer Onboarding" Workflow 并点击 Case Complete 时，自动触发两个 Action：
1. 在 CRM 系统中创建一个 Customer 并自动 Approved
2. 在 IAM 系统中创建一个 External User 并关联 CRM Customer

## 需求类型

➕ 功能新增（基于已有 Action + Integration + Stage Condition 系统）

---

## 用户故事

### US-001: Case Complete 触发 CRM 创建 Customer

**As a** LSO Parcel 运营人员
**I want** 当 LSO Customer Onboarding 的 Case 完成时，系统自动在 CRM 中创建一个 Customer
**So that** 无需手动在 CRM 中重复录入客户信息，减少人工操作和数据不一致

**验收标准 (AC):**

- AC-001-1: 当 Case 关联的 Workflow 为 "LSO Customer Onboarding" 且 Case 状态变为 Completed 时，自动触发 CRM 创建 Customer 的 Action
- AC-001-2: WFE Case 字段按以下映射关系传递到 CRM API（Field Name 为 Dynamic Fields 中的实际名称）：

| WFE Dynamic Field Name | CRM API 字段名 | 占位符 | 说明 |
|---------|---------------|--------|------|
| Company Name | `customerName` | `{{companyName}}` | 公司名称 |
| Tax ID | `taxID` | `{{taxId}}` | 税号 |
| Company Street | `corporateAddress` + `registeredAddress` | `{{companyStreet}}` | 公司街道地址，同时写入 Corporate 和 Registered |
| Company  City | `corporateCity` + `registeredCity` | `{{companyCity}}` | ⚠️ Field Name 有双空格 |
| Company  State | `corporateState` + `registeredState` | `{{companyState}}` | ⚠️ Field Name 有双空格 |
| Company Postal Code | `corporateZipCode` + `registeredZipCode` | `{{companyPostalCode}}` | |
| Contact Email | `email` | `{{contactEmail}}` | 系统预定义字段 (isStatic=true) |
| Contact Phone | `phone` | `{{contactPhone}}` | 系统预定义字段 (isStatic=true) |
| Payment Terms | `contractTerms` | `{{paymentTerms}}` | 付款条款 |
| Contact First Name | (CRM Contact 页签) | `{{contactFirstName}}` | 待确认 CRM API 字段 |
| Contact Last Name | (CRM Contact 页签) | `{{contactLastName}}` | 待确认 CRM API 字段 |
| Sales Name | (CRM Account Holders) | `{{salesName}}` | 待确认 CRM API 字段 |
| Sales Email | (CRM Account Holders) | `{{salesEmail}}` | 待确认 CRM API 字段 |
| Bill To Name | (CRM Address Billto) | `{{billToName}}` | 待确认 CRM API 字段 |
| Bill To Street | (CRM Address Billto) | `{{billToStreet}}` | 待确认 CRM API 字段 |
| Bill To Unit | (CRM Address Billto) | `{{billToUnit}}` | 待确认 CRM API 字段 |
| Bill To City | (CRM Address Billto) | `{{billToCity}}` | 待确认 CRM API 字段 |
| Bill To State | (CRM Address Billto) | `{{billToState}}` | 待确认 CRM API 字段 |
| Bill To Postal Code | (CRM Address Billto) | `{{billToPostalCode}}` | 待确认 CRM API 字段 |

- AC-001-3: CRM API 调用成功后，返回的 Customer ID 应记录在 Action Execution 日志中
- AC-001-4: CRM API 调用失败时，Action Execution 状态标记为 Failed，记录错误信息

### US-002: Case Complete 触发 IAM 创建 User

**As a** LSO Parcel 运营人员
**I want** 当 LSO Customer Onboarding 的 Case 完成时，系统自动在 IAM 中创建一个 External User
**So that** 客户可以使用创建的账号登录系统

**验收标准 (AC):**

- AC-002-1: IAM 创建 User 的 Action 在 CRM 创建 Customer 之后执行（通过 ExecutionOrder 控制）
- AC-002-2: IAM User 创建参数映射（Field Name 为 Dynamic Fields 中的实际名称）：

| IAM API 参数 | 值来源 | WFE Dynamic Field Name | 占位符 | 说明 |
|------|-------|---|---|------|
| userType | 固定值: 0 | - | - | External 类型用户 |
| companyCode | WFE Dynamic Field | Company Code | `{{companyCode}}` | 待确认是否已创建此字段 |
| userName | WFE Dynamic Field | Username | `{{username}}` | IAM 登录用户名 |
| firstName | WFE Dynamic Field | Contact First Name | `{{contactFirstName}}` | |
| lastName | WFE Dynamic Field | Contact Last Name | `{{contactLastName}}` | |
| rawPassword | WFE Dynamic Field | Password | `{{password}}` | 明文密码，MaxLength=100 |
| email | WFE Dynamic Field | Contact Email | `{{contactEmail}}` | 系统预定义字段 |
| contactNumber | WFE Dynamic Field | Contact Phone | `{{contactPhone}}` | 系统预定义字段 |
| userTags | 固定值: ["Admin"] | - | - | 用户标签 |
| roles | 固定值: ["Admin"] | - | - | 用户角色 |

- AC-002-3: IAM User 需要关联上 CRM 创建的 Customer Account（需要 CRM 创建成功后的 Customer ID）
- AC-002-4: IAM API 调用成功后，返回的 User ID 应记录在 Action Execution 日志中
- AC-002-5: IAM API 调用失败时，Action Execution 状态标记为 Failed，记录错误信息

### US-003: Workflow 和 Stage Condition 配置

**As a** 系统管理员
**I want** 创建一个 Workflow，配置 Stage Condition 在最后一个 Stage 完成时触发 Action
**So that** Case Complete 时自动执行 CRM 和 IAM 的集成操作

**验收标准 (AC):**

- AC-003-1: 创建一个新的 Workflow（或使用已有的 "LSO Customer Onboarding" Workflow）
- AC-003-2: 在 Workflow 的最后一个 Stage 上配置 Stage Condition，当 Stage Completed 时触发 TriggerAction
- AC-003-3: 配置两个 TriggerAction，分别关联 CRM 创建 Customer 和 IAM 创建 User 的 ActionDefinition
- AC-003-4: CRM Action 的 ExecutionOrder = 1，IAM Action 的 ExecutionOrder = 2（确保 CRM 先执行）
- AC-003-5: 两个 Action 都关联到 Integration ID `1994239810054787072`

---

## 技术约束

1. **Action 类型**: HttpApi（通过 HttpApiActionExecutor 执行 HTTP 请求）
2. **触发机制**: Stage Condition → TriggerAction → ActionDefinition → HttpApiActionExecutor
3. **字段来源**: WFE Case 的 StaticFieldValue（Dynamic Fields）中存储的值，在 TriggerAction 触发时查询所有 StaticFieldValue 并注入到 contextData，通过 `{{placeholder}}` 占位符在 Action Config 的 Body 中引用
4. **认证方式**: 通过 Integration（ID: `1994239810054787072`）的 OAuth2 Client Credentials 获取 Token
5. **Integration 关联**: 两个 Action 都关联到已有的 Integration（ID: 1994239810054787072）
6. **执行顺序**: CRM Action 先执行（Order=1），IAM Action 后执行（Order=2）。IAM Action 需要使用 CRM 创建返回的 Customer ID
7. **CRM API 端点**: `https://crm-dev.item.pub/crm/customers/v2/company-customers`（POST）
8. **IAM API 端点**: `https://id-dev.item.pub/platform/v1/users`（POST）
9. **代码修改**: 需要修改 `ExecuteTriggerActionAsync` 方法，在构建 contextData 时查询 StaticFieldValue 并注入
10. **Action 链式数据传递**: 需要支持 Action 1（CRM）的返回结果传递给 Action 2（IAM），以便 IAM 创建 User 时关联 CRM Customer

## 已澄清问题

| # | 问题 | 结论 |
|---|------|------|
| 1 | 字段占位符来源 | ✅ 在触发时查询该 Case 的所有 StaticFieldValue 并注入到 contextData 中 |
| 2 | Action 间数据传递 | ✅ 先创建 CRM Customer，再创建 IAM User。IAM 创建 User 时需要关联 CRM Customer（IAM 接口支持传 customer 参数，需确认 IAM 接口是否已支持，不支持则需 IAM 团队加上） |
| 3 | Token 管理 | ✅ 通过 Integration 的 `test-connection` API 获取 Token（Integration ID: `1994239810054787072`，使用 OAuth2 Client Credentials） |
| 4 | WFE 字段名称 | ✅ 这些是自定义字段（Dynamic Fields），可以在 Stage 上添加 |
| 5 | Company Code | ✅ 通过自定义字段（Dynamic Fields）填写 |
| 6 | Username/Password 等字段 | ✅ 通过 Stage 选择，填写的自定义字段（Dynamic Fields / StaticFieldValue） |

## 待确认问题（需 IAM 团队确认）

1. **IAM 创建 User 接口是否支持传 Customer 关联参数**: 根据 Lina Gao 的评论，IAM 页面上 External 类型用户可以配置 Customer 关联，但需要确认 IAM 的 `POST /platform/v1/users` 接口是否支持传递 Customer 关联参数。如不支持，需要 IAM 团队添加。
