using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// 选项集合名称
    /// </summary>
    public enum OptionNamesEnum
    {
        [Description("Prioritys")]
        Prioritys = 1,

        [Description("Industrys")]
        Industrys = 2,

        [Description("LifeCycleStages")]
        LifeCycleStages = 3,

        [Description("DealStages")]
        DealStages = 4,

        [Description("DealTypes")]
        DealTypes = 5,

        [Description("ContractSigningEntity")]
        ContractSigningEntity = 6,

        [Description("TaskToDos")]
        TaskToDos = 7,

        [Description("SendReminderRule")]
        SendReminderRule = 8,

        [Description("PreferencesCommunications")]
        PreferencesCommunications = 9,

        [Description("PreferencesLanguages")]
        PreferencesLanguages = 10,

        [Description("BuyingReasons")]
        BuyingReasons = 11,

        [Description("BuyingTimeframes")]
        BuyingTimeframes = 12,

        [Description("PreferencesCurrencys")]
        PreferencesCurrencys = 13,


    }
}
