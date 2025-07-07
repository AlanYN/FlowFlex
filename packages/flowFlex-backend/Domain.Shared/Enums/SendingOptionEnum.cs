using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum SendingOptionEnum
    {
        Mail = 1,
        Email = 2,
        [Description("Mail+Email")]
        MailEmail = 3,
        [Description("EDI 210")]
        EDI = 4
    }
}
