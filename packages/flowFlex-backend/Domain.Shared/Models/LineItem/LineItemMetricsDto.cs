using System;

namespace FlowFlex.Domain.Shared.Models.LineItem
{
    public class LineItemMetricsDto
    {
        public decimal TCV { get; set; }
        public decimal ACV { get; set; }
        public decimal ARR { get; set; }
        public decimal MRR { get; set; }
        public decimal Margin { get; set; }
        public decimal DealAmount { get; set; }
    }
}
