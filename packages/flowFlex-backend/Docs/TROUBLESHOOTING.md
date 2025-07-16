# FlowFlex 故障排除指南

## 常见问题与解决方案

### 1. PostgreSQL 连接问题

#### 问题描述
```
FlowFlex.Domain.Shared.CRMException: Error querying onboardings: Received backend message BindComplete while expecting ReadyForQueryMessage.
```

#### 可能原因
1. PostgreSQL连接字符串格式错误
2. 数据库服务未启动
3. 连接池配置问题
4. 版本兼容性问题

#### 解决方案

**步骤1：检查数据库服务状态**
```bash
# Windows
net start postgresql-x64-15

# Linux/macOS
sudo systemctl start postgresql
# 或
brew services start postgresql
```

**步骤2：验证连接字符串**
确保 `appsettings.json` 中的连接字符串格式正确：
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=flowflex;Username=flowflex;Password=123456;Pooling=true;MinPoolSize=1;MaxPoolSize=20;ConnectionLifetime=0;CommandTimeout=30;"
  }
}
```

**步骤3：测试数据库连接**
访问健康检查端点：
```
GET https://localhost:5019/api/health/database
```

**步骤4：检查PostgreSQL日志**
```bash
# 查看PostgreSQL日志
tail -f /var/log/postgresql/postgresql-15-main.log
```

### 2. JWT Token 过期问题

#### 问题描述
```
Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException: IDX10223: Lifetime validation failed. The token is expired.
```

#### 解决方案

**步骤1：检查token过期时间**
在 `appsettings.json` 中调整：
```json
{
  "Security": {
    "JwtExpiryMinutes": 1440
  }
}
```

**步骤2：前端token刷新**
确保前端实现了token自动刷新机制，或在token过期时引导用户重新登录。

### 3. 请求超时问题

#### 问题描述
接口请求超时，前端显示"接口请求超时"

#### 解决方案

**步骤1：检查数据库查询性能**
```sql
-- 检查慢查询
SELECT query, mean_exec_time, calls 
FROM pg_stat_statements 
WHERE mean_exec_time > 1000
ORDER BY mean_exec_time DESC;
```

**步骤2：优化数据库索引**
```sql
-- 为常用查询字段添加索引
CREATE INDEX IF NOT EXISTS idx_onboardings_tenant_app ON onboardings(tenant_id, app_code);
CREATE INDEX IF NOT EXISTS idx_onboardings_workflow_id ON onboardings(workflow_id);
CREATE INDEX IF NOT EXISTS idx_onboardings_create_date ON onboardings(create_date DESC);
```

**步骤3：调整超时配置**
- 数据库命令超时：30秒（已配置）
- HTTP请求超时：60秒（已修复）

### 4. 应用启动问题

#### 问题描述
应用启动时出现配置验证错误

#### 解决方案

**步骤1：验证配置文件**
确保 `appsettings.json` 中所有必需的配置项都已正确设置：
```json
{
  "Database": {
    "ConnectionString": "必需项",
    "ConfigId": "FlowFlex",
    "DbType": "PostgreSQL"
  },
  "Security": {
    "JwtSecretKey": "至少32个字符的密钥",
    "JwtIssuer": "FlowFlex",
    "JwtAudience": "FlowFlex.Client"
  }
}
```

**步骤2：检查环境变量**
某些设置可以通过环境变量覆盖：
```bash
export Database__ConnectionString="Host=localhost;Port=5432;..."
export Security__JwtSecretKey="YourSecretKey"
```

### 5. 性能问题

#### 诊断工具

**使用健康检查端点**
```
GET https://localhost:5019/api/health/detailed
```

**监控SQL查询**
在 `appsettings.json` 中启用SQL日志：
```json
{
  "Database": {
    "EnableSqlLogging": true,
    "EnablePerformanceLogging": true
  }
}
```

### 6. 错误日志分析

#### 查看应用日志
```bash
# 查看应用日志文件
tail -f logs/app-{date}.log

# 实时查看控制台输出
dotnet run --project WebApi/WebApi.csproj
```

#### 常见错误代码
- **400**: 请求参数错误
- **401**: 认证失败（通常是JWT问题）
- **403**: 权限不足
- **500**: 内部服务器错误（通常是数据库或配置问题）
- **503**: 服务不可用（通常是数据库连接问题）

## 调试技巧

### 1. 启用详细日志
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 2. 使用Swagger测试API
访问 `https://localhost:5019/swagger` 测试各个API端点

### 3. 数据库直接查询
```sql
-- 检查表是否存在
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';

-- 检查onboarding表数据
SELECT COUNT(*) FROM onboardings;
SELECT * FROM onboardings LIMIT 5;
```

## 联系支持

如果以上解决方案都无法解决问题，请提供以下信息：

1. 错误的完整堆栈跟踪
2. 相关的应用程序日志
3. 数据库连接字符串（隐藏密码）
4. 操作系统和.NET版本信息
5. 复现问题的步骤

## 快速修复检查清单

- [ ] 数据库服务是否运行？
- [ ] 连接字符串格式是否正确？
- [ ] JWT密钥是否至少32个字符？
- [ ] 所有必需的配置项是否已设置？
- [ ] 数据库中是否有数据？
- [ ] 防火墙是否阻止了端口5432？
- [ ] PostgreSQL用户权限是否正确？ 