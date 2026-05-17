# Design: Plugin Price List — AI Coding Package

<!-- 来源: technical-design/design.md -->

## 架构设计

### 分层结构

```
WebApi/Controllers/OW/PluginPriceListController.cs     ← API 入口
    ↓
Application/Services/OW/PluginPriceListService.cs      ← 业务逻辑 + 权限检查
    ↓
Domain/Entities/OW/PluginPriceList.cs                  ← 实体定义
Domain/Repository/OW/IPluginPriceListRepository.cs     ← 仓储接口
    ↓
SqlSugarDB/Implements/OW/PluginPriceListRepository.cs  ← 数据访问
```

### 关键设计决策

1. **部署位置**：dist 放 WFE 前端 `public/plugins/price-list/`，Nginx `try_files` 直接 serve
2. **CaseCode**：前端传 caseCode（如 "C00022"），后端查 ff_onboarding 获取 ID 做权限检查
3. **JSONB 存储**：sections 数据整体存储，不做关系型拆分
4. **GET 无数据**：返回 200 + data=null + permission 字段（不是 HTTP 404）
5. **Upsert**：POST 保存时有则更新、无则创建
6. **并发**：最后写入胜出，不加乐观锁

### 数据库表

```sql
CREATE TABLE ff_plugin_price_lists (
    id              BIGINT PRIMARY KEY,
    case_code       VARCHAR(50) NOT NULL,
    customer_code   VARCHAR(50),
    customer_name   VARCHAR(200),
    price_list_type VARCHAR(50) DEFAULT 'Customer Specific',
    start_date      VARCHAR(20),
    end_date        VARCHAR(20),
    data            JSONB NOT NULL DEFAULT '{"sections":[]}',
    status          VARCHAR(20) DEFAULT 'draft',
    tenant_id       VARCHAR(32) NOT NULL,
    app_code        VARCHAR(32) DEFAULT 'default',
    is_valid        BOOLEAN DEFAULT TRUE,
    create_date     TIMESTAMPTZ DEFAULT NOW(),
    modify_date     TIMESTAMPTZ DEFAULT NOW(),
    create_by       VARCHAR(50) DEFAULT 'SYSTEM',
    modify_by       VARCHAR(50) DEFAULT 'SYSTEM',
    create_user_id  BIGINT,
    modify_user_id  BIGINT
);

CREATE UNIQUE INDEX idx_plugin_price_list_case_code 
    ON ff_plugin_price_lists(tenant_id, app_code, case_code) 
    WHERE is_valid = TRUE;
```

### API 设计

#### GET /api/ow/plugin-price-lists/v1?caseCode={caseCode}

**Response 200（有数据）**：
```json
{
  "code": 200,
  "data": {
    "id": "1234567890",
    "caseCode": "C00022",
    "customerCode": "KINCOF0001",
    "customerName": "KING COFFEE",
    "priceListType": "Customer Specific",
    "startDate": "2026-05-04",
    "endDate": "",
    "data": { "sections": [...] },
    "status": "draft",
    "permission": "write",
    "createdBy": "amli",
    "createdAt": "2026-05-12T10:00:00Z",
    "updatedBy": "vserra",
    "updatedAt": "2026-05-12T15:30:00Z"
  }
}
```

**Response 200（无数据）**：
```json
{
  "code": 200,
  "data": { "caseCode": "C00022", "permission": "write", "data": null }
}
```

#### POST /api/ow/plugin-price-lists/v1

**Request**：`{ caseCode, customerCode, customerName, priceListType, startDate, endDate, data }`  
**Response**：`{ code: 200, data: { id, savedAt } }`  
**规则**：status="submitted" 时拒绝保存（400）

#### POST /api/ow/plugin-price-lists/v1/submit

**Request**：`{ caseCode }`  
**Response**：`{ code: 200, data: { status: "submitted", submittedAt } }`  
**规则**：已 submitted 返回 400，无数据返回 404

---

<!-- 来源: test-verification/design.md -->

## 测试用例摘要

| TC | 场景 | 预期 |
|----|------|------|
| TC-001 | GET 有数据+写权限 | 200, permission="write" |
| TC-002 | GET 有数据+只读 | 200, permission="read" |
| TC-003 | GET 无数据+有权限 | 200, data=null |
| TC-004 | GET CaseCode 不存在 | 404 |
| TC-005 | GET 无权限 | 403 |
| TC-006 | GET 未认证 | 401 |
| TC-007 | POST Save 新建 | 200, 新记录 |
| TC-008 | POST Save 更新 | 200, 记录更新 |
| TC-009 | POST Save 已提交 | 400 |
| TC-010 | POST Save 无写权限 | 403 |
| TC-011 | POST Submit 成功 | 200, status=submitted |
| TC-012 | POST Submit 重复 | 400 |
| TC-013 | POST Submit 无数据 | 404 |
| TC-014 | 租户隔离 | 不返回其他租户数据 |
| TC-015~018 | 前端交互 | 手动浏览器验证 |
