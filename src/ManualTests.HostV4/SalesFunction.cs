using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using NServiceBus;

public partial class SalesFunction : IConfigureEndpoint
{
    [Function(nameof(SalesFunction))]
    // Currently we are bluntly keeing against the nameof(Method) but maybe we should key against the function name instead?
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default);

    public void Configure(EndpointConfiguration endpointConfiguration)
    {
    }
}