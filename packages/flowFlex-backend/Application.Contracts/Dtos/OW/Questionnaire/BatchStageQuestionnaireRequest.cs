using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire
{
    /// <summary>
    /// 批量查询Stage问卷请求DTO
    /// </summary>
    public class BatchStageQuestionnaireRequest
    {
        /// <summary>
        /// Stage ID列表
        /// </summary>

        public List<long> StageIds { get; set; } = new List<long>();
    }
}