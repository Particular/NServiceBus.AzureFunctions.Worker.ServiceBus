namespace MultiEndpoint.Logging;

using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

class LoggerAdapter(ILogger logger, AsyncLocal<NameSlot> slot) : ILog
{
    public bool IsDebugEnabled => logger.IsEnabled(LogLevel.Debug);

    public bool IsInfoEnabled => logger.IsEnabled(LogLevel.Information);

    public bool IsWarnEnabled => logger.IsEnabled(LogLevel.Warning);

    public bool IsErrorEnabled => logger.IsEnabled(LogLevel.Error);

    public bool IsFatalEnabled => logger.IsEnabled(LogLevel.Critical);

    public void Debug(string? message)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogDebug(message);
    }

    public void Debug(string? message, Exception? exception)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogDebug(exception, message);
    }

    public void DebugFormat(string format, params object?[] args)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogDebug(format, args);
    }

    public void Info(string? message)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogInformation(message);
    }

    public void Info(string? message, Exception? exception)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogInformation(exception, message);
    }

    public void InfoFormat(string format, params object?[] args)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogInformation(format, args);
    }

    public void Warn(string? message)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogWarning(message);
    }

    public void Warn(string? message, Exception? exception)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogWarning(exception, message);
    }

    public void WarnFormat(string format, params object?[] args)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogWarning(format, args);
    }

    public void Error(string? message)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogError(message);
    }

    public void Error(string? message, Exception? exception)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogError(exception, message);
    }

    public void ErrorFormat(string format, params object?[] args)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogError(format, args);
    }

    public void Fatal(string? message)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogCritical(message);
    }

    public void Fatal(string? message, Exception? exception)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogCritical(exception, message);
    }

    public void FatalFormat(string format, params object?[] args)
    {
        using var scope = slot.Value == null ? NullScope.Instance : logger.BeginScope(slot.Value.Format, slot.Value.Args);
        logger.LogCritical(format, args);
    }

    sealed class NullScope :IDisposable
    {
        public static NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
}