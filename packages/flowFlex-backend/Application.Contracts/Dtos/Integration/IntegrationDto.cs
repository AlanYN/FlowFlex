namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Integration DTO
    /// </summary>
    public class IntegrationDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SystemName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string EndpointUrl { get; set; }
        public string AuthMethod { get; set; }
        public int ConfiguredEntityTypes { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// Create integration DTO
    /// </summary>
    public class CreateIntegrationDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string SystemName { get; set; }
        public string Description { get; set; }
        public string EndpointUrl { get; set; }
        public string AuthMethod { get; set; }
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BearerToken { get; set; }
        public string OAuth2Config { get; set; }
    }

    /// <summary>
    /// Update integration DTO
    /// </summary>
    public class UpdateIntegrationDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string EndpointUrl { get; set; }
        public string AuthMethod { get; set; }
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BearerToken { get; set; }
        public string OAuth2Config { get; set; }
    }

    /// <summary>
    /// Test connection result DTO
    /// </summary>
    public class TestConnectionDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime TestedAt { get; set; }
        public int ResponseTimeMs { get; set; }
    }

    /// <summary>
    /// Integration query request
    /// </summary>
    public class IntegrationQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string SortField { get; set; } = "CreateDate";
        public string SortDirection { get; set; } = "desc";
    }
}
