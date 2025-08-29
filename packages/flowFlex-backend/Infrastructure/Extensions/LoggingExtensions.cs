using Microsoft.Extensions.Logging;

namespace FlowFlex.Infrastructure.Extensions;

/// <summary>
/// Logging extensions to replace Console.WriteLine with minimal code changes
/// </summary>
public static class LoggingExtensions
{
    private static ILogger? _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Drop-in replacement for Console.WriteLine with structured logging
    /// </summary>
    public static void WriteLine(string message)
    {
        if (_logger != null)
        {
            _logger.LogInformation("{Message}", message);
        }
        else
        {
            // Fallback to console in case logger is not set
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Log error with structured logging
    /// </summary>
    public static void WriteError(string message)
    {
        if (_logger != null)
        {
            _logger.LogError("{Message}", message);
        }
        else
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// Log warning with structured logging  
    /// </summary>
    public static void WriteWarning(string message)
    {
        if (_logger != null)
        {
            _logger.LogWarning("{Message}", message);
        }
        else
        {
            Console.WriteLine($"[WARNING] {message}");
        }
    }
}