using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum NetTermEnum
    {
        [Description("Net 15")]
        Net15 = 1001,

        [Description("Net 30")]
        Net30 = 1002,

        [Description("Net 45")]
        Net45 = 1003,

        [Description("Net 60")]
        Net60 = 1004,

        [Description("Net 7")]
        Net7 = 1009,

        [Description("Net 10")]
        Net10 = 1010,

        [Description("Net 21")]
        Net21 = 1011,

        [Description("Due on Receipt - Credit Card")]
        DueOnReceiptCreditCard = 1012,

        [Description("Due Upon Receipt")]
        DueUponReceipt = 1013,

        [Description("Prepayment")]
        Prepayment = 1014,

        [Description("Net 5")]
        Net5 = 1015,

        [Description("Net 20")]
        Net20 = 1016,

        [Description("DRIVER COLLECT")]
        DriverCollect = 1019,

        [Description("DEPOSIT")]
        Deposit = 1020,

        [Description("Due on Receipt - Deposit")]
        DueOnReceiptDeposit = 1021,

        [Description("C.O.D.")]
        COD = 1022,

        [Description("7 Days")]
        SevenDays = 1023,

        [Description("Net 17")]
        Net17 = 1024,

        [Description("Net 18")]
        Net18 = 1025,

        [Description("Net 19")]
        Net19 = 1026,

        [Description("Net 25")]
        Net25 = 1027,

        [Description("Net 31")]
        Net31 = 1028,

        [Description("3% 5 Net 6")]
        ThreePercent5Net6 = 1029,

        [Description("3% 10 Net 30")]
        ThreePercent10Net30 = 1030,

        [Description("2% 10 Net 30")]
        TwoPercent10Net30 = 1031,

        [Description("1% 7 Net 30")]
        OnePercent7Net30 = 1032,

        [Description("1% 10 Net 30")]
        OnePercent10Net30 = 1033,

        [Description("1% Due Upon Receipt")]
        OnePercentDueUponReceipt = 1036,

        [Description("2% 5 Net 30")]
        TwoPercent5Net30 = 1037,

        [Description("Net 35")]
        Net35 = 1038,

        [Description("3% 5 Net 30")]
        ThreePercent5Net30 = 1039,

        [Description("3% 5 Net 45")]
        ThreePercent5Net45 = 1040,

        [Description("Weekly, Due on Friday")]
        WeeklyDueOnFriday = 1041,

        [Description("Bi-Weekly, Due on Friday")]
        BiWeeklyDueOnFriday = 1042,

        [Description("Weekly, Due on N_Monday")]
        WeeklyDueOnNMonday = 1043,

        [Description("Net 90")]
        Net90 = 1044
    }
}
