using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum NotificationTypeEnum
    {

        [Description("EDI")]
        EDI = 1,

        [Description("Fax")]
        Fax = 2,

        [Description("FTP")]
        FTP = 3,
        [Description("Email")]
        Email = 4
    }
}
