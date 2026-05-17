using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList
{
    public class PluginPriceListSubmitDto
    {
        [Required]
        [StringLength(50)]
        public string CaseCode { get; set; } = string.Empty;
    }
}
