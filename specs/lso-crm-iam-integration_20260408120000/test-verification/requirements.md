# Test Requirements: LSO Parcel CRM & IAM Integration Actions

## 测试覆盖范围

### 单元测试
- ToCamelCase 方法：覆盖所有已知 Field Name 的转换
- GetStaticFieldValuesAsCamelCaseAsync：StaticFieldValue 查询和转换逻辑
- GetIntegrationTokenForActionAsync：Token 获取逻辑
- ExecuteTriggerActionAsync：contextData 注入完整性
- ExecuteActionsAsync：链式数据传递

### 集成测试
- Stage Condition 触发 → TriggerAction 执行 → HttpApi 调用外部 API
- CRM API 调用成功/失败场景
- IAM API 调用成功/失败场景
- Action 链式传递（CRM 结果 → IAM）

### 端到端测试
- 完整 Case Complete 流程：填写 Dynamic Fields → Complete Stage → 触发 Action → CRM + IAM 创建成功

## 质量目标
- 单元测试覆盖率 ≥ 80%（针对修改的代码）
- 所有已知 Field Name 的 camelCase 转换有测试覆盖
- CRM/IAM API 调用失败不阻塞流程
