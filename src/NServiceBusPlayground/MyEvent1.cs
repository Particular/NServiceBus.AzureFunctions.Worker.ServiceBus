namespace Messages;
using NServiceBus;

public class MyEvent1 : IEvent
{
    public string SomeProperty { get; set; }
}