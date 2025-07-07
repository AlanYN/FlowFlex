
namespace FlowFlex.Domain.Shared.Models.Customer.CustomerSimple
{
    public class CustomerSimpleModel
    {
        /// <summary>
        /// Basic customer information
        /// </summary>
        public BasicModel Basic { get; set; }
        /// <summary>
        /// Financial information of the customer
        /// </summary>
        public FinancialModel Financial { get; set; }
    }
}
