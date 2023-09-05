namespace NServiceBus;

using AzureFunctions.Worker.ServiceBus;

class ServerlessInterceptor
{
    readonly ServerlessTransport transport;

    public ServerlessInterceptor(ServerlessTransport transport) => this.transport = transport;

    public IMessageProcessor MessageProcessor => transport.MessageProcessor;
}