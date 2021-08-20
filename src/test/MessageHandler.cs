namespace test
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NServiceBus;
    using NServiceBus.Logging;

    public class MessageHandler : IHandleMessages<TriggerMessage>
    {
        ILog logger = LogManager.GetLogger<MessageHandler>();
        ILogger<MessageHandler> msLogger;

        public MessageHandler(ILogger<MessageHandler> msLogger)
        {
            this.msLogger = msLogger;
        }

        public Task Handle(TriggerMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine("received message!");
            logger.Warn("Warn Logmessage from message handler");
            logger.Info("Info Logmessage from message handler");
            logger.Debug("Debug Logmessage from message handler");

            msLogger.LogWarning("Warn MS logger message from message handler");
            msLogger.LogInformation("Info MS logger message from message handler");
            msLogger.LogDebug("Debug MS logger message from message handler");
            return Task.CompletedTask;
        }
    }
}