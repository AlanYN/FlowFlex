using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList
{
    public class PluginPriceListOutputDto
    {
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        public string CaseCode { get; set; } = string.Empty;

        public string? CustomerCode { get; set; }

        public string? CustomerName { get; set; }

        public string? PriceListType { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public object? Data { get; set; }

        public string Status { get; set; } = "draft";

        public string Permission { get; set; } = "read";

        public string? CreatedBy { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
