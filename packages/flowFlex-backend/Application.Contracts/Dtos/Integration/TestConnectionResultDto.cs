namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Test connection result DTO
    /// </summary>
    public class TestConnectionResultDto
    {
        /// <summary>
        /// Whether the connection test succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if test failed, or success message if test succeeded
        /// </summary>
        public string Msg { get; set; } = string.Empty;
    }
}

