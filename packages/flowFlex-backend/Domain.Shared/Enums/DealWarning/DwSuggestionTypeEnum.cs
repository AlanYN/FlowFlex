using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.DealWarning
{
    /// <summary>
    /// Suggestion type enumeration
    /// </summary>
    public enum DwSuggestionTypeEnum
    {
        /// <summary>
        /// Customer communication
        /// </summary>
        [Description("Customer Communication")]
        CustomerCommunication = 1,

        /// <summary>
        /// Internal collaboration
        /// </summary>
        [Description("Internal Collaboration")]
        InternalCollaboration = 2,

        /// <summary>
        /// Document preparation
        /// </summary>
        [Description("Document Preparation")]
        DocumentPreparation = 3,

        /// <summary>
        /// Pricing strategy
        /// </summary>
        [Description("Pricing Strategy")]
        PricingStrategy = 4,

        /// <summary>
        /// Other
        /// </summary>
        [Description("Other")]
        Other = 5
    }
}
