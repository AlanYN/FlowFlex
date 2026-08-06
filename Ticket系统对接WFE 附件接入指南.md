# Ticket 系统对接 WFE 附件 — 接入指南

> 给 Ticket 团队的附件对接说明。简单直接，告诉你需要做什么。

---

## 你需要做的事

**提供一个 GET 接口**，让 WFE 能根据 Ticket ID 查到该 Ticket 的附件列表。

---

## 接口规范

### 请求

```
GET /api/your-path/attachments?entityId={ticketId}
```

- `entityId` 就是创建 Case 时你传给 WFE 的那个 `entityId`（即 Ticket ID）
- 认证方式可按照ticket这边的来

### 响应格式

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

### 字段说明

| 字段           | 必填 | 说明                     |
| -------------- | ---- | ------------------------ |
| `id`           | 是   | 附件唯一标识             |
| `fileName`     | 是   | 文件名                   |
| `fileSize`     | 否   | 文件大小（字节，字符串） |
| `fileType`     | 否   | MIME 类型                |
| `fileExt`      | 否   | 扩展名                   |
| `createDate`   | 否   | 创建时间                 |
| `downloadLink` | 是   | 可直接访问的下载链接     |

---

## 工作原理

```
WFE 用户在 Case 页面查看附件
    ↓
WFE 自动调你的接口: GET /api/.../attachments?entityId=TKT-456
    ↓
你返回该 Ticket 的附件列表 + downloadLink
    ↓
WFE 页面展示附件，用户点击直接从你的 downloadLink 下载
```

文件始终存在你这边（OSS/S3），WFE 不会把文件拷走，只是展示链接。

---

## 你不需要做的事

- 不需要主动推送文件给 WFE
- 不需要调 WFE 的上传接口
- 不需要关心 WFE 内部怎么存储

---

## WFE 侧需要配置的（由 WFE 团队完成）

1. 在 Integration 的 **Inbound Settings > Attachment Sharing** 中创建一条配置
2. 指定一个 Action，目标 URL 指向你提供的接口
3. 配置完后 WFE 就会在需要时自动调你的接口

---

## 注意事项

- `downloadLink` 必须是外网可访问的（WFE 服务端和用户浏览器都要能访问）
- 如果是私有文件，建议用带过期时间的预签名 URL（如 S3 Presigned URL）
- 没有附件时返回空数组即可：`{"success": true, "data": {"attachments": [], "total": 0}}`
