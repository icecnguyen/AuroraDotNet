using System;
using System.Collections.Concurrent;

namespace Aurora.Core.Logging;

/// <summary>
/// A simple, lock-free, zero-dependency logging manager.
/// </summary>
public static class LogManager
{
    private static readonly ConcurrentDictionary<string, ILogger> Loggers = new();
    
    // Default factory could be replaced by a proper sink later
    public static Func<string, ILogger> LoggerFactory { get; set; } = category => new ConsoleLogger(category);

    public static ILogger GetLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        return Loggers.GetOrAdd(categoryName, LoggerFactory);
    }
}

internal sealed class ConsoleLogger : ILogger
{
    private readonly string _category;

    public ConsoleLogger(string category)
    {
        _category = category;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        // Simple console sink for MVP
        Console.WriteLine($"[{DateTime.UtcNow:O}] [{level}] [{_category}] {message}");
        if (exception != null)
        {
            Console.WriteLine(exception);
        }
    }
}
