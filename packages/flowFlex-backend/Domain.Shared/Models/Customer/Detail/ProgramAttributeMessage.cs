

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class ProgramAttributeMessage : DataMessageBase
    {
        /// <summary>
        /// Group number
        /// </summary>
        public int Program { get; set; }

        /// <summary>
        /// Name of the program
        /// </summary>
        public string ProgramName { get; set; }

        /// <summary>
        /// Name of the customer
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Customer code of the current customer, which can be modified by the user
        /// </summary>
        public string CustomerCode { get; set; }
    }
}
