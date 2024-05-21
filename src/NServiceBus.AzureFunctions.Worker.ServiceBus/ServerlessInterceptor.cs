namespace NServiceBus;

using AzureFunctions.Worker.ServiceBus;

class ServerlessInterceptor(ServerlessTransport transport)
{
    public IMessageProcessor MessageProcessor => transport.MessageProcessor;
}