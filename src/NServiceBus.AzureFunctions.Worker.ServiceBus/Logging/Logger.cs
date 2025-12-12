namespace NServiceBus.AzureFunctions.Worker.ServiceBus;

using System;
using System.Collections.Concurrent;
using System.Threading;
using Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

class Logger(AsyncLocal<ILogger> logger) : ILog
{
    public bool IsDebugEnabled => logger.Value?.IsEnabled(LogLevel.Debug) ?? true;
    public bool IsInfoEnabled => logger.Value?.IsEnabled(LogLevel.Information) ?? true;
    public bool IsWarnEnabled => logger.Value?.IsEnabled(LogLevel.Warning) ?? true;
    public bool IsErrorEnabled => logger.Value?.IsEnabled(LogLevel.Error) ?? true;
    public bool IsFatalEnabled => logger.Value?.IsEnabled(LogLevel.Critical) ?? true;

    void Log(LogLevel level, string message)
    {
        var concreteLogger = logger.Value;
        if (concreteLogger == null)
        {
            DeferredMessageLogs.Enqueue((level, message));
            return;
        }
        concreteLogger.Log(level, message);
    }

    void Log(LogLevel level, string message, Exception exception)
    {
        var concreteLogger = logger.Value;
        if (concreteLogger == null)
        {
            DeferredExceptionLogs.Enqueue((level, message, exception));
            return;
        }
        concreteLogger.Log(level, exception, message);
    }

    void Log(LogLevel level, string format, object[] args)
    {
        var concreteLogger = logger.Value;
        if (concreteLogger == null)
        {
            DeferredFormatLogs.Enqueue((level, format, args));
            return;
        }
        concreteLogger.Log(level, format, args);
    }

    public void Debug(string message) => Log(LogLevel.Debug, message);

    public void Debug(string message, Exception exception) => Log(LogLevel.Debug, message, exception);

    public void DebugFormat(string format, params object[] args) => Log(LogLevel.Debug, format, args);

    public void Info(string message) => Log(LogLevel.Information, message);

    public void Info(string message, Exception exception) => Log(LogLevel.Information, message, exception);

    public void InfoFormat(string format, params object[] args) => Log(LogLevel.Information, format, args);

    public void Warn(string message) => Log(LogLevel.Warning, message);

    public void Warn(string message, Exception exception) => Log(LogLevel.Warning, message, exception);

    public void WarnFormat(string format, params object[] args) => Log(LogLevel.Warning, format, args);

    public void Error(string message) => Log(LogLevel.Error, message);

    public void Error(string message, Exception exception) => Log(LogLevel.Error, message, exception);

    public void ErrorFormat(string format, params object[] args) => Log(LogLevel.Error, format, args);

    public void Fatal(string message) => Log(LogLevel.Critical, message);

    public void Fatal(string message, Exception exception) => Log(LogLevel.Critical, message, exception);

    public void FatalFormat(string format, params object[] args) => Log(LogLevel.Critical, format, args);

    internal void Flush(ILogger concreteLogger)
    {
        while (DeferredMessageLogs.TryDequeue(out var entry))
        {
            concreteLogger.Log(entry.level, entry.message);
        }

        while (DeferredExceptionLogs.TryDequeue(out var entry))
        {
            concreteLogger.Log(entry.level, entry.exception, entry.message);
        }

        while (DeferredFormatLogs.TryDequeue(out var entry))
        {
            concreteLogger.Log(entry.level, entry.format, entry.args);
        }
    }

    readonly ConcurrentQueue<(LogLevel level, string message)> DeferredMessageLogs = new ConcurrentQueue<(LogLevel level, string message)>();
    readonly ConcurrentQueue<(LogLevel level, string message, Exception exception)> DeferredExceptionLogs = new ConcurrentQueue<(LogLevel level, string message, Exception exception)>();
    readonly ConcurrentQueue<(LogLevel level, string format, object[] args)> DeferredFormatLogs = new ConcurrentQueue<(LogLevel level, string format, object[] args)>();
}