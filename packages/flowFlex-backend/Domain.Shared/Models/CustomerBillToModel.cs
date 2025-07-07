namespace FlowFlex.Domain.Shared.Models
{
    public class CustomerBillToModel
    {
        public long Id { get; set; }
        public long? BillTo { get; set; }
        /// <summary>
        ///  ProgramId
        /// </summary>
        public int? ProgramId { get; set; }
    }
}
