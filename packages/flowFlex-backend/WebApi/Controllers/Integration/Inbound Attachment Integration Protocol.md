# Inbound Attachment Integration Protocol

## Overview

Third-party platforms need to provide attachment interfaces according to this protocol for FlowFlex system to call and obtain attachment information.

---

## Interface Specification

### API Endpoint

**URL**: `/api/integration/external/v1/inbound-attachments`

**Request Method**: `GET`

**Base URL Example**: `https://your-domain.com/api/integration/external/v1/inbound-attachments`

### Request Parameters

| Parameter Name | Type   | Required | Location | Description                                      | Example                            |
| -------------- | ------ | -------- | -------- | ------------------------------------------------ | ---------------------------------- |
| SystemId       | string | Yes      | Query    | System ID (unique identifier for entity mapping) | `dcb7fee510a54b93b44617b2dc770e70` |

### Request Headers

| Header Name   | Type   | Required | Description                       | Example                                          |
| ------------- | ------ | -------- | --------------------------------- | ------------------------------------------------ |
| Authorization | string | Yes      | Authentication token              | `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` |
| Content-Type  | string | No       | Content type (not needed for GET) | `application/json`                               |

### Response Format

**Success Response** (HTTP 200):

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

**Response Field Description**:

| Field Name                      | Type    | Required | Description                   | Example                                        |
| ------------------------------- | ------- | -------- | ----------------------------- | ---------------------------------------------- |
| success                         | boolean | Yes      | Whether the request succeeded | true                                           |
| data                            | object  | Yes      | Response data object          | -                                              |
| data.attachments                | array   | Yes      | Attachment list               | -                                              |
| data.attachments[].id           | string  | Yes      | Attachment primary key ID     | "1995789533479837696"                          |
| data.attachments[].fileName     | string  | Yes      | File name (with extension)    | "sample-document.docx"                         |
| data.attachments[].fileSize     | string  | Yes      | File size (bytes, string)     | "1048576"                                      |
| data.attachments[].fileType     | string  | Yes      | File MIME type                | "application/pdf"                              |
| data.attachments[].fileExt      | string  | Yes      | File extension (without dot)  | "docx"                                         |
| data.attachments[].createDate   | string  | Yes      | Creation time                 | "2025-12-02 09:38:01 +00:00"                   |
| data.attachments[].downloadLink | string  | Yes      | Download link (full URL)      | "https://example.com/api/files/download/12345" |
| data.total                      | number  | No       | Total number of attachments   | 1                                              |
| message                         | string  | No       | Response message              | "Success"                                      |

**Field Details**:

- **id**: Unique identifier for the attachment, usually a database primary key or system-generated unique ID
- **fileName**: File name, should include file extension, supports Chinese characters
- **fileSize**: File size in bytes, using string format (e.g., "1048576" represents 1MB)
- **fileType**: File MIME type, common types:
  - `application/pdf` - PDF document
  - `application/vnd.openxmlformats-officedocument.wordprocessingml.document` - Word (.docx)
  - `application/msword` - Word (.doc)
  - `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` - Excel (.xlsx)
  - `application/vnd.ms-excel` - Excel (.xls)
  - `image/jpeg` - JPEG image
  - `image/png` - PNG image
  - `text/plain` - Text file
  - `application/zip` - ZIP archive
- **fileExt**: File extension, without dot (e.g., "docx", "pdf", "xlsx")
- **createDate**: Creation time, format: `"YYYY-MM-DD HH:mm:ss +00:00"` or ISO 8601 format
- **downloadLink**: File download link, must be a complete HTTP/HTTPS URL, directly accessible

**Error Response** (HTTP 4xx/5xx):

```json
{
  "success": false,
  "message": "Entity not found"
}
```

---

## Authentication Methods

According to the authentication method configured in FlowFlex integration settings, the following authentication mechanisms are supported:

- **API Key**: `X-API-Key: {apiKey}` or `Authorization: ApiKey {apiKey}`
- **Basic Auth**: `Authorization: Basic {base64(username:password)}`
- **Bearer Token**: `Authorization: Bearer {token}`
- **OAuth 2.0**: `Authorization: Bearer {accessToken}`

---

## Error Codes

| HTTP Status Code | Error Code          | Description           | Solution                                   |
| ---------------- | ------------------- | --------------------- | ------------------------------------------ |
| 400              | INVALID_PARAMETER   | Invalid parameter     | Check parameter format and required fields |
| 401              | UNAUTHORIZED        | Authentication failed | Check authentication information           |
| 403              | FORBIDDEN           | Access forbidden      | Check user permission configuration        |
| 404              | ENTITY_NOT_FOUND    | Entity not found      | Check if entity ID is correct              |
| 500              | INTERNAL_ERROR      | Internal server error | Contact technical support                  |
| 503              | SERVICE_UNAVAILABLE | Service unavailable   | Retry later                                |

---

## Usage Examples

### Example 1: Get attachment list by System ID

**Request URL**:

```
GET /api/integration/external/v1/inbound-attachments?SystemId=dcb7fee510a54b93b44617b2dc770e70
```

**Request Headers**:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**cURL Example**:

```bash
curl -X GET "https://your-domain.com/api/integration/external/v1/inbound-attachments?SystemId=dcb7fee510a54b93b44617b2dc770e70" \
  -H "Authorization: Bearer your-token-here"
```

**JavaScript/Fetch Example**:

```javascript
fetch(
  "https://your-domain.com/api/integration/external/v1/inbound-attachments?SystemId=dcb7fee510a54b93b44617b2dc770e70",
  {
    method: "GET",
    headers: {
      Authorization: "Bearer your-token-here",
    },
  }
)
  .then((response) => response.json())
  .then((data) => console.log(data));
```

**Response**:

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

## Notes

1. **File Size**: `fileSize` uses string format, unit is bytes
2. **Time Format**: `createDate` should use `"YYYY-MM-DD HH:mm:ss +00:00"` format
3. **Download Link**: `downloadLink` must be a complete URL, recommended validity period of at least 24 hours
4. **File Type**: `fileType` must comply with standard MIME type specifications
5. **Extension**: `fileExt` does not include dot, use lowercase letters

---

## Changelog

- **2025-01-XX**: Initial version release
  - Define attachment list retrieval interface specification
  - Define attachment data structure (id, fileName, fileSize, fileType, fileExt, createDate, downloadLink)
  - Support multiple authentication methods
  - Provide error handling specification
