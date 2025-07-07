using System;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.Lead
{
    /// <summary>
    /// Lead查询请求DTO
    /// </summary>
    public class LeadQueryRequest : QueryPageModel
    {
        /// <summary>
        /// Lead名称或公司名称（模糊查询）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 邮箱（模糊查询）
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 电话（模糊查询）
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Life Cycle Stage ID
        /// </summary>
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// Lead状态ID
        /// </summary>
        public long? LeadStatusId { get; set; }

        /// <summary>
        /// 负责人ID
        /// </summary>
        public long? AssignedToId { get; set; }

        /// <summary>
        /// 是否已有Onboarding（true=已有，false=未有，null=全部）
        /// </summary>
        public bool? HasOnboarding { get; set; }

        /// <summary>
        /// 创建时间开始
        /// </summary>
        public DateTimeOffset? CreateDateFrom { get; set; }

        /// <summary>
        /// 创建时间结束
        /// </summary>
        public DateTimeOffset? CreateDateTo { get; set; }
    }
}