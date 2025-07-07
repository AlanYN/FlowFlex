using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class ContractMessage : DataMessageBase
    {
        /// <summary>
        /// Contract name
        /// </summary>
        public string ContractName { get; set; }

        /// <summary>
        /// Effective date of the contract
        /// </summary>
        public DateTimeOffset? EffectiveDate { get; set; }

        /// <summary>
        /// Contract terms and conditions
        /// </summary>
        public string ContractTerms { get; set; }

        /// <summary>
        /// Shrink allowance or tolerance for the contract
        /// </summary>
        public string ShrinkAllowance { get; set; }
    }
}
