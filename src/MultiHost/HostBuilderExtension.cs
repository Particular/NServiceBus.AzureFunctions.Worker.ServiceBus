namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // It might even be possible to source generate this entire method or we seperate it into a generated method and a core method
    public static void AddNServiceBus(this IServiceCollection services, Action<EndpointConfiguration>? customize = null)
    {
        // Generated part?
        services.AddKeyedSingleton<IConfigureEndpoint>(nameof(BillingFunctions),
            (provider, o) => new BillingFunctions());
        // Ordering might be tricky
        services.AddHostedService<FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>>(sp =>
            new FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>(
                nameof(BillingFunctions.Billing), customize ?? (_ => { }),
                sp.GetRequiredKeyedService<IConfigureEndpoint>(nameof(BillingFunctions))));
        services.AddKeyedSingleton<Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task>>(
            nameof(BillingFunctions.Billing),
            (provider, key) =>
                provider
                    .GetRequiredKeyedService<
                        FunctionEndpointHostedService<BillingFunctions.BillingInitializationContext>>(key).Processor);
        services.AddKeyedSingleton<FunctionEndpoint>(nameof(BillingFunctions.Billing),
            (provider, key) =>
                new FunctionEndpoint(provider
                    .GetRequiredKeyedService<
                        Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task>>(key)));
    }
}