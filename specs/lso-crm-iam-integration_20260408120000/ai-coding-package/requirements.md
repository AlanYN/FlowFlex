# Requirements: LSO Parcel CRM & IAM Integration Actions

<!-- 来源: requirements-analysis/requirements.md -->

## 用户故事

### US-001: Case Complete 触发 CRM 创建 Customer
**As a** LSO Parcel 运营人员
**I want** 当 LSO Customer Onboarding 的 Case 完成时，系统自动在 CRM 中创建一个 Customer
**So that** 无需手动在 CRM 中重复录入客户信息

**AC:**
- AC-001-1: Case 关联 Workflow 为 "LSO Customer Onboarding" 且 Stage Completed 时触发
- AC-001-2: WFE Dynamic Fields 按 camelCase 映射传递到 CRM API（完整映射见 design.md）
- AC-001-3: CRM API 调用成功后记录 Customer ID 到 ActionExecution 日志
- AC-001-4: CRM API 调用失败时 ActionExecution 状态为 Failed

### US-002: Case Complete 触发 IAM 创建 User
**As a** LSO Parcel 运营人员
**I want** 当 Case 完成时，系统自动在 IAM 中创建一个 External User
**So that** 客户可以使用创建的账号登录系统

**AC:**
- AC-002-1: IAM Action 在 CRM Action 之后执行（Order=2）
- AC-002-2: IAM User 参数从 Dynamic Fields 映射（userName, rawPassword, firstName, lastName 等）
- AC-002-3: IAM User 需关联 CRM Customer（待 IAM 团队确认接口支持）
- AC-002-4: IAM API 调用成功后记录 User ID
- AC-002-5: IAM API 调用失败时 ActionExecution 状态为 Failed

<!-- 来源: technical-design/requirements.md -->

## 技术需求

### TR-001: ExecuteTriggerActionAsync 注入 StaticFieldValue
- 查询 Onboarding 所有 StaticFieldValue，以 camelCase key 注入 contextData
- 确保 `{{placeholder}}` 正确替换

### TR-002: Integration Token 注入
- 通过 ActionDefinition → IntegrationAction → Integration 获取 OAuth2 Token
- 以 `integrationToken` key 注入 contextData

### TR-003: Action 链式数据传递
- ExecuteActionsAsync 循环中维护 previousActionResult
- 下一个 TriggerAction 的 contextData 中注入 previousActionResult

<!-- 来源: test-verification/requirements.md -->

## 测试覆盖范围
- 单元测试: ToCamelCase、StaticFieldValue 注入、Token 获取、链式传递
- 集成测试: CRM/IAM API 调用成功/失败场景
- 端到端测试: 完整 Case Complete 流程
