using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// AI模型配置仓储实现
    /// </summary>
    public class AIModelConfigRepository : BaseRepository<AIModelConfig>, IAIModelConfigRepository
    {
        public AIModelConfigRepository(ISqlSugarClient context) : base(context)
        {
        }

        /// <summary>
        /// 获取用户的所有AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>AI模型配置列表</returns>
        public async Task<List<AIModelConfig>> GetUserAIModelConfigsAsync(long userId)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.UserId == userId && x.IsValid == true)
                .OrderByDescending(x => x.IsDefault)
                .ToListAsync();
        }

        /// <summary>
        /// 获取用户的默认AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>默认AI模型配置</returns>
        public async Task<AIModelConfig> GetUserDefaultConfigAsync(long userId)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.UserId == userId && x.IsDefault == true && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// 获取用户指定提供商的AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="provider">AI提供商</param>
        /// <returns>AI模型配置</returns>
        public async Task<AIModelConfig> GetUserProviderConfigAsync(long userId, string provider)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.UserId == userId && x.Provider == provider && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// 根据租户ID和应用代码获取AI模型配置列表
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>AI模型配置列表</returns>
        public async Task<List<AIModelConfig>> GetByTenantAndAppAsync(string tenantId, string appCode)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.TenantId == tenantId && x.AppCode == appCode && x.IsValid == true)
                .OrderByDescending(x => x.IsDefault)
                .ToListAsync();
        }

        /// <summary>
        /// 根据租户ID和应用代码获取默认AI模型配置
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>默认AI模型配置</returns>
        public async Task<AIModelConfig> GetDefaultByTenantAndAppAsync(string tenantId, string appCode)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.TenantId == tenantId && x.AppCode == appCode && x.IsDefault == true && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// 根据租户ID、应用代码和提供商获取AI模型配置
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <param name="provider">AI提供商</param>
        /// <returns>AI模型配置</returns>
        public async Task<AIModelConfig> GetProviderConfigAsync(string tenantId, string appCode, string provider)
        {
            return await db.Queryable<AIModelConfig>()
                .Where(x => x.TenantId == tenantId && x.AppCode == appCode && x.Provider == provider && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// 设置为默认配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>操作结果</returns>
        public async Task<bool> SetAsDefaultAsync(long configId, string tenantId, string appCode)
        {
            // 先将该租户和应用的所有配置设为非默认
            await db.Updateable<AIModelConfig>()
                .SetColumns(x => x.IsDefault == false)
                .Where(x => x.TenantId == tenantId && x.AppCode == appCode && x.IsValid == true)
                .ExecuteCommandAsync();

            // 设置指定配置为默认
            return await db.Updateable<AIModelConfig>()
                .SetColumns(x => x.IsDefault == true)
                .Where(x => x.Id == configId && x.TenantId == tenantId && x.AppCode == appCode && x.IsValid == true)
                .ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// 更新配置可用状态
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="isAvailable">是否可用</param>
        /// <returns>操作结果</returns>
        public async Task<bool> UpdateAvailabilityAsync(long configId, bool isAvailable)
        {
            return await db.Updateable<AIModelConfig>()
                .SetColumns(x => new AIModelConfig 
                { 
                    IsAvailable = isAvailable,
                    LastCheckTime = DateTime.UtcNow
                })
                .Where(x => x.Id == configId && x.IsValid == true)
                .ExecuteCommandAsync() > 0;
        }
    }
} 