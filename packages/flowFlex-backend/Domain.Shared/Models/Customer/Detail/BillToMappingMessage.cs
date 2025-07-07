using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared.Enums.Unis.Relationship;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class BillToMappingMessage : DataMessageBase
    {
        /// <summary>
        /// The Customer associated with this BillTo. Only used when the company is UF, to record which CustomerId the BillTo is associated with.
        /// </summary>
        public long? BillTo { get; set; }

        /// <summary>
        /// The code associated with this BillTo.
        /// </summary>
        public string BillToCode { get; set; }

        /// <summary>
        /// The ID of the third party.
        /// </summary>
        public string ThirdPartyId { get; set; }

        /// <summary>
        /// The name of the third party.
        /// </summary>
        public string ThirdPartyName { get; set; }

        /// <summary>
        /// The ID of the program.
        /// </summary>
        public int? ProgramId { get; set; }

        /// <summary>
        /// The ID of the title.
        /// </summary>
        public string TitleId { get; set; }

        /// <summary>
        /// The name of the title.
        /// </summary>
        public string TitleName { get; set; }

        /// <summary>
        /// The source of synchronization. Indicates where the data was synchronized from. Currently, only title is required.
        /// </summary>
        public SyncSourceEnum SyncSource { get; set; }

        /// <summary>
        /// The ID of the synchronization source. Indicates the ID of where the data was synchronized from. Currently, only title is required.
        /// </summary>
        public long? SyncSourceId { get; set; }
        public string BillToName { get; set; }
        public string BillToFullName { get; set; }
    }
}
