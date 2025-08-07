namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Python action configuration DTO
    /// </summary>
    public class PythonActionConfigDto
    {
        /// <summary>
        /// Python script source code
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>
        /// Command line arguments for the script
        /// </summary>
        public string? CommandLineArguments { get; set; }

        /// <summary>
        /// Standard input for the script (Console.ReadLine input)
        /// </summary>
        public string? Stdin { get; set; }
    }
}