using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum InvoiceEnum
    {
        Summary = 1,
        Detail = 2,
        [Description("Summary+Detail")]
        SummaryDetail = 3,
        [Description("Standard Invoice")]
        StandardInvoice = 4,
        [Description("Full Page BOL")]
        FullPageBOL = 5,
        [Description("Full Page POD")]
        FullPagePOD = 6,
        [Description("All Full Page")]
        AllFullPage = 7,
        [Description("Zep Invoice")]
        ZepInvoice = 8
    }
}
