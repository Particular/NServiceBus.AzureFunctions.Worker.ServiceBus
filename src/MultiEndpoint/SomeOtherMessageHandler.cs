using NServiceBus.Logging;

public class SomeOtherMessageHandler : IHandleMessages<SomeOtherMessage>
{
    // using static logger here deliberately
    static readonly ILog Log = LogManager.GetLogger<SomeOtherMessageHandler>();

    public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
    {
        Log.Warn($"Handling {nameof(SomeOtherMessage)} in {nameof(SomeOtherMessageHandler)}");

        return Task.CompletedTask;
    }
}