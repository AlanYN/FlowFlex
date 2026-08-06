# WFE (FlowFlex) 外部系统对接指南

> 本文档面向需要与 WFE 集成的外部系统开发者（如 Ticket System），说明如何通过 WFE External Integration API 创建和管理 Workflow Case。
>
> 相关 JIRA: CSR-3299 (Ticket+WFE)

---

## 一、对接前准备

### 1.1 WFE 后台配置（由 WFE 管理员完成）

在 WFE 后台管理页面创建以下配置：

| 步骤 | 操作                     | 说明                                                                                                            |
| ---- | ------------------------ | --------------------------------------------------------------------------------------------------------------- |
| 1    | 创建 Integration         | 设定 `systemName`（如 `"Ticket System"`），获得 Integration ID                                                  |
| 2    | 创建 Entity Type Mapping | 关联 Integration，设定 `externalEntityName`（如 `"Ticket-Contract"`），选择可用的 Workflow，生成唯一 `systemId` |
| 3    | 配置 Workflow            | 确保关联的 Workflow 至少有一个 Stage                                                                            |

### 1.2 认证凭据（由 IAM 管理员提供）

| 信息               | 说明                                                  |
| ------------------ | ----------------------------------------------------- |
| IAM Token Endpoint | `https://id-staging.item.com/oauth2/token`（staging） |
| Client ID          | 分配给 Ticket System 的客户端 ID                      |
| Client Secret      | 对应的密钥                                            |
| Tenant ID          | 租户 ID（如 `1000`、`1401`）                          |

---

## 二、认证方式

使用 **OAuth2 Client Credentials** 获取 Bearer Token：

```bash
curl -X POST "https://id-staging.item.com/oauth2/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id={CLIENT_ID}&client_secret={CLIENT_SECRET}"
```

**响应：**

```json
{
  "access_token": "eyJraWQi...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

---

## 三、必要请求头

每个请求必须携带以下 Header：

| Header          | 必填          | 说明                       |
| --------------- | ------------- | -------------------------- |
| `Authorization` | 是            | `Bearer {access_token}`    |
| `X-Tenant-Id`   | 是            | 租户 ID，决定数据隔离      |
| `X-App-Code`    | 否            | 应用标识，默认 `"default"` |
| `Content-Type`  | POST 请求必填 | `application/json`         |

---

## 四、API 端点详情

**Base URL:**

- Staging: `https://workflow-staging.item.com`
- Production: `https://workflow.item.com`

---

### 4.1 获取 Entity Type Mappings（获取 SystemId）

首次对接或初始化时调用，获取你的 `systemId`。

```
GET /api/integration/external/v1/entity-type-mappings?systemName={systemName}
```

**curl 示例：**

```bash
curl -X GET "https://workflow-staging.item.com/api/integration/external/v1/entity-type-mappings?systemName=Ticket%20System" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000"
```

**响应：**

```json
{
  "data": {
    "integrationId": "2009088829700575232",
    "integrationName": "Ticket System",
    "systemName": "Ticket System",
    "entityTypeMappings": [
      {
        "id": "2015718347106291712",
        "systemId": "ABC123DEF456",
        "externalEntityName": "Ticket-Contract",
        "externalEntityType": "Ticket-Contract",
        "wfeEntityType": "case",
        "workflowIds": [1845409245046509568],
        "isActive": true
      }
    ]
  },
  "success": true,
  "msg": "",
  "code": "200"
}
```

**关键字段：**

- `systemId` — 后续所有 API 调用的核心标识，务必缓存
- `workflowIds` — 该 mapping 下允许创建 case 的工作流 ID 列表

---

### 4.2 获取可用 Workflow 列表

```
GET /api/integration/external/v1/workflows?systemId={systemId}
```

**curl 示例：**

```bash
curl -X GET "https://workflow-staging.item.com/api/integration/external/v1/workflows?systemId=ABC123DEF456" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000"
```

**响应：**

```json
{
  "data": [
    {
      "id": "1845409245046509568",
      "name": "Contract Approval Workflow",
      "description": "合同审批工作流",
      "isDefault": false
    }
  ],
  "success": true,
  "msg": "",
  "code": "200"
}
```

---

### 4.3 创建 Case（核心接口）

```
POST /api/integration/external/v1/cases
```

**请求体：**

| 字段           | 类型   | 必填   | 最大长度 | 说明                                           |
| -------------- | ------ | ------ | -------- | ---------------------------------------------- |
| `systemId`     | string | **是** | 100      | 从 entity-type-mappings 获取                   |
| `workflowId`   | long   | **是** | —        | 从 workflows 列表中选择                        |
| `entityType`   | string | **是** | 100      | 实体类型，如 `"Contract"`                      |
| `entityId`     | string | **是** | 100      | 外部系统的实体 ID（如 Ticket ID）              |
| `caseName`     | string | **是** | 200      | Case 名称（重复时自动加后缀 -2, -3...）        |
| `contactName`  | string | 否     | 200      | 联系人姓名                                     |
| `contactEmail` | string | 否     | 200      | 联系人邮箱                                     |
| `contactPhone` | string | 否     | 50       | 联系人电话（**必须是纯字符串**）               |
| `createdBy`    | string | 否     | 200      | 创建者姓名（WFE 会查找对应用户设置 Ownership） |

> **注意：** 所有字段值必须是字符串或数字，不要传对象/数组，否则 WFE 会返回 500。

**curl 示例：**

```bash
curl -X POST "https://workflow-staging.item.com/api/integration/external/v1/cases" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000" \
  -H "Content-Type: application/json" \
  -d '{
    "systemId": "ABC123DEF456",
    "workflowId": 1845409245046509568,
    "entityType": "Contract",
    "entityId": "TKT-456",
    "caseName": "Contract Review - ABC Logistics",
    "contactName": "John Doe",
    "contactEmail": "john@example.com",
    "contactPhone": "+1 234 567 890",
    "createdBy": "System Auto"
  }'
```

**成功响应（HTTP 201）：**

```json
{
  "data": {
    "caseId": "2074806176842911744",
    "caseCode": "C00119",
    "caseName": "Contract Review - ABC Logistics",
    "workflowId": "1845409245046509568",
    "workflowName": "Contract Approval Workflow",
    "currentStageId": "1845411139018035200",
    "currentStageName": "Initial Review",
    "status": "Started",
    "createdBy": "System Auto",
    "createdAt": "2026-07-20T10:00:00.000+00:00"
  },
  "success": true,
  "msg": "",
  "code": "200"
}
```

**关键返回字段：**

- `caseId` — WFE Case 唯一标识，**Ticket 系统需保存此 ID 用于后续关联**
- `currentStageName` — 当前所处阶段
- `status` — Case 状态

---

### 4.4 查询 Case 列表（按外部实体）

```
GET /api/ow/onboardings/v1/by-system?systemId={systemId}&entityId={entityId}&sortField=modifyDate&sortOrder=desc&pageIndex=1&pageSize=10
```

**curl 示例：**

```bash
curl -X GET "https://workflow-staging.item.com/api/ow/onboardings/v1/by-system?systemId=ABC123DEF456&entityId=TKT-456&sortField=modifyDate&sortOrder=desc&pageIndex=1&pageSize=10" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000"
```

**响应：**

```json
{
  "data": {
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 1,
    "items": [
      {
        "caseId": "2074806176842911744",
        "caseName": "Contract Review - ABC Logistics",
        "workflowName": "Contract Approval Workflow",
        "currentStageName": "Initial Review",
        "status": "InProgress",
        "stageAssignee": "Amanda Chen",
        "modifyDate": "2026-07-20T10:30:00+00:00"
      }
    ]
  },
  "success": true,
  "msg": "",
  "code": "200"
}
```

---

### 4.5 获取 Case 附件

```
GET /api/integration/external/v1/inbound-attachments?systemId={systemId}&entityId={entityId}
```

**curl 示例：**

```bash
curl -X GET "https://workflow-staging.item.com/api/integration/external/v1/inbound-attachments?systemId=ABC123DEF456&entityId=TKT-456" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000"
```

---

### 4.6 重试字段映射

如果创建 Case 时字段映射失败，可以重试：

```
POST /api/integration/external/v1/cases/{caseId}/retry-field-mapping
```

---

## 五、Case 状态流转

| 状态            | 说明           | 是否终态 |
| --------------- | -------------- | -------- |
| Started         | 初始状态       | 否       |
| InProgress      | 进行中         | 否       |
| Completed       | 已完成         | 是       |
| Paused          | 已暂停         | 否       |
| Aborted         | 已终止         | 是       |
| Cancelled       | 已取消         | 是       |
| Rejected        | 已拒绝         | 是       |
| Force Completed | 管理员强制完成 | 是       |

---

## 六、错误处理

### 6.1 常见错误

| HTTP 状态码 | 错误信息                                                  | 原因                                         |
| ----------- | --------------------------------------------------------- | -------------------------------------------- |
| 400         | "System ID is required"                                   | systemId 为空                                |
| 400         | "Entity Type is required"                                 | entityType 为空                              |
| 400         | "Entity ID is required"                                   | entityId 为空                                |
| 400         | "Case Name is required"                                   | caseName 为空                                |
| 404         | "Entity mapping not found for System ID 'xxx'"            | systemId 无效或对应 mapping 已删除           |
| 404         | "Workflow not found"                                      | workflowId 对应的工作流不存在或已删除        |
| 400         | "Workflow {id} is not configured for this entity mapping" | 该工作流未被配置到当前 entity mapping 中     |
| 400         | "Workflow has no stages configured"                       | 工作流没有配置任何阶段                       |
| 401         | Unauthorized                                              | Token 无效或过期                             |
| 500         | Internal Server Error                                     | 请求体字段类型不正确（如传了对象而非字符串） |

### 6.2 错误响应格式

```json
{
  "data": null,
  "success": false,
  "msg": "具体错误信息",
  "code": 400
}
```

---

## 七、对接流程总结

```
┌─────────────────────────────────────────────────────────┐
│ 1. 准备阶段（一次性）                                       │
│    WFE 后台配置 Integration + Entity Mapping + Workflow    │
│    IAM 分配 Client Credentials                           │
├─────────────────────────────────────────────────────────┤
│ 2. 初始化（首次或缓存过期时）                                │
│    获取 Token → 调 entity-type-mappings → 缓存 systemId   │
├─────────────────────────────────────────────────────────┤
│ 3. 运行时                                                │
│    创建 Ticket → 调 POST /cases → 保存返回的 caseId       │
│    查询状态  → 调 GET /by-system                         │
│    获取附件  → 调 GET /inbound-attachments               │
└─────────────────────────────────────────────────────────┘
```

---

## 八、注意事项

1. **Token 缓存**：IAM Token 有效期通常 1 小时，建议在过期前 5 分钟刷新
2. **SystemId 缓存**：systemId 不会变，可以长期缓存，但建议每天刷新一次
3. **字段值类型**：所有传给 WFE 的字段值必须是简单类型（string/number），不要传嵌套对象
4. **幂等性**：`POST /cases` 不做去重，每次调用都会创建新 Case。调用方需自行保证不重复调用
5. **租户隔离**：`X-Tenant-Id` 决定数据隔离，确保传正确的租户 ID
6. **Case 名称**：如果 caseName 重复，WFE 会自动加后缀（-2, -3...），不会报错

---

## 九、环境信息

| 环境       | WFE Base URL                        | IAM Token Endpoint                         |
| ---------- | ----------------------------------- | ------------------------------------------ |
| Dev        | `https://workflow-dev.item.pub`     | `https://id-dev.item.com/oauth2/token`     |
| Staging    | `https://workflow-staging.item.com` | `https://id-staging.item.com/oauth2/token` |
| Production | `https://workflow.item.com`         | `https://id.item.com/oauth2/token`         |

---

## 十、联系人

| 角色         | 说明                                            |
| ------------ | ----------------------------------------------- |
| WFE 后台配置 | 联系 WFE 团队配置 Integration 和 Entity Mapping |
| IAM 凭据申请 | 联系平台团队分配 Client Credentials             |
| API 问题排查 | 联系 WFE 开发团队                               |
