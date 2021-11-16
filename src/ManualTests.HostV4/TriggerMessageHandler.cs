using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class TriggerMessageHandler : IHandleMessages<TriggerMessage>
{
    static readonly ILog Log = LogManager.GetLogger<TriggerMessageHandler>();

    public Task Handle(TriggerMessage message, IMessageHandlerContext context)
    {
        Log.Warn($"Handling {nameof(TriggerMessage)} in {nameof(TriggerMessageHandler)}");

        throw new System.Exception("boom");

        //return Task.CompletedTask;
    }
}