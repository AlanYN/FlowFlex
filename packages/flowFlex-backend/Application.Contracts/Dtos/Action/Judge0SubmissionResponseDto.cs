namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Judge0 submission response DTO
    /// </summary>
    public class Judge0SubmissionResponseDto
    {
        /// <summary>
        /// Submission token
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Judge0 submission result DTO
    /// </summary>
    public class Judge0SubmissionResultDto
    {
        /// <summary>
        /// Base64 encoded standard output
        /// </summary>
        public string? Stdout { get; set; }

        /// <summary>
        /// Execution time in seconds
        /// </summary>
        public string? Time { get; set; }

        /// <summary>
        /// Memory usage in KB
        /// </summary>
        public int? Memory { get; set; }

        /// <summary>
        /// Standard error output
        /// </summary>
        public string? Stderr { get; set; }

        /// <summary>
        /// Submission token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Compile output
        /// </summary>
        public string? CompileOutput { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Execution status
        /// </summary>
        public Judge0StatusDto? Status { get; set; }
    }

    /// <summary>
    /// Judge0 status DTO
    /// </summary>
    public class Judge0StatusDto
    {
        /// <summary>
        /// Status ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Status description
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
} 