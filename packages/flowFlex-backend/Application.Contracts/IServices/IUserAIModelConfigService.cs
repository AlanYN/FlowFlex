using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts.IServices
{
    /// <summary>
    /// AI模型配置服务接口
    /// 基于租户隔离，不再依赖用户ID进行数据隔离
    /// </summary>
    public interface IAIModelConfigService : IScopedService
    {
        /// <summary>
        /// 获取当前租户的所有AI模型配置
        /// </summary>
        /// <param name="userId">用户ID（保留兼容性，实际使用租户隔离）</param>
        /// <returns>AI模型配置列表</returns>
        Task<List<AIModelConfig>> GetUserAIModelConfigsAsync(long userId);

        /// <summary>
        /// 获取当前租户的默认AI模型配置
        /// </summary>
        /// <param name="userId">用户ID（保留兼容性，实际使用租户隔离）</param>
        /// <returns>默认AI模型配置</returns>
        Task<AIModelConfig> GetUserDefaultConfigAsync(long userId);

        /// <summary>
        /// 创建AI模型配置
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>创建的配置ID</returns>
        Task<long> CreateConfigAsync(AIModelConfig config);

        /// <summary>
        /// 更新AI模型配置
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>操作结果</returns>
        Task<bool> UpdateConfigAsync(AIModelConfig config);

        /// <summary>
        /// 删除AI模型配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>操作结果</returns>
        Task<bool> DeleteConfigAsync(long configId);

        /// <summary>
        /// 设置为默认配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>操作结果</returns>
        Task<bool> SetAsDefaultAsync(long configId);

        /// <summary>
        /// 通过ID获取AI模型配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>AI模型配置</returns>
        Task<AIModelConfig> GetConfigByIdAsync(long configId);

        /// <summary>
        /// 测试AI模型连接
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>测试结果</returns>
        Task<bool> TestConfigConnectionAsync(long configId);

        /// <summary>
        /// 测试AI模型连接（详细结果）
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>详细测试结果</returns>
        Task<AIModelTestResult> TestConnectionAsync(AIModelConfig config);
    }

    /// <summary>
    /// AI模型测试结果
    /// </summary>
    public class AIModelTestResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 模型信息
        /// </summary>
        public string ModelInfo { get; set; } = string.Empty;

        /// <summary>
        /// 响应时间(毫秒)
        /// </summary>
        public long ResponseTime { get; set; }
    }
}