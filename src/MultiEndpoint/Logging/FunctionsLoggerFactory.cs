namespace MultiEndpoint.Logging;

using System.Collections.Concurrent;
using NServiceBus.Logging;
using ILoggerFactory = NServiceBus.Logging.ILoggerFactory;

public class FunctionsLoggerFactory : ILoggerFactory
{
    FunctionsLoggerFactory()
    {
    }

    public static FunctionsLoggerFactory Instance = new();

    ConcurrentDictionary<string, FunctionsLogger> loggers = new();
    readonly AsyncLocal<string> nameSlot = new();
    Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory;

    public ILog GetLogger(Type type) => loggers.GetOrAdd(type.FullName, name => new FunctionsLogger(nameSlot, loggerFactory, name));

    public ILog GetLogger(string name) => loggers.GetOrAdd(name, name => new FunctionsLogger(nameSlot, loggerFactory, name));

    public void SetLoggerFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;

        foreach (var (_, logger) in loggers)
        {
            logger.Flush(loggerFactory);
        }
    }

    public NameScope PushName(string name)
    {
        var previous = nameSlot.Value;
        nameSlot.Value = name;
        return new NameScope(nameSlot, previous);
    }

    public readonly struct NameScope(AsyncLocal<string> slot, string? previous) : IDisposable
    {
        public void Dispose() => slot.Value = previous;
    }
}