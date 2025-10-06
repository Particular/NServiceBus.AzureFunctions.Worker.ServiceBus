using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using NServiceBus;

public partial class SalesFunction
{
    public partial Task Sales(
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        //process the message
        return Task.CompletedTask;
    }
}