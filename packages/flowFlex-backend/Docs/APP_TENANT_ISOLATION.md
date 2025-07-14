# FlowFlex 应用和租户隔离功能

## 概述

FlowFlex 系统实现了双重数据隔离机制：
- **应用隔离 (AppCode)**：不同应用之间的数据隔离
- **租户隔离 (TenantId)**：同一应用内不同租户的数据隔离

## 架构设计

### 1. 中间件层次

```
AppIsolationMiddleware (处理 appCode 和 tenantId)
    ↓
TenantMiddleware (兼容性支持)
    ↓
其他中间件...
```

### 2. 数据模型

#### AppContext 模型
```csharp
public class AppContext
{
    public string AppCode { get; set; } = "DEFAULT";
    public string TenantId { get; set; } = "DEFAULT";
    public string RequestId { get; set; }
    public DateTimeOffset RequestTime { get; set; }
    public string ClientIp { get; set; }
    public string UserAgent { get; set; }
}
```

#### UserContext 扩展
```csharp
public class UserContext : UserInfoModel
{
    public string AppCode { get; set; } = "DEFAULT";
    public string TenantId { get; set; } = "DEFAULT";
    // ... 其他属性
}
```

### 3. 数据库设计

所有业务表都包含以下字段：
- `tenant_id VARCHAR(32) NOT NULL DEFAULT 'DEFAULT'`
- `app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT'`

复合索引：`(app_code, tenant_id)` 用于优化查询性能。

## 使用方法

### 1. 请求头设置

客户端请求需要包含以下头部信息：

```http
X-App-Code: YOUR_APP_CODE
X-Tenant-Id: YOUR_TENANT_ID
```

或者使用简化版本：
```http
AppCode: YOUR_APP_CODE
TenantId: YOUR_TENANT_ID
```

### 2. 自动识别机制

系统会按以下优先级获取 AppCode 和 TenantId：

#### AppCode 获取顺序：
1. `X-App-Code` 请求头
2. `AppCode` 请求头
3. URL 查询参数 `appCode`
4. JWT Token 中的 `appCode` 声明
5. 从请求路径推断（如 `/api/mobile/` → `MOBILE`）
6. 从子域名推断
7. 默认值 `DEFAULT`

#### TenantId 获取顺序：
1. `X-Tenant-Id` 请求头
2. `TenantId` 请求头
3. URL 查询参数 `tenantId`
4. JWT Token 中的 `tenantId` 声明
5. 从用户邮箱域名推断
6. 默认值 `DEFAULT`

### 3. 数据库查询自动过滤

所有数据库查询会自动添加以下过滤条件：
```sql
WHERE app_code = 'CURRENT_APP_CODE' AND tenant_id = 'CURRENT_TENANT_ID'
```

### 4. 实体创建自动设置

创建新实体时，系统会自动设置：
```csharp
entity.AppCode = userContext.AppCode;
entity.TenantId = userContext.TenantId;
```

## 配置说明

### 1. 中间件注册

在 `Program.cs` 中注册中间件：
```csharp
app.UseMiddleware<FlowFlex.WebApi.Middlewares.AppIsolationMiddleware>();
app.UseMiddleware<FlowFlex.WebApi.Middlewares.TenantMiddleware>();
```

### 2. 数据库过滤器配置

在 `ServiceCollectionExtensions.cs` 中配置：
```csharp
FlowFlex.Infrastructure.Data.AppTenantFilter.ConfigureFilters(provider, httpContextAccessor);
```

### 3. 域名映射配置

在 `AppIsolationMiddleware.cs` 中配置域名到租户的映射：
```csharp
private string MapDomainToTenantId(string domain)
{
    return domain switch
    {
        "company1.com" => "COMPANY1",
        "company2.com" => "COMPANY2",
        "test.com" => "TEST",
        _ => null
    };
}
```

## 数据库迁移

### 1. 执行迁移脚本

运行以下 SQL 脚本添加 `app_code` 列：
```sql
-- 在 PostgreSQL 中执行
\i packages/flowFlex-backend/SqlSugarDB/Migrations/20241219_AddAppCodeColumn.sql
```

### 2. 验证迁移

检查所有表是否都有 `app_code` 列：
```sql
SELECT table_name, column_name, data_type, column_default
FROM information_schema.columns
WHERE column_name = 'app_code'
AND table_schema = 'public'
ORDER BY table_name;
```

## 开发指南

### 1. 创建新实体

```csharp
// 使用扩展方法自动设置隔离字段
var entity = new YourEntity();
entity.InitCreateInfo(userContext);
```

### 2. 跨租户查询

如需查询所有租户的数据：
```csharp
using (var scope = AppTenantFilter.CreateFilterDisabledScope(db))
{
    var allData = await db.Queryable<YourEntity>().ToListAsync();
}
```

### 3. 获取当前上下文

```csharp
public class YourController : BaseController
{
    public async Task<IActionResult> YourAction()
    {
        var appContext = HttpContext.Items["AppContext"] as AppContext;
        var currentAppCode = appContext?.AppCode;
        var currentTenantId = appContext?.TenantId;
        
        // 或者通过 UserContext
        var userContext = HttpContext.RequestServices.GetService<UserContext>();
        var appCode = userContext?.AppCode;
        var tenantId = userContext?.TenantId;
    }
}
```

## 测试示例

### 1. 使用 Postman 测试

```http
GET /api/ow/workflows/v1
Headers:
  X-App-Code: MOBILE
  X-Tenant-Id: COMPANY1
  Authorization: Bearer your-jwt-token
```

### 2. 使用 curl 测试

```bash
curl -X GET "https://localhost:5019/api/ow/workflows/v1" \
  -H "X-App-Code: WEB" \
  -H "X-Tenant-Id: COMPANY2" \
  -H "Authorization: Bearer your-jwt-token"
```

## 监控和日志

### 1. 请求日志

系统会记录每个请求的隔离信息：
```
[AppIsolationMiddleware] Request: GET /api/ow/workflows/v1, AppCode: MOBILE, TenantId: COMPANY1, RequestId: b787126f
```

### 2. 响应头

系统会在响应中添加调试头：
```http
X-Response-App-Code: MOBILE
X-Response-Tenant-Id: COMPANY1
X-Response-Request-Id: b787126f
```

## 故障排除

### 1. 常见错误

#### 错误：column "app_code" does not exist
**原因**：数据库迁移未执行
**解决**：执行迁移脚本 `20241219_AddAppCodeColumn.sql`

#### 错误：Invalid application context
**原因**：请求缺少必要的头部信息
**解决**：确保请求包含 `X-App-Code` 和 `X-Tenant-Id` 头部

### 2. 调试技巧

1. 检查请求头：确认客户端发送了正确的隔离头部
2. 查看日志：检查中间件是否正确识别了 AppCode 和 TenantId
3. 验证数据库：确认数据库表有正确的隔离字段和索引
4. 测试过滤器：验证数据库查询是否自动添加了过滤条件

## 安全考虑

1. **验证隔离参数**：确保用户只能访问其有权限的应用和租户数据
2. **审计日志**：记录所有跨租户访问尝试
3. **输入验证**：验证 AppCode 和 TenantId 格式和长度
4. **权限检查**：结合 JWT 令牌验证用户权限

## 性能优化

1. **索引优化**：为 `(app_code, tenant_id)` 创建复合索引
2. **查询优化**：避免不必要的跨租户查询
3. **缓存策略**：考虑为租户级别的数据添加缓存
4. **连接池**：为不同租户使用独立的数据库连接池（如需要） 