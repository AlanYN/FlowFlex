using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Represents a message containing relationship information for a customer.
    /// </summary>
    public class RelationshipMessage
    {
        public long Id { get; set; }
        public long SourceId { get; set; }

        public string SourceCode { get; set; } = "";

        public string SourceName { get; set; } = string.Empty;

        public CustomerTagEnum SourceType { get; set; }
        public string SourceTypeName { get; set; }


        public long TargetId { get; set; }

        public string TargetCode { get; set; }

        public string TargetName { get; set; }

        public CustomerTagEnum TargetType { get; set; }

        public string TargetTypeName { get; set; }
    }
}
