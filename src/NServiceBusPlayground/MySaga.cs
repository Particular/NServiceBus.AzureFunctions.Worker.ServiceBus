namespace AssemblyScanningPlayground;
using Messages;


public class MySaga : Saga<MySagaData>, IAmStartedByMessages<MyEvent1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
    {
        mapper.MapSaga(sagaData => sagaData.SomeProperty)
            .ToMessage<MyEvent1>(message => message.SomeProperty);
    }

    public Task Handle(MyEvent1 message, IMessageHandlerContext context)
    {
        MarkAsComplete();
        Console.WriteLine("MySaga completed");
        return Task.CompletedTask;
    }
}

public class MySagaData : ContainSagaData
{
    public string SomeProperty { get; set; }
}