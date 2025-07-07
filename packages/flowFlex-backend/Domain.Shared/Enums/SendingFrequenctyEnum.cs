using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum SendingFrequencyEnum
    {
        [Description("Hold Email")]
        HoldEmail = 1,
        [Description("Standard Email")]
        StandardEmail = 2,
        [Description("Daily Email Updates")]
        DailyEmailUpdates = 3,
        [Description("Weekly Email Updates")]
        WeeklyEmailUpdates = 4
    }
}
