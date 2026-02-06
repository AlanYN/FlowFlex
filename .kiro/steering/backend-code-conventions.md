# Code Conventions

## Project Architecture

FlowFlex 后端采用分层架构（Clean Architecture），各层职责如下：

| Layer | Project | Responsibility |
|-------|---------|---------------|
| API | `WebApi` | Controllers, Middlewares, Filters, API routing |
| Application | `Application` | Service implementations, Maps (AutoMapper), Helpers |
| Application Contracts | `Application.Contracts` | Service interfaces (`IServices`), DTOs (`Dtos`), Options |
| Domain | `Domain` | Entities, Repository interfaces, Domain abstractions |
| Domain Shared | `Domain.Shared` | Shared models, Exceptions, Enums, Constants, Helpers |
| Infrastructure | `Infrastructure` | Cross-cutting concerns, Extensions, External service integrations |
| Data Access | `SqlSugarDB` | SqlSugar ORM configuration, Repository implementations, Migrations |

### Namespace Convention

```csharp
// Good ✅ - Namespace matches folder structure
namespace FlowFlex.WebApi.Controllers.OW
namespace FlowFlex.Application.Services.OW
namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
namespace FlowFlex.Domain.Entities.OW
namespace FlowFlex.Domain.Shared.Models

// Bad ❌ - Namespace doesn't match folder
namespace FlowFlex.Services
namespace MyApp.Controllers
```

---

## Controller Convention

### Routing

- OW 模块路由格式：`[Route("ow/{resource}/v{version:apiVersion}")]`
- 使用 `[ApiController]` 和 `[Asp.Versioning.ApiVersion("1.0")]`
- 所有 Controller 继承 `Controllers.ControllerBase`

```csharp
// Good ✅
[ApiController]
[Route("ow/onboardings/v{version:apiVersion}")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class OnboardingController : Controllers.ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateAsync([FromBody] OnboardingInputDto input)
    {
        long result = await _onboardingService.CreateAsync(input);
        return Success(result);
    }
}
```

### Response Pattern

- 成功响应统一使用 `ControllerBase` 提供的 `Success()` 或 `Success<T>(data)` 方法
- 不要直接返回 `Ok()` 或手动构造响应对象

```csharp
// Good ✅
return Success(result);
return Success();

// Bad ❌
return Ok(result);
return Ok(new { code = 200, data = result });
```

### Permission Attributes

- 使用 `[WFEAuthorize(PermissionConsts.Case.Read)]` 进行功能权限控制
- 使用 `[RequirePermission(PermissionEntityTypeEnum.Case, OperationTypeEnum.View)]` 进行数据权限控制
- 使用 `[PortalAccess]` 允许 Portal Token 访问
- 使用 `[AllowAnonymous]` 允许匿名访问
- 使用 `[RateLimit(maxRequests: 5, windowSeconds: 60, keyPrefix: "register")]` 进行速率限制

### API Documentation

- 每个 Action 方法必须有 `/// <summary>` XML 注释
- 使用 `[ProducesResponseType]` 声明响应类型

```csharp
/// <summary>
/// Get onboarding by ID
/// Requires CASE:READ permission
/// </summary>
[HttpGet("{id}")]
[WFEAuthorize(PermissionConsts.Case.Read)]
[ProducesResponseType<SuccessResponse<OnboardingOutputDto>>((int)HttpStatusCode.OK)]
public async Task<IActionResult> GetByIdAsync(long id)
```

---

## Service Convention

### Interface Definition

- 服务接口定义在 `Application.Contracts/IServices/` 目录
- 接口命名：`I{ServiceName}Service`
- 接口继承 `IScopedService`（默认）、`ITransientService` 或 `ISingletonService` 标记生命周期

```csharp
// Good ✅
public interface IOnboardingService : IScopedService
{
    Task<long> CreateAsync(OnboardingInputDto input);
    Task<bool> UpdateAsync(long id, OnboardingInputDto input);
    Task<bool> DeleteAsync(long id, bool confirm = false);
    Task<OnboardingOutputDto?> GetByIdAsync(long id);
}
```

### Service Implementation

- 实现类在 `Application/Services/` 目录
- 实现类同时实现接口和生命周期标记接口
- 构造函数注入所有依赖
- 异步方法使用 `Async` 后缀

```csharp
// Good ✅
public class UserService : IUserService, IScopedService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserDto> GetByIdAsync(long id)
    {
        // ...
    }
}
```

### Dependency Injection Lifecycle

项目使用标记接口自动注册服务，无需手动在 `Startup` 中注册：

| Interface | Lifecycle | Usage |
|-----------|-----------|-------|
| `IScopedService` | Scoped | 大多数业务服务（默认选择） |
| `ITransientService` | Transient | 无状态的轻量级服务 |
| `ISingletonService` | Singleton | 全局共享的服务（如缓存） |

```csharp
// Good ✅ - Service auto-registered via marker interface
public class OnboardingService : IOnboardingService, IScopedService { }

// Bad ❌ - Missing lifecycle marker, won't be auto-registered
public class OnboardingService : IOnboardingService { }
```

---

## Entity Convention

### Entity Base Classes

项目有两套实体基类体系：

1. **OW 模块实体**：继承 `OwEntityBase`（使用雪花 ID，包含审计字段和租户隔离字段）
2. **通用实体**：继承 `EntityBaseCreateInfo` → `IdEntityBase` → `AbstractEntityBase`

```csharp
// Good ✅ - OW module entity
[Table("ff_users")]
[SugarTable("ff_users")]
public class User : OwEntityBase
{
    [MaxLength(50)]
    public string Username { get; set; }

    [MaxLength(100)]
    public string Email { get; set; }
}
```

### Table Naming

- 表名前缀：`ff_`（FlowFlex）
- 表名使用 snake_case：`ff_users`, `ff_onboarding`
- 列名自动转换为 snake_case（通过 SqlSugar `EntityService` 配置）
- 使用 `[SugarTable("ff_table_name")]` 显式指定表名
- 使用 `[SugarColumn(ColumnName = "column_name")]` 显式指定列名（仅在需要覆盖自动转换时）

### Entity Initialization

创建实体时使用扩展方法初始化审计信息：

```csharp
// Good ✅
var entity = new Onboarding();
entity.InitCreateInfo(userContext);  // Sets Id, CreateDate, ModifyDate, CreateBy, ModifyBy, TenantId, AppCode

// Good ✅ - Update
entity.InitUpdateInfo(userContext);  // Sets ModifyDate, ModifyBy, ModifyUserId

// Bad ❌ - Manual initialization
entity.Id = someId;
entity.CreateDate = DateTime.Now;
entity.TenantId = "some-tenant";
```

### ID Generation

- 使用雪花 ID（Snowflake ID）作为主键
- 类型为 `long`
- JSON 序列化时使用 `LongToStringConverter` 转为字符串（避免 JavaScript 精度丢失）

```csharp
[Key]
[SugarColumn(IsPrimaryKey = true)]
[JsonConverter(typeof(LongToStringConverter))]
public long Id { get; set; }
```

### Timestamp Convention

- 所有时间字段使用 `DateTimeOffset` 类型
- 存储和比较使用 UTC 时间：`DateTimeOffset.UtcNow`
- 不要使用 `DateTime.Now` 或 `DateTime.UtcNow`

```csharp
// Good ✅
public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;

// Bad ❌
public DateTime CreateDate { get; set; } = DateTime.Now;
```

---

## DTO Convention

### Naming

- DTO 定义在 `Application.Contracts/Dtos/` 目录
- 按模块和实体组织子目录：`Dtos/OW/Onboarding/`, `Dtos/OW/User/`
- 命名规则：
  - 输入 DTO：`{Entity}InputDto` 或 `{Action}{Entity}RequestDto`
  - 输出 DTO：`{Entity}OutputDto` 或 `{Entity}Dto`
  - 查询 DTO：`{Entity}QueryRequest`

```csharp
// Good ✅
public class OnboardingInputDto { }
public class OnboardingOutputDto { }
public class OnboardingQueryRequest { }
public class RegisterRequestDto { }
public class LoginResponseDto { }

// Bad ❌
public class OnboardingModel { }
public class OnboardingVM { }
public class CreateOnboarding { }
```

### Validation

- 使用 `DataAnnotations` 进行输入验证
- 验证错误消息使用英文

```csharp
// Good ✅
[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Invalid email format")]
[StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
public string Email { get; set; }
```

### Pagination

- 分页查询返回 `PageModelDto<T>` 或 `PagedResult<T>`
- 分页参数：`pageIndex`（从 1 开始）、`pageSize`（默认 20，最大 100）

---

## TenantId and AppCode Source Convention

For `/api/ow/` endpoints, the CRUD operations must get TenantId and AppCode from HTTP headers:

- **TenantId**: Get from `X-Tenant-Id` header
- **AppCode**: Get from `X-App-Code` header

### Implementation

The `UserContext` is automatically populated from HTTP headers in `ServiceCollectionExtensions.cs`:

```csharp
// Priority: headers > JWT claims > AppContext > defaults
var tenantId = tenantIdHeader ?? tenantIdClaim?.Value ?? appContext?.TenantId ?? "default";
var appCode = appCodeHeader ?? appCodeClaim?.Value ?? appContext?.AppCode ?? "default";
```

### Usage in Services

Services should use `UserContext` to get TenantId and AppCode:

```csharp
// Good ✅
entity.TenantId = _userContext.TenantId ?? "default";
entity.AppCode = _userContext.AppCode ?? "default";

// Bad ❌ - Hardcoding values
entity.TenantId = "some-tenant";
entity.AppCode = "some-app";
```

### Query Filtering

Always filter queries by TenantId for tenant isolation:

```csharp
// Good ✅
var entities = await _db.Queryable<Entity>()
    .Where(e => e.TenantId == _userContext.TenantId)
    .ToListAsync();

// Bad ❌ - No tenant filtering
var entities = await _db.Queryable<Entity>()
    .ToListAsync();
```

---

## AppCode Naming Convention

- **NEVER** use uppercase `"DEFAULT"` for AppCode values in code
- **ALWAYS** use lowercase `"default"` for AppCode values
- This applies to:
  - Entity default values
  - Constants
  - String literals
  - Configuration values

### Examples

#### Good ✅
```csharp
public string AppCode { get; set; } = "default";

if (appCode == "default")
{
    // ...
}

const string DEFAULT_APP_CODE = "default";
```

#### Bad ❌
```csharp
public string AppCode { get; set; } = "DEFAULT";

if (appCode == "DEFAULT")
{
    // ...
}

const string DEFAULT_APP_CODE = "DEFAULT";
```

### Reason
- Consistency with database values
- Avoid case-sensitivity issues in queries
- Maintain uniform naming convention across the codebase

---

## Exception Handling Convention

### CRMException

项目使用 `CRMException` 作为业务异常类，配合 `ErrorCodeEnum` 使用：

```csharp
// Good ✅ - Throw with error code
throw new CRMException(ErrorCodeEnum.DataIsNullError, id.ToString());

// Good ✅ - Throw with error code and custom message
throw new CRMException("Custom error message", ErrorCodeEnum.BadRequest);

// Good ✅ - Throw with HTTP status code
throw new CRMException(HttpStatusCode.Forbidden, "Access denied");

// Bad ❌ - Throw generic exception for business errors
throw new Exception("Data not found");
throw new InvalidOperationException("Bad request");
```

### ErrorCodeEnum

- 错误码定义在 `Domain.Shared/Exceptions/ErrorCode.cs`
- 使用 `[EnumValue(Description = "...")]` 定义错误消息模板
- 消息格式支持 `|` 分隔多语言：`"default message|detailed message with {0}"`

### Global Exception Handling

- `GlobalExceptionHandlingMiddleware` 统一处理所有未捕获异常
- `CRMException` 会被转换为对应的 HTTP 状态码和错误响应
- 其他异常返回 500 Internal Server Error

---

## Logging Convention

- 使用 `ILogger<T>` 进行日志记录
- 使用结构化日志（Structured Logging）
- 日志级别：`LogDebug` → `LogInformation` → `LogWarning` → `LogError`

```csharp
// Good ✅ - Structured logging with named parameters
_logger.LogInformation("RegisterAsync called for email: {Email}, SkipEmailVerification: {SkipEmailVerification}",
    request.Email, request.SkipEmailVerification);

_logger.LogWarning(ex, "Failed to start API logging");

_logger.LogDebug("[TenantMiddleware] Request: {Method} {Path}, TenantId: {TenantId}",
    context.Request.Method, context.Request.Path, tenantId);

// Bad ❌ - String interpolation (loses structured logging benefits)
_logger.LogInformation($"RegisterAsync called for email: {request.Email}");

// Bad ❌ - No context information
_logger.LogInformation("Method called");
```

---

## Repository Convention

### Interface

- 仓储接口定义在 `Domain/Repository/` 目录
- 继承 `IBaseRepository<T>` 获取通用 CRUD 方法
- 特定查询方法在子接口中定义

```csharp
// Good ✅
public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetByTenantIdAsync(string tenantId);
}
```

### Implementation

- 仓储实现在 `SqlSugarDB/Implements/` 目录
- 使用 SqlSugar ORM 进行数据访问
- 查询必须包含租户过滤

---

## AutoMapper Convention

- Map Profile 定义在 `Application/Maps/` 目录
- 命名：`{Entity}MapProfile`
- 继承 `AutoMapper.Profile`

```csharp
// Good ✅
public class OnboardingMapProfile : Profile
{
    public OnboardingMapProfile()
    {
        CreateMap<Onboarding, OnboardingOutputDto>();
        CreateMap<OnboardingInputDto, Onboarding>();
    }
}
```

---

## Migration File Naming Convention

Database migration files in `SqlSugarDB/Migrations` folder must follow a consistent naming pattern.

### Naming Format

```
Migration_{YYYYMMDDHHMMSS}_{DescriptiveName}.cs
```

Or for special cases (initial create, seed data, etc.):

```
{DescriptiveName}_{YYYYMMDDHHMMSS}.cs
```

### Rules

1. **Timestamp Format**: Use `YYYYMMDDHHMMSS` format (e.g., `20260108000002`)
   - Year (4 digits) + Month (2 digits) + Day (2 digits) + Hour (2 digits) + Minute (2 digits) + Second (2 digits)
   - For most migrations, use `000001`, `000002`, etc. for the time portion within the same day

2. **Descriptive Name**: Use PascalCase with clear description of what the migration does
   - Good: `AddIsExternalImportToOnboardingFile`, `CreateIntegrationTables`, `AddCaseCodeToOnboarding`
   - Bad: `Update1`, `Fix`, `Changes`

3. **Class Name**: Must match the file name (without `.cs` extension)
   - File: `Migration_20260108000002_AddIsExternalImportToOnboardingFile.cs`
   - Class: `Migration_20260108000002_AddIsExternalImportToOnboardingFile`

4. **Subfolder Organization**: Domain-specific migrations can be placed in subfolders
   - Example: `SqlSugarDB/Migrations/Integration/Migration_20260108000001_CreateIntegrationApiLogTable.cs`

### Examples

#### Good ✅
```
Migration_20260108000001_CreateIntegrationApiLogTable.cs
Migration_20260108000002_AddIsExternalImportToOnboardingFile.cs
Migration_20251124000001_CreateIntegrationTables.cs
Migration_20251105000001_AddCaseCodeToOnboarding.cs
```

#### Bad ❌
```
Migration1.cs
AddNewColumn.cs
20260108_Update.cs
migration_20260108000001_createtable.cs  // lowercase
```

### Registration in MigrationManager

All migrations must be registered in `MigrationManager.cs`:

```csharp
var migrations = new[]
{
    // ... existing migrations
    ("20260108000002_AddIsExternalImportToOnboardingFile", (Action)(() => Migration_20260108000002_AddIsExternalImportToOnboardingFile.Up(_db)))
};
```

- The migration ID string should match the timestamp and name portion of the file
- Migrations are executed in the order they appear in the array
- Keep migrations in chronological order

---

## Middleware Convention

- 中间件定义在 `WebApi/Middlewares/` 或 `Infrastructure/Exceptions/` 目录
- 构造函数注入 `RequestDelegate _next`
- 方法签名：`public async Task InvokeAsync(HttpContext context, ...)`
- Scoped 依赖通过方法参数注入（不是构造函数）

```csharp
// Good ✅
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Scoped services injected via method parameter
    public async Task InvokeAsync(HttpContext context, IPortalTokenService portalTokenService)
    {
        // middleware logic
        await _next(context);
    }
}
```

---

## Filter Convention

- Action Filter 实现 `IAsyncActionFilter` 接口
- 定义在 `WebApi/Filters/` 目录
- 通过 `[ServiceFilter(typeof(FilterName))]` 应用到 Controller 或 Action

```csharp
// Good ✅
public class IntegrationApiLogFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // pre-action logic
        var resultContext = await next();
        // post-action logic
    }
}
```

---

## Extension Method Convention

- 扩展方法定义在对应层的 `Extensions/` 目录
- 类名：`{Target}Extensions` 或 `{Feature}Extensions`
- 使用 `static class` 和 `this` 参数

```csharp
// Good ✅
public static class HttpContextExtensions
{
    public static string GetClientIpAddress(this HttpContext? context)
    {
        // ...
    }
}

public static class EntityBaseCreateInfoExtension
{
    public static void InitCreateInfo(this EntityBaseCreateInfo createInfo, UserContext userContext)
    {
        // ...
    }
}
```

---

## Configuration Options Convention

- Options 类定义在 `Application.Contracts/Options/` 目录
- 使用 `IOptions<T>` 或 `IOptionsSnapshot<T>` 注入
- 在 `ServiceCollectionExtensions` 中通过 `AddOptions<T>().Bind().ValidateDataAnnotations().ValidateOnStart()` 注册

```csharp
// Good ✅
public class DatabaseOptions
{
    public const string SectionName = "Database";
    
    [Required]
    public string ConnectionString { get; set; }
    public string ConfigId { get; set; } = "FlowFlex";
    public string DbType { get; set; } = "PostgreSQL";
}

// Registration
services.AddOptions<DatabaseOptions>()
    .Bind(configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```
