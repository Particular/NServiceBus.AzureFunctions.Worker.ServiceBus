using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

public partial class SalesFunctions
{
    [NServiceBusFunction]
    [Function(nameof(Orders))]
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task Orders(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    // This is the opt-in way, should we consider opt-out?
    [NServiceBusFunction]
    [Function(nameof(CRMIntegration))]
    public partial Task CRMIntegration(
        [ServiceBusTrigger("crm-integration", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    // This show how to allow native functions to also be used
    [Function(nameof(NativeProcessor))]
    public Task NativeProcessor(
        [ServiceBusTrigger("native-processor", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        // custom non-nservicebus processing
        return Task.CompletedTask;
    }
}