namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public partial class LessBoilerplateFunction : IConfigureEndpoint
{
    // Would this actually work though, as is? Because the Functions metadata generator
    // would not see this at all, so would our own source generator need to create another
    // metadata provider that provides this information to the Functions runtime instead?
    [Function("Finance")]
    public async Task _ExecuteFunction(
        [ServiceBusTrigger("finance-queue", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        [FromKeyedServices(nameof(LessBoilerplateFunction))] FunctionEndpoint endpoint,
        [FromKeyedServices(nameof(LessBoilerplateFunction))] IMessageSession session,
        CancellationToken cancellationToken = default)
    {
        await endpoint.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    // We could make these partial and register on the root DI. Then you can inject stuff into Configure
    public class LessBoilerplateFunctionInitializationContext : InitializationContext;

    partial void Configure(LessBoilerplateFunctionInitializationContext context);

    public void Configure(InitializationContext context)
    {
        switch (context)
        {
            case LessBoilerplateFunctionInitializationContext salesContext:
                Configure(salesContext);
                break;
            default:
                break;
        }
    }
}