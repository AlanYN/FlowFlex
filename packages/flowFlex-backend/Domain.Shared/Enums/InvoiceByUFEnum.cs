using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum InvoiceByUFEnum
    {
        Consolidated = 0
    }
    public enum InvoiceByUFListEnum
    {
        [Description("Title separation with consolidated billing")]
        Titleseparationwithconsolidatedbilling = 1,
        [Description("Title separation without consolidated billing")]
        Titleseparationwithoutconsolidatedbilling = 2,
        [Description("inbound handling")]
        inboundhandling = 3,
        [Description("Outbound handling")]
        Outboundhandling = 4,
        [Description("Outbound handling with business type")]
        Outboundhandlingwithbusinesstype = 5,
        Storage = 6,
        [Description("Accessorial")]
        Accessorial = 7,
        [Description("Other (please provide detail)")]
        Other = 8
    }
}
