# Context: LSO Parcel CRM & IAM Integration Actions

## 业务目标

当 LSO Parcel 公司的 "LSO Customer Onboarding" Workflow 中的 Case 完成时，自动在 CRM 创建 Customer 并在 IAM 创建 External User，减少人工操作和数据不一致。

## 核心实体

- **Onboarding (Case)**: WFE 中的客户入职流程实例，包含客户信息（通过 Dynamic Fields / StaticFieldValue 存储）
- **ActionDefinition**: 定义 Action 的配置和元数据，类型为 HttpApi
- **ActionTriggerMapping**: 将 Action 映射到触发源（Stage Completed 事件）
- **Integration**: 外部系统连接配置（ID: 1994239810054787072），使用 OAuth2 认证
- **StaticFieldValue**: 存储 Case 的 Dynamic Fields 值，通过 field_name 标识

## 技术方案摘要

### 触发链路
```
Case Complete → CompleteCurrentStageAsync → EvaluateAndExecuteStageConditionAsync
  → TriggerAction (ConditionActionType=6)
  → ExecuteTriggerActionAsync (注入 StaticFieldValue + Integration Token)
  → ActionExecutionService.ExecuteActionAsync
  → HttpApiActionExecutor (ReplacePlaceholders → HTTP POST)
```

### 代码修改
1. **ActionExecutor.cs** (`ExecuteTriggerActionAsync`): 查询 StaticFieldValue 并以 camelCase key 注入 contextData；获取 Integration Token 注入 contextData；支持 Action 链式数据传递
2. **Action Config**: 使用 `{{placeholder}}` 引用 contextData 中的值

### Dynamic Fields 映射（已确认的完整列表）

| Field Name | Field ID | camelCase Key | 用途 |
|---|---|---|---|
| Company Name | 2042113396144476160 | companyName | CRM customerName |
| Tax ID | 2042113460464128000 | taxId | CRM taxID |
| Company Street | 2042113538813726720 | companyStreet | CRM corporateAddress |
| Company Unit | 2042115431929614336 | companyUnit | CRM 地址补充 |
| Company  City | 2042113657667719168 | companyCity | CRM corporateCity (⚠️双空格) |
| Company  State | 2042113698792869888 | companyState | CRM corporateState (⚠️双空格) |
| Company Postal Code | 2042113822914908160 | companyPostalCode | CRM corporateZipCode |
| Contact First Name | (截图确认) | contactFirstName | IAM firstName |
| Contact Last Name | (截图确认) | contactLastName | IAM lastName |
| Contact Email | 2004391300769648640 | contactEmail | CRM email, IAM email (isStatic) |
| Contact Phone | 2004391301629480960 | contactPhone | CRM phone, IAM contactNumber (isStatic) |
| Sales Name | (截图确认) | salesName | CRM Sales |
| Sales Email | (截图确认) | salesEmail | CRM Sales |
| Bill To Name | (截图确认) | billToName | CRM Billto Address |
| Bill To Street | (截图确认) | billToStreet | CRM Billto Address |
| Bill To Unit | (截图确认) | billToUnit | CRM Billto Address |
| Bill To City | (截图确认) | billToCity | CRM Billto Address |
| Bill To State | (截图确认) | billToState | CRM Billto Address |
| Bill To Postal Code | (截图确认) | billToPostalCode | CRM Billto Address |
| Payment Terms | (截图确认) | paymentTerms | CRM contractTerms |
| Username | (截图确认) | username | IAM userName |
| Password | 2042074506675228672 | password | IAM rawPassword (明文) |
| Company Code | 2042115478826127360 | companyCode | IAM companyCode |
| Verify Password | (截图确认) | - | 仅 UI 验证，不传 API |
| Create Account Notification | (截图确认) | - | 仅通知用，不传 API |

### API 端点
- CRM: `POST https://crm-dev.item.pub/crm/customers/v2/company-customers`
- IAM: `POST https://id-dev.item.pub/platform/v1/users`
- Token: 通过 Integration OAuth2 Client Credentials 获取

### 待确认项
- IAM `POST /platform/v1/users` 是否支持 Customer 关联参数（需 IAM 团队确认）
