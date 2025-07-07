using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Lead
{
    /// <summary>
    /// Lead Code输出DTO - 专门用于返回leads_code信息
    /// </summary>
    public class LeadCodeOutputDto
    {
        /// <summary>
        /// Lead ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Lead编码
        /// </summary>
        public string LeadsCode { get; set; }

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
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }
    }
}