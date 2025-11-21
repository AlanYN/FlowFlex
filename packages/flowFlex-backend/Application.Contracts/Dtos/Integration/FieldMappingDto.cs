namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Field mapping DTO
    /// </summary>
    public class FieldMappingDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public long EntityMappingId { get; set; }
        public string ExternalFieldName { get; set; }
        public string ExternalFieldLabel { get; set; }
        public string ExternalFieldType { get; set; }
        public string WfeFieldName { get; set; }
        public string WfeFieldLabel { get; set; }
        public string WfeFieldType { get; set; }
        public string SyncDirection { get; set; }
        public bool IsRequired { get; set; }
        public string TransformationRule { get; set; }
        public string DefaultValue { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Create field mapping DTO
    /// </summary>
    public class CreateFieldMappingDto
    {
        public long IntegrationId { get; set; }
        public long EntityMappingId { get; set; }
        public string ExternalFieldName { get; set; }
        public string ExternalFieldLabel { get; set; }
        public string ExternalFieldType { get; set; }
        public string WfeFieldName { get; set; }
        public string WfeFieldLabel { get; set; }
        public string WfeFieldType { get; set; }
        public string SyncDirection { get; set; }
        public bool IsRequired { get; set; }
        public string TransformationRule { get; set; }
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Update field mapping DTO
    /// </summary>
    public class UpdateFieldMappingDto
    {
        public string ExternalFieldLabel { get; set; }
        public string WfeFieldName { get; set; }
        public string WfeFieldLabel { get; set; }
        public string SyncDirection { get; set; }
        public bool IsRequired { get; set; }
        public string TransformationRule { get; set; }
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Field mapping suggestion DTO
    /// </summary>
    public class FieldMappingSuggestionDto
    {
        public string ExternalFieldName { get; set; }
        public string ExternalFieldType { get; set; }
        public string SuggestedWfeField { get; set; }
        public string SuggestedWfeFieldType { get; set; }
        public double ConfidenceScore { get; set; }
        public string Reason { get; set; }
    }
}
