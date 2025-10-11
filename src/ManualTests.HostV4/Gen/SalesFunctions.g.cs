using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using NServiceBus;

public partial class SalesFunctions
{
    public partial Task Orders(
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        //process the message
        return Task.CompletedTask;
    }
    
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task CRMIntegration(
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        //process the message
        return Task.CompletedTask;
    }
}