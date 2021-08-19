namespace test
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

    public class MessageHandler : IHandleMessages<TriggerMessage>
    {
        public Task Handle(TriggerMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine("received message!");
            return Task.CompletedTask;
        }
    }
}