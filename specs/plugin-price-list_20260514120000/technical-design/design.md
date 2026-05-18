# Technical Design: Plugin Price List

## 技术选型

| 层级 | 技术 | 说明 |
|------|------|------|
| 后端框架 | ASP.NET Core 8 | 复用 FlowFlex 现有框架 |
| ORM | SqlSugar | 复用现有 ORM |
| 数据库 | PostgreSQL | 复用现有数据库实例 |
| 前端 | Vue 3 + Vite | Amanda 已完成原型 |
| 认证 | JWT Bearer | 复用 WFE 现有认证 |
| 部署 | 静态文件 + API 同服务 | wwwroot + Controller |

---

## 架构设计

### 分层结构

```
WebApi/Controllers/OW/PluginPriceListController.cs     ← API 入口
    ↓
Application/Services/OW/PluginPriceListService.cs      ← 业务逻辑
    ↓
Domain/Entities/OW/PluginPriceList.cs                  ← 实体定义
Domain/Repository/OW/IPluginPriceListRepository.cs     ← 仓储接口
    ↓
SqlSugarDB/Implements/OW/PluginPriceListRepository.cs  ← 数据访问
```

### 关键设计决策

1. **部署位置**：Amanda 的 dist 文件放在 WFE 前端项目的 `public/plugins/price-list/` 目录下（不是后端 wwwroot）。构建后 Nginx 的 `try_files $uri` 会直接匹配到静态文件，不走 WFE SPA 的 Vue Router。API 调用 `/api/ow/...` 由 Nginx 转发到后端。

2. **CaseCode vs CaseId**：前端 URL 传的是 `caseCode`（如 "C00022"），不是 Onboarding 的数据库 ID。后端需要通过 `caseCode` 查找 Onboarding 记录获取其 ID，再做权限检查。

2. **JSONB 存储**：Price List 的 sections 数据以 JSONB 存储在 `data` 字段，不做关系型拆分。原因：
   - 数据结构复杂且嵌套深（sections → items → tiers → conditionValues）
   - 读写都是整体操作（不需要单独查询某个 item）
   - 未来对接 BNP API 时直接取出 JSON 转换格式

3. **权限检查流程**：
   ```
   caseCode → 查 ff_onboarding 表 → 获取 onboarding.Id
   → IPermissionService.CheckCaseAccessAsync(userId, onboarding.Id, OperationType.View)
   → 返回 CanView / CanOperate
   ```

4. **Upsert 语义**：POST 保存时，如果该 caseCode 已有记录则更新，没有则创建。

---

## 数据库设计

### 表结构：ff_plugin_price_lists

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| id | BIGINT | PK | 雪花 ID |
| case_code | VARCHAR(50) | NOT NULL | WFE Case Code（如 "C00022"） |
| customer_code | VARCHAR(50) | NULLABLE | 客户代码 |
| customer_name | VARCHAR(200) | NULLABLE | 客户名称 |
| price_list_type | VARCHAR(50) | DEFAULT 'Customer Specific' | 类型 |
| start_date | VARCHAR(20) | NULLABLE | 生效日期 |
| end_date | VARCHAR(20) | NULLABLE | 结束日期 |
| data | JSONB | NOT NULL | 完整 sections JSON |
| status | VARCHAR(20) | DEFAULT 'draft' | draft / submitted / approved |
| tenant_id | VARCHAR(32) | NOT NULL | 租户隔离 |
| app_code | VARCHAR(32) | DEFAULT 'default' | 应用隔离 |
| is_valid | BOOLEAN | DEFAULT TRUE | 软删除标记 |
| create_date | TIMESTAMPTZ | DEFAULT NOW() | 创建时间 |
| modify_date | TIMESTAMPTZ | DEFAULT NOW() | 修改时间 |
| create_by | VARCHAR(50) | DEFAULT 'SYSTEM' | 创建人 |
| modify_by | VARCHAR(50) | DEFAULT 'SYSTEM' | 修改人 |
| create_user_id | BIGINT | NULLABLE | 创建人 ID |
| modify_user_id | BIGINT | NULLABLE | 修改人 ID |

### 索引

```sql
-- 唯一约束：同一租户+应用下，一个 CaseCode 只有一份有效的 Price List
CREATE UNIQUE INDEX idx_plugin_price_list_case_code 
  ON ff_plugin_price_lists(tenant_id, app_code, case_code) 
  WHERE is_valid = TRUE;
```

---

## API 设计

### GET /api/ow/plugin-price-lists/v1

**Query Parameters**：
- `caseCode` (required): WFE Case Code

**Response 200**：
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

**Response 404**：该 Case 无 Price List
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

> 注：404 场景不返回 HTTP 404，而是返回 200 + data=null + permission 字段。这样前端可以知道自己有权限但数据为空，可以开始创建。

**Response 403**：无权限
```json
{
  "code": 403,
  "message": "Access denied"
}
```

### POST /api/ow/plugin-price-lists/v1

**Request Body**：
```json
{
  "caseCode": "C00022",
  "customerCode": "KINCOF0001",
  "customerName": "KING COFFEE",
  "priceListType": "Customer Specific",
  "startDate": "2026-05-04",
  "endDate": "",
  "data": { "sections": [...] }
}
```

**Response 200**：
```json
{
  "code": 200,
  "data": {
    "id": "1234567890",
    "savedAt": "2026-05-14T12:00:00Z"
  }
}
```

### POST /api/ow/plugin-price-lists/v1/submit

**Request Body**：
```json
{
  "caseCode": "C00022"
}
```

**Response 200**：
```json
{
  "code": 200,
  "data": {
    "status": "submitted",
    "submittedAt": "2026-05-14T12:00:00Z"
  }
}
```

---

## 风险登记表

| ID | 风险 | 影响 | 概率 | 缓解措施 |
|----|------|------|------|---------|
| R-1 | CaseCode 在 Onboarding 表中不存在 | API 无法做权限检查 | 低 | 返回 404 + 明确错误信息 |
| R-2 | JSONB 数据过大（342 items 全选） | 性能下降 | 低 | 单条记录预估 < 1MB，PostgreSQL JSONB 可处理 |
| R-3 | 并发保存冲突（两人同时编辑） | 后保存的覆盖先保存的 | 中 | 本期接受"最后写入胜出"，未来可加乐观锁 |
| R-4 | JWT cookie 跨子域问题 | 认证失败 | 低 | 同域名部署，不存在跨域 |
| R-5 | 前端 Vite base path 配置错误 | 静态资源 404 | 低 | 构建前确认 base 配置 |
