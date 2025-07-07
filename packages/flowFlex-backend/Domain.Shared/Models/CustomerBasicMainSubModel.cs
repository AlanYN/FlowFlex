using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models;

public class CustomerBasicMainSubModel
{
    public CustomerMappingModel Main { get; set; }

    public List<CustomerMappingModel> Subs { get; set; }
}
