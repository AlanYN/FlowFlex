using Hangfire;
using Hangfire.PostgreSql;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.SqlSugarDB.Implements.OW;
using FlowFlex.SqlSugarDB.Repositories.OW;
using FlowFlex.Application.Contracts;
using FlowFlex.Domain.Shared.Models;
using RulesEngine.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using FlowFlex.Infrastructure;
using SqlSugar;
using MediatR;
// using FlowFlex.Infrastructure.Services; // This namespace does not exist
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Item.Redis;
using Microsoft.Extensions.Configuration;
using FlowFlex.SqlSugarDB.Context;
using Microsoft.AspNetCore.Http;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Shared;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from configuration file
            var connectionString = configuration.GetConnectionString("Default") 
                ?? "Host=localhost;Port=5432;Database=flowflex;Username=postgres;Password=123456;";

            // Read SqlSugar configuration
            var configId = configuration["SqlSugar:ConfigId"] ?? "FlowFlex";
            var dbTypeString = configuration["SqlSugar:DbType"] ?? "PostgreSQL";
            var dbType = Enum.Parse<DbType>(dbTypeString);

            // Log which connection string is being used
            Console.WriteLine($"[SqlSugar Config] Using connection string: {connectionString}");
            Console.WriteLine($"[SqlSugar Config] Using ConfigId: {configId}");
            Console.WriteLine($"[SqlSugar Config] Using DbType: {dbType}");
            
            // Log configuration source information
            var configFromFile = configuration.GetConnectionString("Default");
            Console.WriteLine($"[SqlSugar Config] Connection string from config file: {configFromFile ?? "NULL"}");
            Console.WriteLine($"[SqlSugar Config] Using fallback connection string: {configFromFile == null}");

            // Register SqlSugar
            services.AddSingleton<ISqlSugarClient>(sp =>
            {
                var config = new ConnectionConfig()
                {
                    ConfigId = configId,
                    DbType = dbType,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute,
                    LanguageType = LanguageType.English,
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
                                // Check if there is a Table attribute
                                var tableAttribute = type.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false)
                                    .FirstOrDefault() as System.ComponentModel.DataAnnotations.Schema.TableAttribute;
                                
                                if (tableAttribute != null)
                                {
                                    entity.DbTableName = tableAttribute.Name;
                                }
                                else
                                {
                                    // If there is no Table attribute, convert class name to snake_case
                                    entity.DbTableName = UtilMethods.ToUnderLine(type.Name);
                                }
                            }
                        }
                    }
                };

                var sqlSugarClient = new SqlSugarScope(config, db =>
                {
                    var provider = db.GetConnectionScope(configId);
                    
                    provider.Aop.OnError = (exp) =>
                    {
                        Console.WriteLine($"[SqlSugar Error] {exp.Message}");
                        Console.WriteLine($"[SqlSugar SQL] {exp.Sql}");
                    };
                    
                    provider.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        string logSql = $"{UtilMethods.GetSqlString(dbType, sql, pars)}";
                        Console.WriteLine($"[SqlSugar SQL] {logSql}");
                    };

                    // Note: SqlSugar global filters with complex lambda expressions are not supported
                    // Tenant filtering will be handled at the repository level instead
                    // provider.QueryFilter.Add(new TableFilterItem<OwEntityBase>(it => it.TenantId == "CURRENT_TENANT"));
                    // provider.QueryFilter.Add(new TableFilterItem<EntityBaseCreateInfo>(it => it.TenantId == "CURRENT_TENANT"));
                });

                return sqlSugarClient;
            });

            // Register SqlSugar context
            services.AddScoped<ISqlSugarContext, SqlSugarContext>();

            // Register UserContext - get from HTTP request headers or default values
            services.AddScoped<UserContext>(provider =>
            {
                var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor?.HttpContext;
                
                if (httpContext != null)
                {
                    // Try to get user information from request headers
                    var userIdHeader = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();
                    var userNameHeader = httpContext.Request.Headers["X-User-Name"].FirstOrDefault();
                    var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                    
                    // Also try alternative header names
                    if (string.IsNullOrEmpty(tenantIdHeader))
                    {
                        tenantIdHeader = httpContext.Request.Headers["TenantId"].FirstOrDefault();
                    }
                    
                    // ���header��û���⻧ID�����Դ�JWT Token�л�ȡ
                    if (string.IsNullOrEmpty(tenantIdHeader))
                    {
                        var tenantIdClaim = httpContext.User?.FindFirst("tenantId");
                        if (tenantIdClaim != null)
                        {
                            tenantIdHeader = tenantIdClaim.Value;
                        }
                        else
                        {
                            // ���Token��Ҳû�У����Դ������ȡ
                            var emailClaim = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Email);
                            if (emailClaim != null)
                            {
                                tenantIdHeader = TenantHelper.GetTenantIdByEmail(emailClaim.Value);
                            }
                        }
                    }
                    
                    var tenantId = tenantIdHeader ?? "DEFAULT";
                    
                    Console.WriteLine($"[UserContext] Headers - UserId: {userIdHeader}, UserName: {userNameHeader}, TenantId: {tenantId}");
                    
                    return new UserContext
                    {
                        UserId = userIdHeader ?? "1",
                        UserName = userNameHeader ?? "System",
                        TenantId = tenantId
                    };
                }
                
                // Default values for test environment or when no HTTP context
                return new UserContext
                {
                    UserId = "1",
                    UserName = "TestUser",
                    TenantId = "DEFAULT"
                };
            });

            // Register necessary ASP.NET Core services
            services.AddHttpContextAccessor();
            
            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(WorkflowService).Assembly));

            // Only register basic Repositories
            RegisterBasicRepositories(services);
            
            // Only register basic Services
            RegisterBasicServices(services);

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
            catch
            {
                // Ignore service resolution errors during startup
            }
            
            return "default";
        }

        private static void RegisterBasicRepositories(IServiceCollection services)
        {
            // Register all repositories that implement IScopedService
            var scopedServiceType = typeof(IScopedService);
            var repositoryTypes = typeof(WorkflowRepository).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && scopedServiceType.IsAssignableFrom(t))
                .ToList();

            foreach (var repositoryType in repositoryTypes)
            {
                var interfaces = repositoryType.GetInterfaces()
                    .Where(i => i != scopedServiceType && !i.IsGenericType)
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, repositoryType);
                    Console.WriteLine($"[DI Registration] Registered {interfaceType.Name} -> {repositoryType.Name}");
                }
            }
        }

        private static void RegisterBasicServices(IServiceCollection services)
        {
            // Register all services that implement IScopedService
            var scopedServiceType = typeof(IScopedService);
            var serviceTypes = typeof(WorkflowService).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && scopedServiceType.IsAssignableFrom(t))
                .ToList();

            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces()
                    .Where(i => i != scopedServiceType && !i.IsGenericType)
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, serviceType);
                    Console.WriteLine($"[DI Registration] Registered {interfaceType.Name} -> {serviceType.Name}");
                }
            }
        }
    }
}
