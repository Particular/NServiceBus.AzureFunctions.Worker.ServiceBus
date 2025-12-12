using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MultiEndpoint.Logging;

class LoggerAdapter(ILogger logger, AsyncLocal<string> name) : ILog
{
    public bool IsDebugEnabled => logger.IsEnabled(LogLevel.Debug);

    public bool IsInfoEnabled => logger.IsEnabled(LogLevel.Information);

    public bool IsWarnEnabled => logger.IsEnabled(LogLevel.Warning);

    public bool IsErrorEnabled => logger.IsEnabled(LogLevel.Error);

    public bool IsFatalEnabled => logger.IsEnabled(LogLevel.Critical);

    public void Debug(string? message)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogDebug(message);
    }

    public void Debug(string? message, Exception? exception)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogDebug(exception, message);
    }

    public void DebugFormat(string format, params object?[] args)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogDebug(format, args);
    }

    public void Info(string? message)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogInformation(message);
    }

    public void Info(string? message, Exception? exception)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogInformation(exception, message);
    }

    public void InfoFormat(string format, params object?[] args)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogInformation(format, args);
    }

    public void Warn(string? message)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogWarning(message);
    }

    public void Warn(string? message, Exception? exception)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogWarning(exception, message);
    }

    public void WarnFormat(string format, params object?[] args)
    {
        using var scope = logger.BeginScope(name.Value ?? string.Empty);
        logger.LogWarning(format, args);
    }

    public void Error(string? message) => logger.LogError(message);

    public void Error(string? message, Exception? exception) => logger.LogError(exception, message);

    public void ErrorFormat(string format, params object?[] args) => logger.LogError(format, args);

    public void Fatal(string? message) => logger.LogCritical(message);

    public void Fatal(string? message, Exception? exception) => logger.LogCritical(exception, message);

    public void FatalFormat(string format, params object?[] args) => logger.LogCritical(format, args);

    readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
}