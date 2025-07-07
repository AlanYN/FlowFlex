using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// Customer MainSub operation type before activation
    /// </summary>
    public enum CustomerMainSubTypeEnum
    {
        /// <summary>
        /// No operation needed, directly proceed to next step activation
        /// </summary>
        Activate = 0,
        /// <summary>
        /// Associate with existing Parent Account
        /// </summary>
        ParentAccountJoin = 101,
        /// <summary>
        /// Add new Parent Account and associate
        /// </summary>
        ParentAccountAddAndJoin = 102,
        /// <summary>
        /// Sub Customer
        /// </summary>
        SubCustomer = 201
    }
}
