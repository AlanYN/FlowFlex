using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums;

public enum BillingFrequencyEnum
{
    [EnumValue(Name = "Monthly")]
    Monthly = 1,      // Billing occurs every month

    //Quarterly = 2,    // Billing occurs every three months

    [EnumValue(Name = "OneTime")]
    OneTime = 3,      // Billing occurs only once
    //Weekly = 4,       // Billing occurs every week
    //Annually = 5,     // Billing occurs every year
    //SemiAnnually = 6  // Billing occurs every six months
}
