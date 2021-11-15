﻿namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    sealed class FakeFunctionContext : FunctionContext
    {
        public FakeFunctionContext()
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(new TestLoggingFactory());

            InstanceServices = sc.BuildServiceProvider();
        }

        public override string InvocationId { get; }
        public override string FunctionId { get; }
        public override TraceContext TraceContext { get; }
        public override BindingContext BindingContext { get; }
        public override IServiceProvider InstanceServices { get; set; }
        public override FunctionDefinition FunctionDefinition { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IInvocationFeatures Features { get; }
    }

    class TestLoggingFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DummyLogger();
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }

    class DummyLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}