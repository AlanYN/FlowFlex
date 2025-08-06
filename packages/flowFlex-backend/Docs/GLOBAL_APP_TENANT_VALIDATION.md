# 全局 AppCode 和 TenantId 验证与数据隔离

## 概述

FlowFlex 系统已实现了严格的全局 AppCode 和 TenantId 验证与数据隔离机制。所有 API 接口都需要通过请求头提供应用代码和租户标识，实现多租户、多应用的数据完全隔离。

## 核心功能

### 1. 强制头部验证
- 所有数据新增和查询接口都需要接收头部：`X-App-Code` 和 `X-Tenant-Id`
- 如果没有获取到这两个头部，系统将使用默认值 `DEFAULT`
- **不再**从 JWT token、查询参数、邮箱域名等其他来源推断这两个值

### 2. 数据库自动过滤
- 所有数据库查询自动添加 `WHERE app_code = ? AND tenant_id = ?` 条件
- 确保每个请求只能访问其对应应用和租户的数据
- 支持跨租户查询的特殊场景（使用过滤器禁用范围）

### 3. 实体自动标记
- 创建新实体时自动设置 `AppCode` 和 `TenantId` 字段
- 所有实体基类都包含这两个字段，默认值为 `DEFAULT`

## 使用方法

### 1. 客户端请求示例

#### 正确的请求格式
```http
GET /api/ow/workflows/v1
Headers:
  X-App-Code: MOBILE
  X-Tenant-Id: COMPANY1
  Authorization: Bearer your-jwt-token
```

#### 使用 curl 示例
```bash
curl -X GET "https://localhost:5019/api/ow/workflows/v1" \
  -H "X-App-Code: WEB" \
  -H "X-Tenant-Id: COMPANY2" \
  -H "Authorization: Bearer your-jwt-token"
```

#### 使用 JavaScript 示例
```javascript
const response = await fetch('/api/ow/workflows/v1', {
  method: 'GET',
  headers: {
    'X-App-Code': 'WEB',
    'X-Tenant-Id': 'COMPANY1',
    'Authorization': 'Bearer your-jwt-token',
    'Content-Type': 'application/json'
  }
});
```

### 2. 头部参数说明

| 头部名称 | 说明 | 必需 | 默认值 | 示例 |
|---------|------|------|--------|------|
| `X-App-Code` | 应用代码，用于区分不同应用 | 推荐 | `DEFAULT` | `WEB`, `MOBILE`, `ADMIN` |
| `X-Tenant-Id` | 租户标识，用于区分不同租户 | 推荐 | `DEFAULT` | `COMPANY1`, `COMPANY2`, `TEST` |

### 3. 支持的头部格式

系统支持以下头部名称（按优先级排序）：

**AppCode:**
1. `X-App-Code` （推荐）
2. `AppCode` （备用）

**TenantId:**
1. `X-Tenant-Id` （推荐）
2. `TenantId` （备用）

## 响应头部

系统会在每个响应中添加调试头部：

```http
HTTP/1.1 200 OK
X-Response-App-Code: WEB
X-Response-Tenant-Id: COMPANY1
X-Response-Request-Id: a1b2c3d4
```

## 开发指南

### 1. 创建新实体

```csharp
// 使用扩展方法自动设置隔离字段
var entity = new YourEntity();
entity.InitCreateInfo(userContext);
// 这会自动设置 AppCode 和 TenantId
```

### 2. 获取当前上下文

```csharp
public class YourController : ControllerBase
{
    public async Task<IActionResult> YourAction()
    {
        // 从 HttpContext 获取 AppContext
        var appContext = HttpContext.Items["AppContext"] as AppContext;
        var currentAppCode = appContext?.AppCode;
        var currentTenantId = appContext?.TenantId;
        
        // 或者通过 UserContext 依赖注入
        var userContext = HttpContext.RequestServices.GetService<UserContext>();
        var appCode = userContext?.AppCode;
        var tenantId = userContext?.TenantId;
    }
}
```

### 3. 跨租户查询（特殊场景）

```csharp
// 临时禁用过滤器进行跨租户查询
using (var scope = AppTenantFilter.CreateFilterDisabledScope(db))
{
    var allData = await db.Queryable<YourEntity>().ToListAsync();
    // 这里会查询所有租户的数据
}
```

## 数据库结构

### 表结构要求

所有业务表都必须包含以下字段：

```sql
CREATE TABLE your_table (
    id BIGINT PRIMARY KEY,
    tenant_id VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
    -- 其他业务字段...
    
    -- 建议创建复合索引优化查询性能
    INDEX idx_app_tenant (app_code, tenant_id)
);
```

### 实体基类

#### OwEntityBase（OW模块实体）
```csharp
public abstract class OwEntityBase
{
    public long Id { get; set; }
    public string TenantId { get; set; } = "DEFAULT";
    public string AppCode { get; set; } = "DEFAULT";
    // 其他基础字段...
}
```

#### AbstractEntityBase（通用实体）
```csharp
public abstract class AbstractEntityBase : IdEntityBase, ITenantFilter, IAppFilter
{
    public virtual string TenantId { get; set; } = string.Empty;
    public virtual string AppCode { get; set; } = "DEFAULT";
}
```

## 中间件配置

系统使用以下中间件顺序确保正确的请求处理：

```csharp
// Program.cs 中的中间件配置
app.UseRouting();
app.UseMiddleware<AppIsolationMiddleware>();      // 1. 应用隔离中间件
app.UseMiddleware<TenantMiddleware>();             // 2. 租户中间件（兼容性）
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // 3. 异常处理
app.UseAuthentication();                          // 4. 认证
app.UseAuthorization();                           // 5. 授权
```

## 日志监控

### 请求日志示例

```
[AppIsolationMiddleware] Request: GET /api/ow/workflows/v1, AppCode: WEB, TenantId: COMPANY1, RequestId: a1b2c3d4, ClientIp: 192.168.1.100
```

### 调试信息

当头部缺失时：
```
[AppIsolationMiddleware] No AppCode headers found, using default: DEFAULT
[AppIsolationMiddleware] No TenantId headers found, using default: DEFAULT
```

## 测试验证

### 1. 验证头部处理

```bash
# 测试有头部的请求
curl -X GET "http://localhost:5000/api/ow/users" \
  -H "X-App-Code: TEST" \
  -H "X-Tenant-Id: TEST_TENANT"

# 测试无头部的请求（应使用默认值）
curl -X GET "http://localhost:5000/api/ow/users"
```

### 2. 验证数据隔离

不同的 `AppCode` 和 `TenantId` 组合应该返回不同的数据集，确保完全隔离。

## 注意事项

### 1. 客户端集成
- 前端应用需要在所有 API 请求中包含正确的头部
- 建议在 HTTP 客户端的拦截器中自动添加这些头部

### 2. 性能考虑
- 数据库表应该为 `(app_code, tenant_id)` 创建复合索引
- 考虑根据租户数据量进行分表策略

### 3. 安全性
- 确保用户只能访问其有权限的应用和租户数据
- 在 JWT token 验证时同时验证 AppCode 和 TenantId 权限

### 4. 兼容性
- 系统保持向后兼容，支持旧的头部名称
- 如果都没有提供头部，系统会优雅地使用默认值而不会报错

## 故障排除

### 常见问题

1. **数据查询返回空结果**
   - 检查请求是否包含正确的 `X-App-Code` 和 `X-Tenant-Id` 头部
   - 确认数据库中的数据确实属于该应用和租户

2. **数据创建后无法查询**
   - 确认创建时使用了正确的 AppCode 和 TenantId
   - 检查实体是否正确继承了基类

3. **跨租户查询失败**
   - 使用 `CreateFilterDisabledScope` 临时禁用过滤器
   - 确保有足够的权限进行跨租户操作

4. **过滤器未生效，仍能查询到其他租户数据**
   - **重要**：已修复 SqlSugar 继承过滤器问题，现在为所有具体实体类型单独配置过滤器
   - 验证过滤器配置：使用调试接口 `GET /api/debug/test-filters` 检查过滤效果
   - 检查实体类是否包含 `TenantId` 和 `AppCode` 字段

### 调试接口

系统提供了调试接口来验证过滤器是否正确工作：

```bash
# 测试过滤器效果
curl -X GET "http://localhost:5000/api/debug/test-filters" \
  -H "X-App-Code: TEST" \
  -H "X-Tenant-Id: TEST_TENANT"

# 检查上下文信息
curl -X GET "http://localhost:5000/api/debug/context" \
  -H "X-App-Code: TEST" \
  -H "X-Tenant-Id: TEST_TENANT"
```

### 调试步骤

1. 检查响应头部中的 `X-Response-App-Code` 和 `X-Response-Tenant-Id`
2. 查看应用日志中的中间件日志
3. 验证数据库表结构是否包含必要的字段
4. 使用调试接口验证过滤器配置和效果
5. 确认 SqlSugar 过滤器是否正确配置

### 技术说明

由于 SqlSugar 的继承过滤器在某些情况下可能不生效，系统现在为所有主要实体类型单独配置了过滤器：

- Workflow
- User  
- Onboarding
- Stage
- Checklist
- Questionnaire

这确保了所有查询都会自动应用 AppCode 和 TenantId 过滤条件。

---

**重要提醒：这个验证机制确保了严格的数据隔离，所有接口调用都必须明确指定应用代码和租户标识，否则将使用默认值 `DEFAULT`。** 