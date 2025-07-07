using System;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Lead
{
    /// <summary>
    /// Lead输出DTO - 用于Onboarding选择Lead
    /// </summary>
    public class LeadOutputDto
    {
        /// <summary>
        /// Lead ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Lead编码
        /// </summary>
        public string LeadCode { get; set; }

        /// <summary>
        /// Lead名称/公司名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 联系人邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 联系人电话
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Life Cycle Stage ID
        /// </summary>
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// Life Cycle Stage名称
        /// </summary>
        public string LifeCycleStageName { get; set; }

        /// <summary>
        /// Lead状态ID
        /// </summary>
        public long? LeadStatusId { get; set; }

        /// <summary>
        /// Lead状态名称
        /// </summary>
        public string LeadStatusName { get; set; }

        /// <summary>
        /// 负责人ID
        /// </summary>
        public long? AssignedToId { get; set; }

        /// <summary>
        /// 负责人姓名
        /// </summary>
        public string AssignedToName { get; set; }

        /// <summary>
        /// 负责人邮箱
        /// </summary>
        public string AssignedToEmail { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// 是否已有Onboarding
        /// </summary>
        public bool HasOnboarding { get; set; }

        /// <summary>
        /// Onboarding ID（如果存在）
        /// </summary>
        public long? OnboardingId { get; set; }
    }
}