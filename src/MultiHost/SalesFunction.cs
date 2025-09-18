namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    public void Configure(EndpointConfiguration endpointConfiguration)
    {
    }
}

public partial class SalesFunction
{
    public async partial Task Sales(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
    }
}