using System.Collections.Concurrent;
using NServiceBus.Logging;
using ILoggerFactory = NServiceBus.Logging.ILoggerFactory;

namespace MultiEndpoint.Logging;

public class FunctionsLoggerFactory : ILoggerFactory
{
    private FunctionsLoggerFactory()
    {
    }

    public static FunctionsLoggerFactory Instance = new();

    ConcurrentDictionary<string, FunctionsLogger> loggers = new();
    private readonly AsyncLocal<string> nameSlot = new();
    private Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory;

    public ILog GetLogger(Type type)
    {
        return loggers.GetOrAdd(type.FullName, name => new FunctionsLogger(nameSlot, loggerFactory, name));
    }

    public ILog GetLogger(string name)
    {
        return loggers.GetOrAdd(name, name => new FunctionsLogger(nameSlot, loggerFactory, name));
    }

    public void SetLoggerFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;

        foreach (var (_, logger) in loggers)
        {
            logger.Flush(loggerFactory);
        }
    }

    public void SetName(string name)
    {
        nameSlot.Value = name;
    }
}