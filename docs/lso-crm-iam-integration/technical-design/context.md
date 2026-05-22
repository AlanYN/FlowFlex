# Context: LSO Parcel CRM & IAM Integration — Technical Design

## 技术栈
- 后端: C# / ASP.NET Core / SqlSugar ORM
- 数据库: PostgreSQL
- 外部 API: CRM (crm-dev.item.pub), IAM (id-dev.item.pub)
- 认证: OAuth2 Client Credentials (Integration ID: 1994239810054787072)

## 架构决策
- 仅修改 `ActionExecutor.cs` 一个文件，扩展 `ExecuteTriggerActionAsync` 方法
- StaticFieldValue 以 camelCase key 注入 contextData，复用 `HttpApiActionExecutor.ReplacePlaceholders()` 的 `\{\{(\w+)\}\}` 正则
- Integration Token 通过 ActionDefinition → IntegrationAction → Integration 链路获取
- Action 链式传递通过 `ExecuteActionsAsync` 循环中维护 `previousActionResult` 实现
- 新增两个 ActionDefinition（CRM + IAM），通过 API 创建，不需要数据库迁移

## 修改文件
| 文件 | 修改内容 |
|------|---------|
| `Application/Services/OW/StageCondition/ActionExecutor.cs` | 扩展 ExecuteTriggerActionAsync + ExecuteActionsAsync，新增 3 个辅助方法 |

## 新增依赖注入
| 依赖 | 用途 |
|------|------|
| `IHttpClientFactory` | 获取 Integration OAuth2 Token |
| `IEncryptionService` | 解密 Integration Credentials |

## API 端点
- CRM: `POST https://crm-dev.item.pub/crm/customers/v2/company-customers`
- IAM: `POST https://id-dev.item.pub/platform/v1/users`

## 已有配置 ID
| 实体 | ID |
|------|-----|
| Workflow | 2042115982671089664 |
| Stage | 2042116465028632576 |
| Stage Condition | 2042118279430017024 |
| Integration | 1994239810054787072 |
