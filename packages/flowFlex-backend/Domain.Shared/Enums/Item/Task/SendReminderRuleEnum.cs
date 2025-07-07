using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item.Task
{
    /// <summary>
    /// 发送提醒规则
    /// </summary>
    public enum SendReminderRuleEnum
    {
        [Description("No")]
        No = 1,

        [Description("30 minutes before")]
        MinutesBefore30 = 2,

        [Description("1 hour before")]
        HourBefore1 = 3,

        [Description("1 day before")]
        DayBefore1 = 4,

        [Description("When the task expires")]
        Expire = 5
    }
}
