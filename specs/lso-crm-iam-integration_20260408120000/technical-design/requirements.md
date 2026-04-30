# Technical Requirements: LSO Parcel CRM & IAM Integration Actions

## 技术需求

### TR-001: ExecuteTriggerActionAsync 注入 StaticFieldValue
- 在 `ExecuteTriggerActionAsync` 中查询 Onboarding 的所有 StaticFieldValue
- 将 Field Name 转换为 camelCase 后注入 contextData
- 确保 `HttpApiActionExecutor.ReplacePlaceholders()` 能正确替换 `{{placeholder}}`

### TR-002: Integration Token 注入
- 通过 ActionDefinition → IntegrationAction → Integration 链路获取 OAuth2 Token
- 将 Token 以 `integrationToken` key 注入 contextData
- Action Config Headers 中使用 `Bearer {{integrationToken}}`

### TR-003: Action 链式数据传递
- 在 `ExecuteActionsAsync` 循环中维护 `previousActionResult`
- 每个 TriggerAction 执行完成后保存其 ExecutionOutput
- 下一个 TriggerAction 的 contextData 中注入 `previousActionResult`

## 验收标准

- AC-TR-001: 当 Stage Condition 触发 TriggerAction 时，contextData 中包含所有 StaticFieldValue 的 camelCase key-value
- AC-TR-002: contextData 中包含有效的 `integrationToken`
- AC-TR-003: 第二个 TriggerAction 的 contextData 中包含第一个 Action 的执行结果
- AC-TR-004: "Company  State"（双空格）正确转换为 `companyState`
- AC-TR-005: Action 执行失败不阻塞后续 Action 的执行（已有逻辑）
