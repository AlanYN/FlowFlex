using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Const
{
    public class FinancialCosts
    {
        public const string DefaultClassCode = "GNL";
        public const string UnderLimit = "Under Limit";
        public const string AboveLimit = "Above Limit";
        public const int ARAccountOnRepresentCompany = 48;

        /// <summary>
        /// Get DefaultARAccount by clientShortName
        /// </summary>
        public static int? GetDefaultARAccount(string clientShortName)
        {
            return clientShortName switch
            {
                _ => 47,
            };
        }
    }
}
