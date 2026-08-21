using System;

namespace Aurora.Core.Logging;

public interface ILogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
    void Trace(string message) => Log(LogLevel.Trace, message);
    void Debug(string message) => Log(LogLevel.Debug, message);
    void Information(string message) => Log(LogLevel.Information, message);
    void Warning(string message, Exception? exception = null) => Log(LogLevel.Warning, message, exception);
#pragma warning disable CA1716
    void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
#pragma warning restore CA1716
    void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}
