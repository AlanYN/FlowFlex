using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// 合同签约主体 这个enum要和CustomerTypeEnum的值相对应
    /// </summary>
    public enum DealContractSigningEntityEnum
    {
        /// <summary>
        /// Company
        /// </summary>
        [Description("Company")]
        Company = 1,
        ///// <summary>
        ///// Personal(等个人业务开放后，再解除注释)
        ///// </summary>
        //[Description("Personal")]
        //Personal = 2
    }
}
