using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList
{
    public class PluginPriceListInputDto
    {
        [Required]
        [StringLength(50)]
        public string CaseCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CustomerCode { get; set; }

        [StringLength(200)]
        public string? CustomerName { get; set; }

        [StringLength(50)]
        public string? PriceListType { get; set; }

        [StringLength(20)]
        public string? StartDate { get; set; }

        [StringLength(20)]
        public string? EndDate { get; set; }

        public object? Data { get; set; }
    }
}
