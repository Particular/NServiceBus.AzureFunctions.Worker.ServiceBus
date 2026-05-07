namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_sagas
    {
        [Test]
        public async Task Should_invoke_saga_message_handlers()
        {
            var context = await Scenario.Define<Context>()
                .WithComponent(new SendingFunction())
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.CounterValue, Is.EqualTo(42));
        }

        class Context : ScenarioContext
        {
            public int CounterValue { get; set; }
        }

        class SendingFunction : FunctionEndpointComponent
        {
            public SendingFunction()
            {
                CustomizeConfiguration = configuration =>
                    configuration.AdvancedConfiguration.UsePersistence<LearningPersistence>();

                var correlationProperty = Guid.NewGuid().ToString("N");
                AddTestMessage(new StartSagaMessage { CorrelationProperty = correlationProperty });
                AddTestMessage(new UpdateSagaMessage { CorrelationProperty = correlationProperty, UpdateValue = 42 });
                AddTestMessage(new ReadSagaDataValueMessage { CorrelationProperty = correlationProperty });
            }

            class DemoSaga(Context testContext) : Saga<DemoSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<UpdateSagaMessage>,
                IHandleMessages<ReadSagaDataValueMessage>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DemoSagaData> mapper) =>
                    mapper.MapSaga(s => s.CorrelationProperty)
                        .ToMessage<StartSagaMessage>(m => m.CorrelationProperty)
                        .ToMessage<UpdateSagaMessage>(m => m.CorrelationProperty)
                        .ToMessage<ReadSagaDataValueMessage>(m => m.CorrelationProperty);

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeCounter = 1;
                    return Task.CompletedTask;
                }

                public Task Handle(UpdateSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeCounter = message.UpdateValue;
                    return Task.CompletedTask;
                }

                public Task Handle(ReadSagaDataValueMessage message, IMessageHandlerContext context)
                {
                    testContext.CounterValue = Data.SomeCounter;
                    MarkAsComplete();
                    return Task.CompletedTask;
                }
            }

            class DemoSagaData : ContainSagaData
            {
                public string CorrelationProperty { get; set; }
                public int SomeCounter { get; set; }
            }
        }

        class StartSagaMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
        }

        class UpdateSagaMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
            public int UpdateValue { get; set; }
        }

        class ReadSagaDataValueMessage : IMessage
        {
            public string CorrelationProperty { get; set; }
        }
    }
}
