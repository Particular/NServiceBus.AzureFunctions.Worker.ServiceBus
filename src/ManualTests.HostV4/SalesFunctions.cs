using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

public partial class SalesFunctions
{
    [Function(nameof(Orders))]
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task Orders(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    [Function(nameof(CRMIntegration))]
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task CRMIntegration(
        [ServiceBusTrigger("crm-integration", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);
}