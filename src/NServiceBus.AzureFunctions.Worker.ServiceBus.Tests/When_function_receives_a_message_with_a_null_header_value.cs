﻿namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_function_receives_a_message_with_a_null_header_value
{
    [Test]
    public async Task Should_process_the_message()
    {
        var headerKey = "MyNullHeader";

        var context = await Scenario.Define<Context>()
            .WithComponent(new FunctionHandler(headerKey))
            .Done(c => c.HandlerInvocationCount > 0)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.HandlerInvocationCount, Is.EqualTo(1));
            Assert.That(context.Headers.ContainsKey(headerKey), Is.True);
            Assert.That(context.Headers[headerKey], Is.Null);
        });
    }

    public class Context : ScenarioContext
    {
        public int HandlerInvocationCount => count;

        public IReadOnlyDictionary<string, string> Headers { get; set; }

        public void HandlerInvoked() => Interlocked.Increment(ref count);

        int count;
    }

    class FunctionHandler : FunctionEndpointComponent
    {
        public FunctionHandler(string headerKey) => AddTestMessage(new MessageWithNullHeader(), new Dictionary<string, object> { { headerKey, null } });

        class MessageWithNullHeaderHandler(Context testContext) : IHandleMessages<MessageWithNullHeader>
        {
            public Task Handle(MessageWithNullHeader message, IMessageHandlerContext context)
            {
                testContext.Headers = context.MessageHeaders;
                testContext.HandlerInvoked();
                return Task.CompletedTask;
            }
        }
    }

    class MessageWithNullHeader : IMessage;
}