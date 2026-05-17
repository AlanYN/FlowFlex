# API Specification: Plugin Price List

## Base URL

```
/api/ow/plugin-price-lists/v1
```

## Authentication

所有端点需要 JWT Bearer Token（通过同域名 cookie 自动携带）。

---

## GET /api/ow/plugin-price-lists/v1

获取指定 Case 的 Price List 数据及用户权限。

### Request

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| caseCode | string (query) | Yes | WFE Case Code (e.g., "C00022") |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| Authorization | Yes | Bearer {JWT token} (auto via cookie) |
| X-Tenant-Id | Yes | Tenant identifier |
| X-App-Code | No | Application code (default: "default") |

### Response 200 — 有数据

```json
{
  "code": 200,
  "data": {
    "id": "1923456789012345678",
    "caseCode": "C00022",
    "customerCode": "KINCOF0001",
    "customerName": "KING COFFEE",
    "priceListType": "Customer Specific",
    "startDate": "2026-05-04",
    "endDate": "",
    "data": {
      "sections": [...]
    },
    "status": "draft",
    "permission": "write",
    "createdBy": "amli",
    "createdAt": "2026-05-12T10:00:00Z",
    "updatedBy": "vserra",
    "updatedAt": "2026-05-12T15:30:00Z"
  }
}
```

### Response 200 — 无数据（Case 存在但尚未创建 Price List）

```json
{
  "code": 200,
  "data": {
    "caseCode": "C00022",
    "permission": "write",
    "data": null
  }
}
```

### Response 403 — 无权限

```json
{
  "code": 403,
  "message": "Access denied to this case"
}
```

### Response 404 — CaseCode 不存在

```json
{
  "code": 404,
  "message": "Case not found: C99999"
}
```

---

## POST /api/ow/plugin-price-lists/v1

保存或更新 Price List（Upsert 语义）。

### Request Body

```json
{
  "caseCode": "C00022",
  "customerCode": "KINCOF0001",
  "customerName": "KING COFFEE",
  "priceListType": "Customer Specific",
  "startDate": "2026-05-04",
  "endDate": "",
  "data": {
    "sections": [
      {
        "locations": ["825-Northampton"],
        "expanded": true,
        "items": [
          {
            "chargeCode": "HANDLING-0128",
            "itemName": "Offload(Rate by Case qty)-Range Rate",
            "category": "HANDLING",
            "uom": "Case",
            "rateType": "Unit Price",
            "price": "0.560",
            "isTiered": true,
            "tiers": [
              { "from": "0", "to": "500", "price": "570" }
            ],
            "availableDimensions": ["OffloadType", "ContainerSize"],
            "conditionValues": {
              "OffloadType": ["Floor Loaded"],
              "ContainerSize": ["40'"]
            },
            "customerDescription": "Container Offloading"
          }
        ]
      }
    ]
  }
}
```

### Response 200

```json
{
  "code": 200,
  "data": {
    "id": "1923456789012345678",
    "savedAt": "2026-05-14T12:00:00Z"
  }
}
```

### Response 403 — 无写权限

```json
{
  "code": 403,
  "message": "Write access denied to this case"
}
```

### Business Rules

- 如果该 caseCode 已有记录 → 更新（UPDATE）
- 如果该 caseCode 无记录 → 创建（INSERT）
- 如果 status 为 "submitted" → 拒绝保存，返回 400

---

## POST /api/ow/plugin-price-lists/v1/submit

将 Price List 标记为已提交。

### Request Body

```json
{
  "caseCode": "C00022"
}
```

### Response 200

```json
{
  "code": 200,
  "data": {
    "status": "submitted",
    "submittedAt": "2026-05-14T12:00:00Z"
  }
}
```

### Response 400 — 已提交

```json
{
  "code": 400,
  "message": "Price list already submitted"
}
```

### Response 404 — 无数据可提交

```json
{
  "code": 404,
  "message": "No price list found for case: C00022"
}
```

---

## 统一响应格式

所有成功响应遵循 FlowFlex 统一格式：

```json
{
  "code": 200,
  "data": { ... }
}
```

错误响应：

```json
{
  "code": 4xx,
  "message": "Error description"
}
```
