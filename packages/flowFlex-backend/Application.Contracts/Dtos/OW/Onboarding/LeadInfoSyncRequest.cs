using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Lead信息同步请求DTO
    /// </summary>
    public class LeadInfoSyncRequest
    {
        /// <summary>
        /// Lead ID
        /// </summary>

        public string LeadId { get; set; } = string.Empty;

        /// <summary>
        /// Lead名称
        /// </summary>
        public string? LeadName { get; set; }

        /// <summary>
        /// Lead邮箱
        /// </summary>
        public string? LeadEmail { get; set; }

        /// <summary>
        /// Lead电话
        /// </summary>
        public string? LeadPhone { get; set; }

        /// <summary>
        /// 生命周期阶段ID
        /// </summary>
        public long? LifecycleStageId { get; set; }

        /// <summary>
        /// 生命周期阶段名称
        /// </summary>
        public string? LifecycleStageName { get; set; }
    }
}