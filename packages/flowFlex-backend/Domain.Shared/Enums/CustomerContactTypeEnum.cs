using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum CustomerContactTypeEnum
    {
        [Description("AP")]
        AP = 1002,

        [Description("Alert Email")]
        AlertEmail = 1003,

        [Description("Sales Rep")]
        SalesRep = 1004,

        [Description("Primary")]
        Primary = 1001,

        [Description("Others")]
        Others = 1005,

        [Description("Admin")]
        Admin = 1006,

        [Description("EDI/IT")]
        EDIIT = 1007,

        [Description("AR")]
        AR = 1008,

        [Description("Claims")]
        Claims = 1009,

        [Description("Owner")]
        Owner = 1010,

        [Description("Applicant")]
        Applicant = 1011,

        [Description("Debt Collection")]
        DebtCollection = 1012,

        [Description("Primary")]
        PrimaryAlternate = 1013,

        [Description("Relatives")]
        Relatives = 1014
    }
}
