namespace AssemblyScanningPlayground;

using Messages;

public class HandlerNotPickedUp : IHandleMessages<MyEvent2>
{
    public Task Handle(MyEvent2 message, IMessageHandlerContext context)
    {
        Console.WriteLine("Handler picked up MyEvent2");
        return Task.CompletedTask;
    }
}