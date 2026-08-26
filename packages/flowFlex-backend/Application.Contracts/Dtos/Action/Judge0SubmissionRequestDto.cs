namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Judge0 submission request DTO
    /// </summary>
    public class Judge0SubmissionRequestDto
    {
        /// <summary>
        /// Base64 encoded source code
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>
        /// Language ID (e.g., 71 for Python3)
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Base64 encoded standard input
        /// </summary>
        public string? Stdin { get; set; }

        /// <summary>
        /// Compiler options
        /// </summary>
        public string? CompilerOptions { get; set; }

        /// <summary>
        /// Command line arguments
        /// </summary>
        public string? CommandLineArguments { get; set; }

        /// <summary>
        /// Redirect stderr to stdout
        /// </summary>
        public bool RedirectStderrToStdout { get; set; } = true;

        /// <summary>
        /// CPU time limit in seconds (overrides Judge0 default).
        /// Null means use Judge0 server default.
        /// </summary>
        public double? CpuTimeLimit { get; set; }

        /// <summary>
        /// Wall time limit in seconds (overrides Judge0 default).
        /// Wall time is larger than cpu_time_limit because network I/O does not count toward CPU time
        /// but does count toward wall time.
        /// </summary>
        public double? WallTimeLimit { get; set; }
    }
}