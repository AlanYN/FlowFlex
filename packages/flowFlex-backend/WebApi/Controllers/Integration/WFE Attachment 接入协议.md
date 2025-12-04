# WFE Attachment 接入协议

## 概述

第三方平台需要按照本协议提供附件接口，供 FlowFlex 系统调用获取附件信息。

---

## 接口规范

第三方平台需要提供一个接口用于获取附件列表，请求方式和参数由第三方平台自行定义。

### 响应格式

**成功响应** (HTTP 200):

```json
{
  "success": true,
  "data": {
    "attachments": [
      {
        "id": "1995789533479837696",
        "fileName": "sample-document.docx",
        "fileSize": "1048576",
        "fileType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "fileExt": "docx",
        "createDate": "2025-12-02 09:38:01 +00:00",
        "downloadLink": "https://example.com/api/files/download/12345?token=xxx"
      }
    ],
    "total": 1
  },
  "message": "Success"
}
```

**响应字段说明**:

| 字段名                          | 类型    | 必填 | 说明                         | 示例                                           |
| ------------------------------- | ------- | ---- | ---------------------------- | ---------------------------------------------- |
| success                         | boolean | 是   | 请求是否成功                 | true                                           |
| data                            | object  | 是   | 响应数据对象                 | -                                              |
| data.attachments                | array   | 是   | 附件列表                     | -                                              |
| data.attachments[].id           | string  | 是   | 附件主键 ID                  | "1995789533479837696"                          |
| data.attachments[].fileName     | string  | 是   | 文件名（包含扩展名）         | "sample-document.docx"                         |
| data.attachments[].fileSize     | string  | 是   | 文件大小（字节，字符串格式） | "1048576"                                      |
| data.attachments[].fileType     | string  | 是   | 文件 MIME 类型               | "application/pdf"                              |
| data.attachments[].fileExt      | string  | 是   | 文件扩展名（不含点号）       | "docx"                                         |
| data.attachments[].createDate   | string  | 是   | 创建时间                     | "2025-12-02 09:38:01 +00:00"                   |
| data.attachments[].downloadLink | string  | 是   | 文件下载链接（完整 URL）     | "https://example.com/api/files/download/12345" |
| data.total                      | number  | 否   | 附件总数                     | 1                                              |
| message                         | string  | 否   | 响应消息                     | "Success"                                      |

**字段详细说明**:

- **id**: 附件的唯一标识符，通常是数据库主键或系统生成的唯一 ID
- **fileName**: 文件名，应包含文件扩展名，支持中文
- **fileSize**: 文件大小，单位为字节，使用字符串格式（如 "1048576" 表示 1MB）
- **fileType**: 文件的 MIME 类型，常见类型：
  - `application/pdf` - PDF 文档
  - `application/vnd.openxmlformats-officedocument.wordprocessingml.document` - Word (.docx)
  - `application/msword` - Word (.doc)
  - `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` - Excel (.xlsx)
  - `application/vnd.ms-excel` - Excel (.xls)
  - `image/jpeg` - JPEG 图片
  - `image/png` - PNG 图片
  - `text/plain` - 文本文件
  - `application/zip` - ZIP 压缩包
- **fileExt**: 文件扩展名，不包含点号（如 "docx", "pdf", "xlsx"）
- **createDate**: 创建时间，格式：`"YYYY-MM-DD HH:mm:ss +00:00"` 或 ISO 8601 格式
- **downloadLink**: 文件下载链接，必须是完整的 HTTP/HTTPS URL，可直接访问

**错误响应** (HTTP 4xx/5xx):

```json
{
  "success": false,
  "message": "Entity not found"
}
```

---

## 认证方式

根据 FlowFlex 集成配置中设置的认证方式，支持以下认证机制：

- **API Key**: `X-API-Key: {apiKey}` 或 `Authorization: ApiKey {apiKey}`
- **Basic Auth**: `Authorization: Basic {base64(username:password)}`
- **Bearer Token**: `Authorization: Bearer {token}`
- **OAuth 2.0**: `Authorization: Bearer {accessToken}`

---

## 错误码

| HTTP 状态码 | 错误码              | 说明           | 解决方案                   |
| ----------- | ------------------- | -------------- | -------------------------- |
| 400         | INVALID_PARAMETER   | 请求参数无效   | 检查请求参数格式和必填字段 |
| 401         | UNAUTHORIZED        | 认证失败       | 检查认证信息是否正确       |
| 403         | FORBIDDEN           | 无权限访问     | 检查用户权限配置           |
| 404         | ENTITY_NOT_FOUND    | 实体不存在     | 检查实体 ID 是否正确       |
| 500         | INTERNAL_ERROR      | 服务器内部错误 | 联系技术支持               |
| 503         | SERVICE_UNAVAILABLE | 服务不可用     | 稍后重试                   |

---

## 使用示例

### 示例 1：获取 Case 的附件列表

**请求**:

```http
POST https://api.example.com/attachments
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "entityId": "CASE-001",
  "entityType": "Case"
}
```

**响应**:

```json
{
  "success": true,
  "data": {
    "attachments": [
      {
        "id": "1995789533479837696",
        "fileName": "contract.pdf",
        "fileSize": "2097152",
        "fileType": "application/pdf",
        "fileExt": "pdf",
        "createDate": "2025-12-01 14:20:30 +00:00",
        "downloadLink": "https://api.example.com/files/download/12345?token=xxx"
      },
      {
        "id": "1995789533479837697",
        "fileName": "invoice.xlsx",
        "fileSize": "524288",
        "fileType": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "fileExt": "xlsx",
        "createDate": "2025-12-02 09:38:01 +00:00",
        "downloadLink": "https://api.example.com/files/download/12346?token=xxx"
      }
    ],
    "total": 2
  },
  "message": "Success"
}
```

---

## 注意事项

1. **文件大小**: `fileSize` 使用字符串格式，单位为字节
2. **时间格式**: `createDate` 建议使用 `"YYYY-MM-DD HH:mm:ss +00:00"` 格式
3. **下载链接**: `downloadLink` 必须是完整的 URL，建议有效期至少 24 小时
4. **文件类型**: `fileType` 必须符合标准 MIME 类型规范
5. **扩展名**: `fileExt` 不包含点号，使用小写字母

---

## 更新日志

- **2025-01-XX**: 初始版本发布
  - 定义附件列表获取接口规范
  - 定义附件数据结构（id, fileName, fileSize, fileType, fileExt, createDate, downloadLink）
  - 支持多种认证方式
  - 提供错误处理规范

