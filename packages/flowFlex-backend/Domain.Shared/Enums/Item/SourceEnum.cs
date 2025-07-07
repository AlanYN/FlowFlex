using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Source
    /// </summary>
    public enum SourceEnum
    {
        None = 0,

        /// <summary>
        /// System added
        /// </summary>
        [Description("Default")]
        Default = 1,

        /// <summary>
        /// BNPSystem
        /// </summary>
        [Description("BNPSystem")]
        BNPSystem = 2,

        /// <summary>
        /// ClientPortal
        /// </summary>
        [Description("ClientPortal")]
        ClientPortal = 3,

        [Description("OnBoarding")]
        OnBoarding = 4,

        /// <summary>
        /// LSOApp
        /// </summary>
        [Description("LSOApp")]
        LSOApp = 5,

        /// <summary>
        /// Deal
        /// </summary>
        [Description("Deal")]
        Deal = 10001,

        /// <summary>
        /// Wise
        /// </summary>
        [Description("Wise")]
        Wise = 7,

        /// <summary>
        /// Indicates that the token used in the current request is from client authentication
        /// </summary>
        [Description("Client")]
        Client = 8,

        /// <summary>
        /// DI
        /// </summary>
        [Description("DI")]
        DI = 9,

        /// <summary>
        /// WMS
        /// </summary>
        [Description("WMS")]
        WMS = 10,

        /// <summary>
        /// Yardi(Cubework)
        /// </summary>
        [Description("Yardi")]
        Yardi = 11,

        [Description("ItemApp")]
        ItemApp = 20,

        [Description("ItemWeb")]
        ItemWeb = 30,

        /// <summary>
        /// ClientApp
        /// </summary>
        [Description("ClientApp")]
        ClientApp = 40,

        /// <summary>
        /// Leads(OnBoarding)
        /// </summary>
        [Description("Leads")]
        Leads = 10002,
    }
}
