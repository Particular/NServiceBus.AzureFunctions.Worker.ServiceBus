namespace MultiHost;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // It might even be possible to source generate this entire method or we seperate it into a generated method and a core method
    public static void AddNServiceBus(this IServiceCollection services, Action<EndpointConfiguration>? customize = null)
    {
        // Generated part?
        services.AddKeyedSingleton<FunctionEndpoint>(new FunctionEndpoint(nameof(SalesFunction), customize ?? (_ => { })));
    }
}