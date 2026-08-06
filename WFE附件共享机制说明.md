# WFE 附件共享机制说明（Inbound & Outbound）

> 本文档说明 WFE 与外部系统之间的附件共享机制，包括 Inbound（拉取外部附件）和 Outbound（暴露 WFE 附件）两个方向。

---

## 一、概述

WFE 的附件共享采用 **PULL 模式**——不主动推送文件，而是按需拉取或按配置暴露。

| 配置项                            | 方向           | 含义                                       |
| --------------------------------- | -------------- | ------------------------------------------ |
| **Inbound Attachment Sharing**    | 外部系统 → WFE | WFE 去外部系统拉取附件列表，在 Case 里展示 |
| **Outbound Attachments to Share** | WFE → 外部系统 | 外部系统从 WFE 查询 Case 里的文件          |

两个方向都**不涉及物理文件传输**（只传元数据 + 下载链接），文件始终存在原始系统中。

---

## 二、Inbound Attachment Sharing（WFE 拉取外部附件）

### 2.1 含义

配置 WFE 应该调用外部系统的哪个接口来获取附件列表。WFE 用户在 Case 页面查看外部系统的附件时触发。

### 2.2 配置位置

WFE 后台：**Integration > Inbound Settings > Attachment Sharing**

### 2.3 配置内容

每条配置包含：

| 字段       | 说明                                          | 示例                  |
| ---------- | --------------------------------------------- | --------------------- |
| ModuleName | 外部系统的模块名                              | `"Leads"`             |
| WorkflowId | 关联的 WFE 工作流 ID                          | `1845409245046509568` |
| ActionId   | HTTP Action 定义 ID（指向外部系统的附件接口） | `456`                 |

数据存储在 `ff_integration.inbound_attachments` 列（JSON 格式）：

```json
[
  {
    "Id": "1995789533479837696",
    "ModuleName": "Leads",
    "WorkflowId": 1845409245046509568,
    "ActionId": 456
  }
]
```

### 2.4 运行时流程

```
WFE 用户在 Case 页面点击查看外部附件
    ↓
WFE 前端调: GET /api/integration/external/v1/fetch-inbound-attachments?SystemId=xxx&EntityId=yyy
    ↓
WFE 后端读取 Inbound Attachment Sharing 配置
    ↓
按 ActionId 找到 Action 定义（HTTP 请求配置）
    ↓
WFE 回调外部系统: GET /your-system/attachments?entityId=yyy
    ↓
外部系统返回附件列表（含 downloadLink）
    ↓
WFE 前端展示，用户可点击下载
```

### 2.5 外部系统需要提供的接口

需要暴露一个 GET 接口，返回格式遵循 Inbound Attachment Protocol：

```bash
GET /api/your-system/wfe/attachments?entityId={entityId}
```

**响应格式：**

```json
{
  "success": true,
  "data": {
    "attachments": [
      {
        "id": "att-001",
        "fileName": "contract.pdf",
        "fileSize": "102400",
        "fileType": "application/pdf",
        "fileExt": "pdf",
        "createDate": "2026-07-20 10:00:00 +00:00",
        "downloadLink": "https://your-oss.com/files/contract.pdf"
      }
    ],
    "total": 1
  }
}
```

**字段说明：**

| 字段           | 类型   | 必填 | 说明                                         |
| -------------- | ------ | ---- | -------------------------------------------- |
| `id`           | string | 是   | 附件唯一标识                                 |
| `fileName`     | string | 是   | 文件名                                       |
| `fileSize`     | string | 否   | 文件大小（字节）                             |
| `fileType`     | string | 否   | MIME 类型                                    |
| `fileExt`      | string | 否   | 文件扩展名                                   |
| `createDate`   | string | 否   | 创建时间                                     |
| `downloadLink` | string | 是   | 文件下载链接（WFE 用户点击时直接访问此 URL） |

### 2.6 CRM 的实际例子

CRM 为此提供了接口：`GET /crm/system/v1/wfe/attachments?entityId=X`

WFE 在 Integration 的 Inbound Attachment Sharing 中配置了一个 Action 指向此接口。当 WFE 用户查看 Case 的外部附件时，WFE 自动回调 CRM 获取该 Lead 的附件列表。

---

## 三、Outbound Attachments to Share（暴露 WFE 附件给外部系统）

### 3.1 含义

配置 WFE Case 中哪些工作流、哪些阶段的文件可以暴露给外部系统查询。

### 3.2 配置位置

WFE 后台：**Integration > Outbound Settings > Attachments to Share**

### 3.3 配置内容

每条配置包含：

| 字段       | 说明                           | 示例                  |
| ---------- | ------------------------------ | --------------------- |
| WorkflowId | 哪个工作流的文件               | `1845409245046509568` |
| StageIds   | 该工作流下哪些阶段的文件可暴露 | `[10, 20, 30]`        |

数据存储在 `ff_integration.outbound_attachments` 列（JSON 格式）：

```json
[
  {
    "Id": "1995789533479837700",
    "WorkflowId": 1845409245046509568,
    "StageIds": [10, 20, 30]
  }
]
```

### 3.4 运行时流程

```
外部系统想查看 WFE Case 里产生的文件
    ↓
外部系统调: GET /api/integration/external/v1/outbound-attachments?SystemId=xxx
    ↓
WFE 根据 SystemId 找到关联的 Case 列表
    ↓
返回这些 Case 中的文件列表（含 downloadLink）
    ↓
外部系统展示给用户
```

### 3.5 curl 示例

```bash
curl -X GET "https://workflow-staging.item.com/api/integration/external/v1/outbound-attachments?SystemId=ABC123DEF456" \
  -H "Authorization: Bearer {access_token}" \
  -H "X-Tenant-Id: 1000"
```

**响应格式：**

```json
{
  "success": true,
  "data": {
    "attachments": [
      {
        "id": "file-001",
        "fileName": "approval-result.pdf",
        "fileSize": "204800",
        "fileType": "application/pdf",
        "fileExt": "pdf",
        "createDate": "2026-07-20 15:30:00 +00:00",
        "downloadLink": "https://workflow-staging.item.com/files/download/file-001"
      }
    ],
    "total": 1
  }
}
```

### 3.6 CRM 的实际例子

CRM 前端的 WFE Workflows 面板中有个 Attachments 区域，调用 `GET /inbound-attachments?SystemId=xxx&entityId=yyy` 来获取 WFE Case 中的文件，展示给 CRM 用户。

---

## 四、完整流向示意图

```
┌──────────────────┐                              ┌──────────────────┐
│    外部系统       │                              │       WFE        │
│  (CRM / Ticket)  │                              │    (FlowFlex)    │
└────────┬─────────┘                              └────────┬─────────┘
         │                                                  │
         │  【Inbound — WFE 拉取外部附件】                    │
         │                                                  │
         │  <── GET /your-api/attachments?entityId=X ───────│  WFE 回调外部
         │  ──→ 返回 {attachments: [{downloadLink}]} ──────→│  WFE UI 展示
         │                                                  │
         │  【Outbound — 外部查询 WFE 附件】                  │
         │                                                  │
         │  ──→ GET /outbound-attachments?SystemId=X ──────→│
         │  <── 返回 WFE Case 文件列表 ────────────────────←│  外部系统展示
         │                                                  │
```

---

## 五、三个容易混淆的附件接口对比

| 接口                             | 方向         | 谁调谁         | 做什么                                   |
| -------------------------------- | ------------ | -------------- | ---------------------------------------- |
| `GET /fetch-inbound-attachments` | 外部 → WFE   | WFE 调外部系统 | WFE 拉取外部的附件列表展示在 Case 里     |
| `GET /inbound-attachments`       | WFE → 调用方 | 外部系统调 WFE | 获取 WFE Case 里的文件（排除外部导入的） |
| `GET /outbound-attachments`      | WFE → 调用方 | 外部系统调 WFE | 获取 WFE Case 里的文件（按配置暴露）     |

> `inbound-attachments` 和 `outbound-attachments` 看起来都是"从 WFE 拿文件"，区别在于：
>
> - `inbound-attachments` 返回 WFE 内部产生的文件（排除外部导入的）
> - `outbound-attachments` 按后台配置的工作流/阶段范围返回文件

---

## 六、对接建议

### 场景 1：WFE 用户想在 Case 里看到外部系统的文件

**用 Inbound Attachment Sharing**

外部系统需要：

1. 提供一个 GET 接口返回附件列表（遵循上述 Protocol）
2. 文件存在自己的 OSS/S3，提供可访问的 downloadLink
3. WFE 后台配置 Inbound Action 指向该接口

### 场景 2：外部系统想查看 WFE Case 处理中产生的文件

**用 Outbound Attachments to Share**

外部系统需要：

1. 调 `GET /outbound-attachments?SystemId=xxx` 获取文件列表
2. 根据返回的 downloadLink 下载/展示文件
3. WFE 后台需配置暴露哪些工作流/阶段的文件

### 场景 3：想把文件物理存入 WFE Case

**Inbound/Outbound 都不适用**（它们只传链接不传文件）

需要使用 WFE 内部接口：

```
POST /api/ow/onboardings/{caseId}/files/v1/import
```

```json
{
  "stageId": 1845411139018035200,
  "files": [
    {
      "downloadLink": "https://your-oss.com/file.pdf",
      "fileName": "contract.pdf",
      "source": "Ticket"
    }
  ],
  "category": "Document"
}
```

WFE 会从 URL 下载文件并物理存储。此接口需标准 WFE 认证权限，非外部集成专用接口。

---

## 七、总结对比表

| 需求                          | 方案               | 文件存在哪 | 需要外部系统做什么                    |
| ----------------------------- | ------------------ | ---------- | ------------------------------------- |
| WFE Case 里展示外部系统的文件 | Inbound            | 外部系统   | 提供附件查询接口                      |
| 外部系统展示 WFE Case 的文件  | Outbound           | WFE        | 调 WFE 的 outbound-attachments API    |
| 把文件物理存入 WFE Case       | Import 接口        | WFE        | 提供 downloadLink，调 WFE import 接口 |
| 双向都能看到                  | Inbound + Outbound | 各自系统   | 两边都配置                            |
