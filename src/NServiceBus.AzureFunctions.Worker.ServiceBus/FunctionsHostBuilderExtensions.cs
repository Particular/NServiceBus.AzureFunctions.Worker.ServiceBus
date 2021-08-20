namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Provides extension methods to configure a <see cref="FunctionEndpoint"/> using <see cref="IHostBuilder"/>.
    /// </summary>
    public static class FunctionsHostBuilderExtensions
    {
        //TODO
        ///// <summary>
        ///// Use the IConfiguration to configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        ///// </summary>
        //public static void UseNServiceBus(
        //    this IHostBuilder hostBuilder)
        //{
        //    hostBuilder.UseNServiceBus(config => new ServiceBusTriggeredEndpointConfiguration(config));
        //}

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            Func<ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var serviceBusTriggeredEndpointConfiguration = configurationFactory();

            RegisterEndpointFactory(hostBuilder, serviceBusTriggeredEndpointConfiguration);

            return hostBuilder;
        }

        //TODO no access to IConfiguration at this point
        ///// <summary>
        ///// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        ///// </summary>
        //public static void UseNServiceBus(
        //    this IHostBuilder hostBuilder,
        //    Func<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        //{
        //    var serviceBusTriggeredEndpointConfiguration = configurationFactory(configuration);

        //    RegisterEndpointFactory(hostBuilder, serviceBusTriggeredEndpointConfiguration);
        //}

        static void RegisterEndpointFactory(IHostBuilder hostBuilder,
            ServiceBusTriggeredEndpointConfiguration serviceBusTriggeredEndpointConfiguration)
        {
            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var endpointFactory = Configure(serviceBusTriggeredEndpointConfiguration, serviceCollection);

                // for backward compatibility
                serviceCollection.AddSingleton(endpointFactory);
                serviceCollection.AddSingleton<IFunctionEndpoint>(sp => sp.GetRequiredService<FunctionEndpoint>());
            });


        }

        internal static Func<IServiceProvider, FunctionEndpoint> Configure(
            ServiceBusTriggeredEndpointConfiguration configuration,
            IServiceCollection serviceCollection)
        {
            var startableEndpoint = EndpointWithExternallyManagedServiceProvider.Create(
                    configuration.EndpointConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration, serviceProvider);
        }
    }
}