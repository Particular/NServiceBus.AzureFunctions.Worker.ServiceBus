namespace AssemblyScanningPlayground;
using Messages;

public class HandlerPickedUp : IHandleMessages<MyEvent1>
{
    public Task Handle(MyEvent1 message, IMessageHandlerContext context)
    {
        Console.WriteLine("Handler picked up MyEvent1");
        return Task.CompletedTask;
    }
}