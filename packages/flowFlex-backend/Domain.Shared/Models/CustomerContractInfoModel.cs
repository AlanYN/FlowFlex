using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Models
{
    public class CustomerContractInfoModel : QueryPageModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string ContractName { get; set; }
        /// <summary>
        /// Effective date
        /// </summary>
        public DateTimeOffset EffectiveDate { get; set; }
        /// <summary>
        /// Contract terms
        /// </summary>
        public string ContractTerms { get; set; }
        /// <summary>
        /// Shrink allowance
        /// </summary>
        public string ShrinkAllowance { get; set; }
    }
}
