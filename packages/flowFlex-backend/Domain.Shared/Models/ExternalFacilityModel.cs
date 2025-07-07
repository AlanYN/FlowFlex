namespace FlowFlex.Domain.Shared.Models
{
    public class ExternalFacilityModel : BaseModel
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }

    }
}
