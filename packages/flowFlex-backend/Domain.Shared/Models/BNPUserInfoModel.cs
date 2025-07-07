using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Models
{
    public class BNPUserInfoModel : BaseUserInfoModel
    {
        public int ClientId { get; set; }

        public Dictionary<int, string> Clients { get; set; }
    }
}
