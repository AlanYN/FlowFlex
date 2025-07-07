using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum NotificationModuleTypeEnum
    {
        [Description("Email Notifications")]
        EmailNotifications = 1,

        [Description("Leads")]
        Leads = 2,

        [Description("Tasks")]
        Tasks = 3,

        [Description("Deals")]
        Deals = 4,
    }
}
