using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Logging;

public class SomeEventMessageHandler(ILogger<SomeEventMessageHandler> logger) : IHandleMessages<SomeEvent>
{
    public Task Handle(SomeEvent message, IMessageHandlerContext context)
    {
        logger.LogWarning($"Handling {nameof(SomeEvent)} in {nameof(SomeEventMessageHandler)}");

        return Task.CompletedTask;
    }
}