using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Update required fields input DTO
    /// </summary>
    public class UpdateRequiredFieldsInputDto
    {
        /// <summary>
        /// 必填字段列表（逗号分隔）
        /// </summary>
        
        public string RequiredFields { get; set; }

        /// <summary>
        /// 字段说明
        /// </summary>
        public string FieldDescription { get; set; }
    }
}