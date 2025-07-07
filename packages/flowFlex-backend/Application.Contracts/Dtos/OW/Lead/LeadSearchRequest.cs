using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.Lead
{
    /// <summary>
    /// Lead搜索请求DTO - 用于前端简化搜索
    /// </summary>
    public class LeadSearchRequest : QueryPageModel
    {
        /// <summary>
        /// 搜索关键词（会在名称、邮箱、电话中搜索）
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        /// 是否已有Onboarding（true=已有，false=未有，null=全部）
        /// </summary>
        public bool? HasOnboarding { get; set; }
    }
}