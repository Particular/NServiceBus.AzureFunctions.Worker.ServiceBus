namespace MultiEndpoint;

using Microsoft.Extensions.Logging;

public class TriggerMessageHandler(ILogger<TriggerMessageHandler> logger) : IHandleMessages<TriggerMessage>
{
    public async Task Handle(TriggerMessage message, IMessageHandlerContext context)
    {
        logger.LogWarning($"Handling {nameof(TriggerMessage)} in {nameof(TriggerMessageHandler)}");

        await context.SendLocal(new SomeOtherMessage()).ConfigureAwait(false);
        await context.Publish(new SomeEvent()).ConfigureAwait(false);
    }
}