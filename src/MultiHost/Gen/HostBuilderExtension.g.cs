namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public static class FunctionsWorkerApplicationBuilderExtensions
{
    // It might even be possible to source generate this entire method or we seperate it into a generated method and a core method
    // Should we also pass in the service collection of the endpoint here too? Or we could pass on the initialization context as the base type
    // we also need to think about the expectation of DI registrations and the split brain between service collection here vs collection per endpont
    // and what needs to be forwarded https://github.com/Particular/docs.particular.net/pull/4813/files
    // We need to extend IFunctionsWorkerApplicationBuilder to have all the power available in the endpoint configuration
    public static void AddNServiceBus(this IFunctionsWorkerApplicationBuilder builder, Action<EndpointConfiguration>? customize = null)
    {
        // Generated part?
        builder.Services.AddKeyedSingleton<IConfigureEndpoint>(nameof(BillingFunctions),
            (provider, o) => new BillingFunctions());
        // Ordering might be tricky
        builder.Services.AddHostedService<FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>>(sp =>
            new FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>(
                nameof(BillingFunctions.Billing), customize ?? (_ => { }),
                sp.GetRequiredKeyedService<IConfigureEndpoint>(nameof(BillingFunctions))));
        builder.Services.AddKeyedSingleton<Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task>>(
            nameof(BillingFunctions.Billing),
            (provider, key) =>
                provider
                    .GetRequiredKeyedService<
                        FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>>(key).Processor);
        builder.Services.AddKeyedSingleton<FunctionEndpoint>(nameof(BillingFunctions.Billing),
            (provider, key) =>
                new FunctionEndpoint(provider
                    .GetRequiredKeyedService<
                        Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task>>(key)));
    }
}