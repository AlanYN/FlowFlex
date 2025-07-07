using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum CustomerBusinessTypeEnum
    {
        [Description("Sole Proprietorship")]
        SoleProprietorship = 1001,

        [Description("Partnerships")]
        Partnerships = 1002,

        [Description("Privately Held")]
        PrivatelyHeld = 1003,

        [Description("Joint Venture")]
        JointVenture = 1004,

        [Description("Limited Liability Company (LLC)")]
        LimitedLiabilityCompanyLLC = 1005,

        [Description("Corporation (S Corp)")]
        CorporationSCorp = 1006,

        [Description("Corporation (C Corp)")]
        CorporationCCorp = 1007,

        [Description("Corporation (B Corp)")]
        CorporationBCorp = 1008,

        [Description("Nonprofit Organization")]
        NonprofitOrganization = 1009,

    }
}
