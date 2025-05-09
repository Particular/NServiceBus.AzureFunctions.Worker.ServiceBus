﻿namespace NServiceBus.AzureFunctions.Worker.ServiceBus;

using System;
using System.Threading;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ILoggerFactory = Logging.ILoggerFactory;

class FunctionsLoggerFactory : ILoggerFactory
{
    public static FunctionsLoggerFactory Instance { get; } = new FunctionsLoggerFactory();

    Logger log;

    AsyncLocal<ILogger> logger = new AsyncLocal<ILogger>();

    FunctionsLoggerFactory() => log = new Logger(logger);

    public void SetCurrentLogger(ILogger currentLogger)
    {
        var newLogger = currentLogger ?? NullLogger.Instance;

        logger.Value = newLogger;
        log.Flush(newLogger);
    }

    public ILog GetLogger(Type type) => log;

    public ILog GetLogger(string name) => log;
}