namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

sealed class FakeFunctionContext : FunctionContext
{
    public override string InvocationId { get; }
    public override string FunctionId { get; }
    public override TraceContext TraceContext { get; }
    public override BindingContext BindingContext { get; }
    public override RetryContext RetryContext { get; }
    public override IServiceProvider InstanceServices { get; set; }
    public override FunctionDefinition FunctionDefinition { get; }
#pragma warning disable PS0025 // Dictionary keys should implement IEquatable<T> - Overriding Microsoft type for test
    public override IDictionary<object, object> Items { get; set; }
#pragma warning restore PS0025 // Dictionary keys should implement IEquatable<T>
    public override IInvocationFeatures Features { get; }
}

class TestLoggingFactory : ILoggerFactory
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName) => new DummyLogger();

    public void AddProvider(ILoggerProvider provider)
    {
    }
}

class DummyLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
}