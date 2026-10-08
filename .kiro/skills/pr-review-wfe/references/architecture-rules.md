# FlowFlex 架构规则参考

## 分层架构

```
WebApi → Application → Domain ← SqlSugarDB
                    ↑
               Infrastructure
```

### 各层职责与约束

#### WebApi 层 (`packages/flowFlex-backend/WebApi/`)

- Controller 只调用 `IService` 接口，禁止直接调用 Repository 或操作 DbContext
- 返回值必须使用 `Success<T>(data)` 或 `Success()`，禁止裸 `Ok()`
- Controller 必须继承 `ControllerBase`
- 路由前缀规则：
  - `Controllers/OW/` → 前缀 `ow/`（如 `ow/workflows/v1`）
  - `Controllers/Integration/` → 前缀 `integration/`
  - `Controllers/AI/` → 前缀 `ai/`
  - `Controllers/Action/` → 前缀 `action/`
  - `Controllers/Shared/` → 前缀 `shared/`
  - `Controllers/MessageCenter/` → 前缀 `ow/`

#### Application 层 (`packages/flowFlex-backend/Application/`)

- Service 必须实现对应的 `I{Name}Service` 接口
- Service 必须同时实现 `IScopedService` / `ISingletonService` / `ITransientService` 之一（DI 自动注册标记）
- 禁止 Service 直接依赖其他 Service 的具体类，必须通过接口
- DTO 映射必须通过 AutoMapper Profile，禁止手动赋值大量字段

#### Application.Contracts 层 (`packages/flowFlex-backend/Application.Contracts/`)

- 只包含接口 (`IServices/`) 和 DTO (`Dtos/`)，禁止包含业务逻辑实现

#### Domain 层 (`packages/flowFlex-backend/Domain/`)

- Entity 必须继承 `EntityBaseCreateInfo`（包含公共审计字段）
- Repository 接口继承 `IBaseRepository<T>`
- 禁止 Domain 层引用 Application 或 WebApi

#### SqlSugarDB 层 (`packages/flowFlex-backend/SqlSugarDB/`)

- Repository 实现继承 `BaseRepository<T>`
- ORM 使用 SqlSugar，不使用 EF Core
- 列名自动 snake_case 转换（`ToUnderLine()`）
- 所有表名以 `ff_` 为前缀

---

## 多租户规则

- 每个 Entity 必须包含 `AppCode`（string）和 `TenantId`（long）字段
- SqlSugar 全局过滤器自动注入，**禁止在无充分理由的情况下调用 `.Filter(null, true)` 绕过**
- 跨租户操作必须显式标注注释说明原因

---

## 软删除规则

- 使用 `IsValid`（bool）标志位，`true` = 活跃，`false` = 已删除
- 禁止物理删除（`DELETE FROM`），除非有明确迁移脚本
- SqlSugar 全局过滤器 `IValidFilter` 自动过滤 `IsValid = false` 的记录

---

## ID 策略

- 主键为雪花算法 `long` 类型
- 序列化时通过 `LongToStringConverter` 转为字符串（前端 JS 精度限制）
- 禁止使用 `Guid` 或自增 `int` 作为主键

---

## 认证与鉴权

- 同时支持 3 种 JWT scheme：本地 JWT、IdentityHub (IDM)、ItemIAM
- Controller 使用 `[WFEAuthorize]` 特性（非 `[Authorize]`）
- 公开接口必须显式标注 `[AllowAnonymous]` 并附注释说明理由
- `AppCode` + `TenantId` 由 `AppIsolationMiddleware` 从 header/JWT 中提取注入

---

## 数据库约定

| 约定 | 规则 |
|------|------|
| 表前缀 | `ff_` |
| 列命名 | snake_case |
| 主键 | `id`（snowflake long） |
| 审计字段 | `create_date`, `modify_date`, `create_by`, `modify_by`, `create_user_id`, `modify_user_id` |
| 软删除 | `is_valid`（bool） |
| 多租户 | `app_code`, `tenant_id` |
| 保留字 | `order` 需转义为 `"order"` |
| 动态数据 | 使用 JSONB 列 |

---

## Migration 规则

- 手写 SQL Migration，不使用 EF Core 的 `Add-Migration`
- 文件命名：`Migration_{YYYYMMDD}{序号}_{描述}.cs`，位于 `SqlSugarDB/Migrations/`
- 必须在 `MigrationManager.cs` 的 `migrations` 数组末尾注册
- Migration 必须保证幂等性（使用 `IF NOT EXISTS` / `IF EXISTS`）
- Entity 字段改动和 Migration SQL 必须同步更新

---

## API 响应规范

- 成功响应：`Success<T>(data)` → 包裹在 `SuccessResponse` 信封中
- 业务错误：`throw new CRMException(ErrorCodeEnum.xxx, message)`
- 全局异常由 `GlobalExceptionHandlingMiddleware` 统一处理，Controller 层禁止 try-catch
- 验证：使用 FluentValidation 对请求 DTO 做输入验证

---

## 前端架构规则 (Vue 3)

### 组件规范
- 只使用 `<script setup lang="ts">`，禁止 Options API
- Props 定义：`withDefaults(defineProps<Props>(), {...})`
- Emits 定义：`defineEmits<{ eventName: [argType] }>()`
- 样式：`<style scoped lang="scss">` + Tailwind CSS 工具类

### 状态管理 (Pinia)
- Store ID 必须以 `item-wfe-app-` 为前缀
- Store 文件位置：`src/app/stores/modules/{feature}.ts`

### API 层
- 所有 HTTP 请求通过统一的 Axios 实例（自动注入 JWT + AppCode + 时区 header）
- API 函数位于 `src/app/apis/{domain}/index.ts`
- 响应由 `transformResponseHook` 自动解包 `SuccessResponse` 信封

### 路径别名
- `@/` → `src/app/`
- `#/` → `types/`

### URL 前缀（前端拼接必须带域前缀）
- OW 域：`ow/`
- Integration 域：`integration/`
- AI 域：`ai/`
- Action 域：`action/`
- Shared 域：`shared/`
