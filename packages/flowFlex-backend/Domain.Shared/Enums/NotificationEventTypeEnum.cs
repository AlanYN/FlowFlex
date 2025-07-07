using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum NotificationEventTypeEnum
    {
        [Description("email notifications")]
        EmailNotifications = 0,

        [Description("lead assigned")]
        LeadsAssigned = 1,

        [Description("leads record mentioned")]
        LeadsRecordMentioned = 2,

        [Description("tasks assigned")]
        TasksAssigned = 3,

        [Description("reminders when tasks are due")]
        TasksAreDue = 4,

        [Description("creator and modifier of the task are inconsistent")]
        TasksUpdate = 5,

        [Description("deals assigned")]
        DealsAssigned = 6,

        [Description("mentioned on a deal record")]
        DealsRecordMentioned = 7,

        [Description("leads collaborator assigned")]
        LeadsCollaboratorAssigned = 8,

        [Description("deals collaborator assigned")]
        DealsCollaboratorAssigned = 9,
    }
}
