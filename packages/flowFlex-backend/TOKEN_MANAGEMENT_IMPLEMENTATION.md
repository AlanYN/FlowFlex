# Token Management System Implementation

## 概述

本文档描述了FlowFlex系统中新实现的JWT token管理系统，该系统解决了以下问题：

1. **重新登录后，原有token没有失效**
2. **refresh-access-token后，原有token没有失效**
3. **所有token记录在access-token表中，只有最新的token有效**

## 系统架构

### 1. 数据库表结构

#### ff_access_tokens 表

| 字段名 | 类型 | 描述 |
|--------|------|------|
| id | BIGINT | 主键 |
| tenant_id | VARCHAR(32) | 租户ID |
| app_code | VARCHAR(32) | 应用代码 |
| jti | VARCHAR(100) | JWT ID (唯一标识符) |
| user_id | BIGINT | 用户ID |
| user_email | VARCHAR(100) | 用户邮箱 |
| token_hash | VARCHAR(500) | Token哈希值 (安全存储) |
| issued_at | TIMESTAMPTZ | Token发放时间 |
| expires_at | TIMESTAMPTZ | Token过期时间 |
| is_active | BOOLEAN | Token是否活跃 |
| token_type | VARCHAR(20) | Token类型 (login, refresh, portal-access) |
| revoked_at | TIMESTAMPTZ | Token撤销时间 |
| revoke_reason | VARCHAR(50) | 撤销原因 |
| last_used_at | TIMESTAMPTZ | 最后使用时间 |
| issued_ip | VARCHAR(45) | 发放IP地址 |
| user_agent | VARCHAR(500) | 用户代理 |

### 2. 关键组件

#### 2.1 实体类
- **AccessToken**: 表示access token表的实体类

#### 2.2 仓储层
- **IAccessTokenRepository**: Access token仓储接口
- **AccessTokenRepository**: Access token仓储实现

#### 2.3 服务层
- **IAccessTokenService**: Access token业务服务接口
- **AccessTokenService**: Access token业务服务实现
- **IJwtService**: JWT服务接口 (扩展了新方法)
- **JwtService**: JWT服务实现 (添加了token详细信息生成)

#### 2.4 DTO
- **TokenDetailsDto**: Token详细信息传输对象

#### 2.5 中间件
- **JwtAuthenticationMiddleware**: JWT认证中间件 (添加了数据库验证)

## 实现细节

### 1. Token生成流程

```csharp
// 1. 生成带详细信息的token
var tokenDetails = _jwtService.GenerateTokenWithDetails(
    userId, email, username, tenantId, tokenType);

// 2. 记录token到数据库并撤销其他token
await _accessTokenService.RecordTokenAsync(
    tokenDetails.Jti,
    tokenDetails.UserId,
    tokenDetails.UserEmail,
    tokenDetails.Token,
    tokenDetails.ExpiresAt,
    tokenDetails.TokenType,
    tokenDetails.IssuedIp,
    tokenDetails.UserAgent,
    revokeOtherTokens: true  // 撤销其他活跃token
);
```

### 2. Token验证流程

```csharp
// 1. JWT签名和过期时间验证
var principal = ValidateToken(token);

// 2. 数据库中验证token是否活跃
var isTokenActive = await ValidateTokenInDatabaseAsync(context, token);

// 3. 更新最后使用时间
await accessTokenService.UpdateTokenUsageAsync(jti);
```

### 3. Token刷新流程

```csharp
// 1. 验证旧token是否有效
var isTokenValid = await _accessTokenService.ValidateTokenAsync(oldJti);

// 2. 生成新token
var newTokenDetails = _jwtService.GenerateTokenWithDetails(...);

// 3. 记录新token并撤销旧token
await _accessTokenService.RecordTokenAsync(..., revokeOtherTokens: true);
await _accessTokenService.RevokeTokenAsync(oldJti, "refresh");
```

## API端点

### 1. 登录相关
- `POST /api/ow/users/login` - 用户登录 (会撤销其他token)
- `POST /api/ow/users/login-with-code` - 验证码登录 (会撤销其他token)
- `POST /api/ow/users/refresh-access-token` - 刷新token (会撤销旧token)

### 2. 登出相关
- `POST /api/ow/users/logout` - 退出登录 (撤销当前token)
- `POST /api/ow/users/logout-all-devices` - 退出所有设备 (撤销用户所有token)

## 数据库迁移

### 迁移文件
- `20250101000010_CreateAccessTokenTable.cs`

### 执行迁移
系统启动时会自动执行迁移，创建 `ff_access_tokens` 表及相关索引。

## 安全特性

### 1. Token安全存储
- Token值经过SHA256哈希后存储
- 不会在数据库中存储明文token

### 2. 自动清理
- 系统会自动清理过期的token记录
- 保留已撤销token 7天用于审计

### 3. 细粒度控制
- 支持按设备/会话独立管理token
- 支持强制退出所有设备
- 记录详细的token使用日志

## 配置选项

### JWT配置 (appsettings.json)
```json
{
  "Security": {
    "JwtSecretKey": "your-secret-key",
    "JwtIssuer": "FlowFlex",
    "JwtAudience": "FlowFlex.Client",
    "JwtExpiryMinutes": 1440
  }
}
```

## 性能优化

### 1. 数据库索引
- `idx_access_tokens_jti`: JTI唯一索引
- `idx_access_tokens_user_active`: 用户活跃token复合索引
- `idx_access_tokens_cleanup`: 清理操作优化索引

### 2. 缓存策略
- JWT中间件中使用服务作用域避免重复查询
- Token验证结果可以结合Redis缓存进一步优化

## 测试验证

### 1. 登录测试
1. 用户A在设备1登录 → 获得token1
2. 用户A在设备2登录 → 获得token2，token1被撤销
3. 使用token1访问API → 401 Unauthorized
4. 使用token2访问API → 成功

### 2. 刷新测试
1. 用户使用token2刷新 → 获得token3，token2被撤销
2. 使用token2访问API → 401 Unauthorized
3. 使用token3访问API → 成功

### 3. 登出测试
1. 用户主动退出 → token3被撤销
2. 使用token3访问API → 401 Unauthorized

## 故障排查

### 1. 常见问题
- **Token验证失败**: 检查数据库中token是否为active状态
- **迁移失败**: 确保数据库连接正常，检查迁移日志
- **服务注册问题**: 确保实现了正确的生命周期接口

### 2. 日志监控
系统会记录以下关键日志：
- Token创建和撤销操作
- Token验证失败原因
- 数据库操作异常

## 总结

本token管理系统实现了以下目标：
1. ✅ 重新登录时自动撤销旧token
2. ✅ 刷新token时自动撤销旧token  
3. ✅ 所有token记录在数据库中统一管理
4. ✅ 只有最新的token保持有效状态
5. ✅ 提供灵活的token撤销和管理功能

该系统确保了用户会话的安全性，防止了token重放攻击，并提供了完整的token生命周期管理。 