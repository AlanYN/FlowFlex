using Application.Contracts.Options;
using Application.Services.AI;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.SqlSugarDB.Context;
using FlowFlex.SqlSugarDB.Implements.OW;
using Item.BlobProvider.Extensions;
using Item.ThirdParty;
using SqlSugar;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from configuration file
            var connectionString = configuration["Database:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database:ConnectionString is not configured. Please set it in appsettings.json or environment variables.");
            }

            // Read database configuration
            var configId = configuration["Database:ConfigId"] ?? "FlowFlex";
            var dbTypeString = configuration["Database:DbType"] ?? "PostgreSQL";
            var dbType = Enum.Parse<DbType>(dbTypeString);
            var commandTimeout = configuration.GetValue<int>("Database:CommandTimeout", 30);

            // Register SqlSugar
            services.AddScoped<ISqlSugarClient>(sp =>
            {
                var config = new ConnectionConfig()
                {
                    ConfigId = configId,
                    DbType = dbType,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute,
                    LanguageType = LanguageType.English,
                    MoreSettings = new ConnMoreSettings()
                    {
                        PgSqlIsAutoToLower = false, // Prevent automatic lowercase conversion
                        IsAutoRemoveDataCache = true, // Auto remove data cache
                        IsAutoToUpper = false, // Prevent automatic uppercase conversion
                        DefaultCacheDurationInSeconds = 600, // Cache duration
                        // 优化连接池设置以避免并发冲突
                        SqlServerCodeFirstNvarchar = true,
                        IsWithNoLockQuery = false, // 禁用NoLock以确保数据一致性
                        DisableNvarchar = false,
                        EnableILike = true
                    },
                    ConfigureExternalServices = new ConfigureExternalServices()
                    {
                        EntityService = (x, p) =>
                        {
                            // Convert entity property names to snake_case database column names
                            p.DbColumnName = UtilMethods.ToUnderLine(p.DbColumnName);
                        },
                        EntityNameService = (type, entity) =>
                        {
                            // First check if there is a SugarTable attribute
                            var sugarTableAttribute = type.GetCustomAttributes(typeof(SugarTable), false)
                                .FirstOrDefault() as SugarTable;

                            if (sugarTableAttribute != null)
                            {
                                entity.DbTableName = sugarTableAttribute.TableName;
                            }
                            else
                            {
                                // Fallback: handle special table naming
                                var tableName = type.Name;
                                if (tableName.EndsWith("Entity"))
                                {
                                    tableName = tableName.Substring(0, tableName.Length - 6);
                                }

                                // Add prefix
                                entity.DbTableName = $"ff_{UtilMethods.ToUnderLine(tableName)}";
                            }
                        },
                        // Custom serializer to prevent double serialization of JSON strings
                        SerializeService = new SqlSugarJsonSerializer()
                    }
                };

                // 使用SqlSugarScope代替单例，支持并发操作
                var sqlSugarClient = new SqlSugarScope(config, provider =>
                {
                    // 配置AOP事件
                    provider.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        if (configuration.GetValue<bool>("Database:EnableSqlLogging", false))
                        {
                            Console.WriteLine($"[SQL] {sql}");
                            if (pars?.Any() == true)
                            {
                                Console.WriteLine($"[Parameters] {string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"))}");
                            }
                        }
                        var finalSql = UtilMethods.GetSqlString(DbType.PostgreSQL, sql, pars);
                    };

                    provider.Aop.OnError = (exp) =>
                    {
                        Console.WriteLine($"[SQL Error] {exp.Message}");
                    };

                    provider.Aop.DataExecuting = (oldValue, entityInfo) =>
                    {
                        try
                        {

                            if (entityInfo.OperationType != DataFilterType.InsertByObject &&
                                entityInfo.OperationType != DataFilterType.UpdateByObject)
                                return;

                            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                            UserContext userContext = null;

                            // Try to get UserContext safely
                            try
                            {
                                userContext = httpContextAccessor?.HttpContext?.RequestServices?.GetService<UserContext>();
                            }
                            catch (ObjectDisposedException)
                            {
                                // RequestServices was disposed (e.g., in background tasks), silently skip
                                return;
                            }

                            if (userContext == null) return;

                            var now = DateTimeOffset.UtcNow;
                            // Priority: FirstName + LastName > UserName > Email > "SYSTEM"
                            var firstName = userContext.FirstName?.Trim();
                            var lastName = userContext.LastName?.Trim();
                            var fullName = $"{firstName} {lastName}".Trim();
                            var userName = !string.IsNullOrEmpty(fullName) ? fullName :
                                          (!string.IsNullOrEmpty(userContext.UserName) ? userContext.UserName :
                                          (!string.IsNullOrEmpty(userContext.Email) ? userContext.Email : "SYSTEM"));
                            var userId = long.TryParse(userContext.UserId, out var parsedUserId) ? parsedUserId : 0;
                            var tenantId = userContext.TenantId ?? "default";

                            if (entityInfo.EntityColumnInfo.PropertyInfo.PropertyType == typeof(DateTimeOffset?))
                            {
                                DateTimeOffset? sourceValue = (DateTimeOffset?)entityInfo.EntityColumnInfo.PropertyInfo.GetValue(entityInfo.EntityValue);
                                if (sourceValue == DateTimeOffset.MinValue)
                                {
                                    entityInfo.SetValue(null);
                                }
                            }

                            // Check if this is a system-initialized entity (CreateBy = "SYSTEM")
                            // If so, skip auto-fill for audit fields to preserve system values
                            var createByProperty = entityInfo.EntityValue.GetType().GetProperty("CreateBy");
                            var isSystemInit = createByProperty != null && 
                                              (string)createByProperty.GetValue(entityInfo.EntityValue) == "SYSTEM";

                            if (entityInfo.OperationType == DataFilterType.InsertByObject)
                            {
                                switch (entityInfo.PropertyName)
                                {
                                    case nameof(EntityBaseCreateInfo.TenantId):
                                        if (string.IsNullOrEmpty((string)oldValue) || (string)oldValue == "0")
                                        {
                                            entityInfo.SetValue(tenantId);
                                        }
                                        break;

                                    case nameof(EntityBaseCreateInfo.CreateDate):
                                        entityInfo.SetValue(now);
                                        break;

                                    case nameof(EntityBaseCreateInfo.ModifyDate):
                                        entityInfo.SetValue(now);
                                        break;

                                    case nameof(EntityBaseCreateInfo.CreateBy):
                                        // Only set if not already specified
                                        if (string.IsNullOrEmpty((string)oldValue))
                                        {
                                            entityInfo.SetValue(userName);
                                        }
                                        break;

                                    case nameof(EntityBaseCreateInfo.ModifyBy):
                                        // Skip if system initialized, otherwise only set if empty
                                        if (!isSystemInit && string.IsNullOrEmpty((string)oldValue))
                                        {
                                            entityInfo.SetValue(userName);
                                        }
                                        break;

                                    case nameof(EntityBaseCreateInfo.CreateUserId):
                                        // Skip if system initialized
                                        if (!isSystemInit && (oldValue == null || (long)oldValue == 0))
                                        {
                                            entityInfo.SetValue(userId);
                                        }
                                        break;

                                    case nameof(EntityBaseCreateInfo.ModifyUserId):
                                        // Skip if system initialized
                                        if (!isSystemInit && (oldValue == null || (long)oldValue == 0))
                                        {
                                            entityInfo.SetValue(userId);
                                        }
                                        break;
                                }
                            }

                            if (entityInfo.OperationType == DataFilterType.UpdateByObject)
                            {
                                switch (entityInfo.PropertyName)
                                {
                                    case nameof(EntityBaseCreateInfo.ModifyDate):
                                        entityInfo.SetValue(now);
                                        break;

                                    case nameof(EntityBaseCreateInfo.ModifyBy):
                                        entityInfo.SetValue(userName);
                                        break;

                                    case nameof(EntityBaseCreateInfo.ModifyUserId):
                                        entityInfo.SetValue(userId);
                                        break;
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // ServiceProvider disposed - silently skip in background tasks
                            return;
                        }
                        catch (Exception ex)
                        {
                            // Log other errors but don't interrupt operations
                            Console.WriteLine($"[AOP Warning] Failed to set audit fields: {ex.Message}");
                        }
                    };

                    // Set command timeout using the correct API
                    provider.Ado.CommandTimeOut = commandTimeout;

                    // Note: Tenant and app filters are handled at repository level
                    // to avoid IServiceProvider disposal issues in SqlSugar configuration
                });

                return sqlSugarClient;
            });

            // Register SqlSugar context
            services.AddScoped<ISqlSugarContext, SqlSugarContext>();

            // Register UserContext - get from HTTP request headers, JWT claims, and AppContext
            services.AddScoped<UserContext>(provider =>
            {
                try
                {
                    var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
                    var httpContext = httpContextAccessor?.HttpContext;

                    if (httpContext != null)
                    {
                        // Get AppContext from middleware if available
                        var appContext = httpContext.Items["AppContext"] as AppContext;

                        // Try to get user information from JWT claims first
                        // Priority: userId claim > sub claim > NameIdentifier claim
                        var userIdClaim = httpContext.User?.FindFirst("userId")
                                        ?? httpContext.User?.FindFirst("sub")
                                        ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                        var emailClaim = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)
                                        ?? httpContext.User?.FindFirst("email");
                        var usernameClaim = httpContext.User?.FindFirst("username");
                        var tenantIdClaim = httpContext.User?.FindFirst("tenantId");
                        var appCodeClaim = httpContext.User?.FindFirst("appCode");

                        // Fallback to request headers if JWT claims are not available
                        var userIdHeader = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();
                        var userNameHeader = httpContext.Request.Headers["X-User-Name"].FirstOrDefault();
                        var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                        var appCodeHeader = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();

                        // Also try alternative header names
                        if (string.IsNullOrEmpty(tenantIdHeader))
                        {
                            tenantIdHeader = httpContext.Request.Headers["TenantId"].FirstOrDefault();
                        }
                        if (string.IsNullOrEmpty(appCodeHeader))
                        {
                            appCodeHeader = httpContext.Request.Headers["AppCode"].FirstOrDefault();
                        }

                        // Determine final values with priority: headers > JWT claims > AppContext > defaults
                        // 修改优先级顺序，使header优先级高于JWT token
                        var userId = userIdHeader ?? userIdClaim?.Value ?? "1";
                        var email = emailClaim?.Value ?? string.Empty;
                        var userName = userNameHeader ?? usernameClaim?.Value ?? email ?? "System";
                        var tenantId = tenantIdHeader ?? tenantIdClaim?.Value ?? appContext?.TenantId ?? "default";
                        var appCode = appCodeHeader ?? appCodeClaim?.Value ?? appContext?.AppCode ?? "default";

                        // Note: No inference from email domain - use explicit headers only

                        // User context logging handled by structured logging

                        return new UserContext
                        {
                            UserId = userId,
                            UserName = userName,
                            Email = email,
                            TenantId = tenantId,
                            AppCode = appCode
                        };
                    }

                    // Default values for test environment or when no HTTP context
                    return new UserContext
                    {
                        UserId = "1",
                        UserName = "TestUser",
                        Email = string.Empty,
                        TenantId = "default",
                        AppCode = "default"
                    };
                }
                catch (ObjectDisposedException)
                {
                    // Service provider was disposed during shutdown, return safe default
                    return new UserContext
                    {
                        UserId = "1",
                        UserName = "System",
                        Email = string.Empty,
                        TenantId = "default",
                        AppCode = "default"
                    };
                }
                catch (Exception ex)
                {
                    // Any other error, log and return safe default
                    Console.WriteLine($"Warning: Failed to create UserContext: {ex.Message}");
                    return new UserContext
                    {
                        UserId = "1",
                        UserName = "System",
                        Email = string.Empty,
                        TenantId = "default",
                        AppCode = "default"
                    };
                }
            });

            // Register necessary ASP.NET Core services
            services.AddHttpContextAccessor();

            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(WorkflowService).Assembly));

            // Register AI services
            services.AddScoped<IAIModelConfigRepository, AIModelConfigRepository>();
            services.AddScoped<IAIModelConfigService, AIModelConfigService>();
            services.AddScoped<FlowFlex.Domain.Repository.OW.IAIPromptHistoryRepository, FlowFlex.SqlSugarDB.Repositories.OW.AIPromptHistoryRepository>();

            // Register distributed cache service
            services.AddScoped<IDistributedCacheService, FlowFlex.Infrastructure.Services.RedisCacheService>();

            services.AddMemoryCache();
            services.AddIdentityHubService(configuration);

            // Auto-register services based on lifetime marker interfaces
            RegisterServicesByLifetime(services);

            // Register blob storage provider and file storage service
            // This must be called after RegisterServicesByLifetime to override auto-registration
            services.AddBlobStorage(configuration);

            return services;
        }

        /// <summary>
        /// Register blob storage provider and file storage service based on configuration
        /// </summary>
        public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
        {
            // Read global configuration
            var globalConfigOptions = configuration.GetSection("Global").Get<GlobalConfigOptions>();
            if (globalConfigOptions == null)
            {
                globalConfigOptions = new GlobalConfigOptions { BlobStoreType = AttachmentStoreType.Local };
            }

            // Read BlobStore configuration
            var blobStoreOptions = configuration.GetSection("BlobStore").Get<BlobStoreOptions>();

            // Register BlobStoreOptions
            services.Configure<BlobStoreOptions>(configuration.GetSection("BlobStore"));

            // Remove any existing IFileStorageService registrations to avoid conflicts
            // CloudFileStorageService and LocalFileStorageService are excluded from auto-registration
            // We will register the correct one based on configuration below
            var fileStorageDescriptors = services.Where(s => s.ServiceType == typeof(IFileStorageService)).ToList();
            foreach (var descriptor in fileStorageDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register BlobProvider if using cloud storage (OSS or AWS)
            if (globalConfigOptions.BlobStoreType == AttachmentStoreType.OSS || 
                globalConfigOptions.BlobStoreType == AttachmentStoreType.AWS)
            {
                if (blobStoreOptions == null)
                {
                    throw new InvalidOperationException("BlobStore configuration is required when using cloud storage (OSS/AWS)");
                }

                // Register BlobProvider
                services.AddBlobProvider(() => new BlobProviderConfiguration
                {
                    ProviderType = globalConfigOptions.BlobStoreType switch
                    {
                        AttachmentStoreType.OSS => BlobProviderType.OSS,
                        AttachmentStoreType.AWS => BlobProviderType.AWS,
                        _ => BlobProviderType.AWS
                    },
                    AccessKeyId = blobStoreOptions.AccessKeyId,
                    AccessKeySecret = blobStoreOptions.SecretAccessKey,
                    Region = blobStoreOptions.Region ?? string.Empty,
                    ContainerName = blobStoreOptions.Bucket,
                    ProfileName = blobStoreOptions.ProfileName,
                    EndPoint = blobStoreOptions.EndPoint ?? string.Empty
                });

                // Register CloudFileStorageService as IFileStorageService
                // Remove any existing registration first to avoid conflicts
                var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileStorageService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IFileStorageService, CloudFileStorageService>();
            }
            else
            {
                // Use LocalFileStorageService for Local storage
                // Remove any existing registration first to avoid conflicts
                var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileStorageService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                // LocalFileStorageService will be auto-registered via IScopedService interface
                // But we explicitly register it here to ensure it's used for Local storage
                services.AddScoped<IFileStorageService, LocalFileStorageService>();
            }

            return services;
        }

        /// <summary>
        /// Get current tenant ID from HTTP context
        /// </summary>
        private static string GetCurrentTenantId(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                // Try to get tenant ID from request headers
                var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = httpContext.Request.Headers["TenantId"].FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    return tenantId;
                }
            }

            // Fallback: try to get from UserContext service
            try
            {
                var userContext = serviceProvider.GetService<UserContext>();
                if (!string.IsNullOrEmpty(userContext?.TenantId))
                {
                    return userContext.TenantId;
                }
            }
            catch (ObjectDisposedException)
            {
                // Service provider was disposed, use default
                return "default";
            }
            catch
            {
                // Ignore other service resolution errors during startup
            }

            return "default";
        }

        /// <summary>
        /// Auto-register services based on their lifetime marker interfaces
        /// </summary>
        private static void RegisterServicesByLifetime(IServiceCollection services)
        {
            // Get all assemblies that contain our services
            var assemblies = new[]
            {
                typeof(WorkflowService).Assembly,      // Application layer
                typeof(WorkflowRepository).Assembly,   // SqlSugarDB layer
                typeof(UserService).Assembly           // Application.Services layer (if different)
            }.Distinct().ToArray();

            // Types to exclude from auto-registration (will be registered manually based on configuration)
            var excludedTypes = new[]
            {
                typeof(CloudFileStorageService),
                typeof(LocalFileStorageService)
            };

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !excludedTypes.Contains(t))
                    .ToList();

                // Register Scoped services
                RegisterByLifetime<IScopedService>(services, types, ServiceLifetime.Scoped);

                // Register Singleton services  
                RegisterByLifetime<ISingletonService>(services, types, ServiceLifetime.Singleton);

                // Register Transient services
                RegisterByLifetime<ITransientService>(services, types, ServiceLifetime.Transient);
            }
        }

        /// <summary>
        /// Register services by specific lifetime marker interface
        /// </summary>
        private static void RegisterByLifetime<TMarkerInterface>(
            IServiceCollection services,
            List<Type> types,
            ServiceLifetime lifetime)
        {
            var markerInterfaceType = typeof(TMarkerInterface);
            var serviceTypes = types.Where(t => markerInterfaceType.IsAssignableFrom(t)).ToList();

            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces()
                    .Where(i => i != markerInterfaceType && !i.IsGenericType)
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    switch (lifetime)
                    {
                        case ServiceLifetime.Scoped:
                            services.AddScoped(interfaceType, serviceType);
                            break;
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(interfaceType, serviceType);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(interfaceType, serviceType);
                            break;
                    }
                }

                // Also register the concrete type for direct injection
                switch (lifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.AddScoped(serviceType);
                        break;
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(serviceType);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(serviceType);
                        break;
                }
            }
        }
    }
}
