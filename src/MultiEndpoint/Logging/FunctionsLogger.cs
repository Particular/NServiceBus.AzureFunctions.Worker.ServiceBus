using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using LogLevel = NServiceBus.Logging.LogLevel;

namespace MultiEndpoint.Logging;

internal class FunctionsLogger : ILog
{
    private readonly ThreadLocal<Slot> slots = new(static () => new Slot());
    private LoggerAdapter? logger;
    private readonly AsyncLocal<string> nameSlot;
    private readonly string loggerName;

    public FunctionsLogger(AsyncLocal<string> nameSlot, ILoggerFactory? loggerFactory, string loggerName)
    {
        this.nameSlot = nameSlot;
        this.loggerName = loggerName;

        if (loggerFactory != null)
        {
            this.logger = new LoggerAdapter(loggerFactory.CreateLogger(this.loggerName), this.nameSlot);
        }
    }

    public void Flush(ILoggerFactory loggerFactory)
    {
        this.logger = new LoggerAdapter(loggerFactory.CreateLogger(loggerName), nameSlot);

        Flush(this.logger, slots.Value);
    }

    static void Flush(LoggerAdapter logger, Slot slot)
    {
        while (slot.DeferredMessageLogs.TryDequeue(out var entry))
        {
            switch (entry.level)
            {
                case LogLevel.Debug:
                    logger.Debug(entry.message);
                    break;
                case LogLevel.Info:
                    logger.Info(entry.message);
                    break;
                case LogLevel.Warn:
                    logger.Warn(entry.message);
                    break;
                case LogLevel.Error:
                    logger.Error(entry.message);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal(entry.message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        while (slot.DeferredExceptionLogs.TryDequeue(out var entry))
        {
            switch (entry.level)
            {
                case LogLevel.Debug:
                    logger.Debug(entry.message, entry.exception);
                    break;
                case LogLevel.Info:
                    logger.Info(entry.message, entry.exception);
                    break;
                case LogLevel.Warn:
                    logger.Warn(entry.message, entry.exception);
                    break;
                case LogLevel.Error:
                    logger.Error(entry.message, entry.exception);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal(entry.message, entry.exception);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        while (slot.DeferredFormatLogs.TryDequeue(out var entry))
        {
            switch (entry.level)
            {
                case LogLevel.Debug:
                    logger.DebugFormat(entry.format, entry.args);
                    break;
                case LogLevel.Info:
                    logger.InfoFormat(entry.format, entry.args);
                    break;
                case LogLevel.Warn:
                    logger.WarnFormat(entry.format, entry.args);
                    break;
                case LogLevel.Error:
                    logger.ErrorFormat(entry.format, entry.args);
                    break;
                case LogLevel.Fatal:
                    logger.FatalFormat(entry.format, entry.args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void Debug(string? message)
    {
        if (logger != null)
        {
            logger.Debug(message);
            return;
        }

        slots.Value.DeferredMessageLogs.Enqueue((LogLevel.Debug, message));
    }

    public void Debug(string? message, Exception? exception)
    {
        if (logger != null)
        {
            logger.Debug(message, exception);
            return;
        }

        slots.Value.DeferredExceptionLogs.Enqueue((LogLevel.Debug, message, exception));
    }

    public void DebugFormat(string format, params object?[] args)
    {
        if (logger != null)
        {
            logger.DebugFormat(format, args);
            return;
        }

        slots.Value.DeferredFormatLogs.Enqueue((LogLevel.Debug, format, args));
    }

    public void Info(string? message)
    {
        if (logger != null)
        {
            logger.Info(message);
            return;
        }

        slots.Value.DeferredMessageLogs.Enqueue((LogLevel.Info, message));
    }

    public void Info(string? message, Exception? exception)
    {
        if (logger != null)
        {
            logger.Info(message, exception);
            return;
        }

        slots.Value.DeferredExceptionLogs.Enqueue((LogLevel.Info, message, exception));
    }

    public void InfoFormat(string format, params object?[] args)
    {
        if (logger != null)
        {
            logger.InfoFormat(format, args);
            return;
        }

        slots.Value.DeferredFormatLogs.Enqueue((LogLevel.Info, format, args));
    }

    public void Warn(string? message)
    {
        if (logger != null)
        {
            logger.Warn(message);
            return;
        }

        slots.Value.DeferredMessageLogs.Enqueue((LogLevel.Warn, message));
    }

    public void Warn(string? message, Exception? exception)
    {
        if (logger != null)
        {
            logger.Warn(message, exception);
            return;
        }

        slots.Value.DeferredExceptionLogs.Enqueue((LogLevel.Warn, message, exception));
    }

    public void WarnFormat(string format, params object?[] args)
    {
        if (logger != null)
        {
            logger.WarnFormat(format, args);
            return;
        }

        slots.Value.DeferredFormatLogs.Enqueue((LogLevel.Warn, format, args));
    }

    public void Error(string message)
    {
        if (logger != null)
        {
            logger.Error(message);
            return;
        }

        slots.Value.DeferredMessageLogs.Enqueue((LogLevel.Error, message));
    }

    public void Error(string? message, Exception? exception)
    {
        if (logger != null)
        {
            logger.Error(message, exception);
            return;
        }

        slots.Value.DeferredExceptionLogs.Enqueue((LogLevel.Error, message, exception));
    }

    public void ErrorFormat(string format, params object?[] args)
    {
        if (logger != null)
        {
            logger.ErrorFormat(format, args);
            return;
        }

        slots.Value.DeferredFormatLogs.Enqueue((LogLevel.Error, format, args));
    }

    public void Fatal(string? message)
    {
        if (logger != null)
        {
            logger.Fatal(message);
            return;
        }

        slots.Value.DeferredMessageLogs.Enqueue((LogLevel.Fatal, message));
    }

    public void Fatal(string? message, Exception? exception)
    {
        if (logger != null)
        {
            logger.Fatal(message, exception);
            return;
        }

        slots.Value.DeferredExceptionLogs.Enqueue((LogLevel.Fatal, message, exception));
    }

    public void FatalFormat(string format, params object?[] args)
    {
        if (logger != null)
        {
            logger.FatalFormat(format, args);
            return;
        }

        slots.Value.DeferredFormatLogs.Enqueue((LogLevel.Fatal, format, args));
    }

    // capturing everything just in case when the logger is not yet set
    public bool IsDebugEnabled => logger == null || logger.IsDebugEnabled;

    public bool IsInfoEnabled => logger == null || logger.IsInfoEnabled;

    public bool IsWarnEnabled => logger == null || logger.IsWarnEnabled;

    public bool IsErrorEnabled => logger == null || logger.IsErrorEnabled;

    public bool IsFatalEnabled => logger == null || logger.IsFatalEnabled;

    sealed class Slot
    {
        public readonly ConcurrentQueue<(LogLevel level, string? message)> DeferredMessageLogs = new();

        public readonly ConcurrentQueue<(LogLevel level, string? message, Exception? exception)> DeferredExceptionLogs =
            new();

        public readonly ConcurrentQueue<(LogLevel level, string format, object?[] args)> DeferredFormatLogs = new();
    }
}