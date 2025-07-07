using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Models
{
    public class FacilityInputModel : QueryPageModel
    {
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
    }
}
