using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    [SugarTable("ff_plugin_price_lists")]
    public class PluginPriceList : OwEntityBase
    {
        [Required]
        [StringLength(50)]
        [SugarColumn(ColumnName = "case_code")]
        public string CaseCode { get; set; } = string.Empty;

        [StringLength(50)]
        [SugarColumn(ColumnName = "customer_code")]
        public string? CustomerCode { get; set; }

        [StringLength(200)]
        [SugarColumn(ColumnName = "customer_name")]
        public string? CustomerName { get; set; }

        [StringLength(50)]
        [SugarColumn(ColumnName = "price_list_type")]
        public string PriceListType { get; set; } = "Customer Specific";

        [StringLength(20)]
        [SugarColumn(ColumnName = "start_date")]
        public string? StartDate { get; set; }

        [StringLength(20)]
        [SugarColumn(ColumnName = "end_date")]
        public string? EndDate { get; set; }

        [SugarColumn(ColumnName = "data", ColumnDataType = "jsonb", IsJson = true)]
        public string Data { get; set; } = "{}";

        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "draft";
    }
}
