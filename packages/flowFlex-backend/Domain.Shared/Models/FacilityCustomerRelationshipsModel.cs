
namespace FlowFlex.Domain.Shared.Models
{
    public class FacilityCustomerRelationshipsModel
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public long? Facility { get; set; }
    }
}
