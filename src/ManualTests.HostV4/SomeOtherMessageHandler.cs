using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;

public class SomeOtherMessageHandler(ILogger<SomeOtherMessageHandler> logger) : IHandleMessages<SomeOtherMessage>
{
    public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
    {
        logger.LogWarning($"Handling {nameof(SomeOtherMessage)} in {nameof(SomeOtherMessageHandler)}");

        return Task.CompletedTask;
    }
}