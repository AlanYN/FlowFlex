using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Set stage color input DTO
    /// </summary>
    public class SetStageColorInputDto
    {
        /// <summary>
        /// 阶段颜色
        /// </summary>
        
        [StringLength(20)]
        public string Color { get; set; }
    }
}