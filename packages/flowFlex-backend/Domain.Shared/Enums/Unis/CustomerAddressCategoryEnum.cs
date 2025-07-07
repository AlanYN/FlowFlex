using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum CustomerAddressCategoryEnum
    {
        /// <summary>
        /// Corporate Address
        /// </summary>
        [Description("Corporate Address")]
        CorporateAddress = 1,
        /// <summary>
        /// Registered Address
        /// </summary>
        [Description("Registered Address")]
        RegisteredAddress = 2
    }
}
