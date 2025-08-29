using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// AI模型配置仓储接口
    /// </summary>
    public interface IAIModelConfigRepository : IBaseRepository<AIModelConfig>
    {
        /// <summary>
        /// 获取用户的所有AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>AI模型配置列表</returns>
        Task<List<AIModelConfig>> GetUserAIModelConfigsAsync(long userId);

        /// <summary>
        /// 获取用户的默认AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>默认AI模型配置</returns>
        Task<AIModelConfig> GetUserDefaultConfigAsync(long userId);

        /// <summary>
        /// 获取用户指定提供商的AI模型配置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="provider">AI提供商</param>
        /// <returns>AI模型配置</returns>
        Task<AIModelConfig> GetUserProviderConfigAsync(long userId, string provider);

        /// <summary>
        /// 根据租户ID和应用代码获取AI模型配置列表
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>AI模型配置列表</returns>
        Task<List<AIModelConfig>> GetByTenantAndAppAsync(string tenantId, string appCode);

        /// <summary>
        /// 根据租户ID和应用代码获取默认AI模型配置
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>默认AI模型配置</returns>
        Task<AIModelConfig> GetDefaultByTenantAndAppAsync(string tenantId, string appCode);

        /// <summary>
        /// 根据租户ID、应用代码和提供商获取AI模型配置
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <param name="provider">AI提供商</param>
        /// <returns>AI模型配置</returns>
        Task<AIModelConfig> GetProviderConfigAsync(string tenantId, string appCode, string provider);

        /// <summary>
        /// 设置为默认配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="tenantId">租户ID</param>
        /// <param name="appCode">应用代码</param>
        /// <returns>操作结果</returns>
        Task<bool> SetAsDefaultAsync(long configId, string tenantId, string appCode);

        /// <summary>
        /// 更新配置可用状态
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="isAvailable">是否可用</param>
        /// <returns>操作结果</returns>
        Task<bool> UpdateAvailabilityAsync(long configId, bool isAvailable);
    }
}