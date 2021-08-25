namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Provides extension methods to configure a <see cref="FunctionEndpoint"/> using <see cref="IHostBuilder"/>.
    /// </summary>
    public static class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            Action<ServiceBusTriggeredEndpointConfiguration> configuration = null)
        {
            var endpointName = Assembly.GetCallingAssembly()
                                   .GetCustomAttribute<NServiceBusTriggerFunctionAttribute>()
                                   ?.EndpointName;

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configurationFactory)`");
            }
            return hostBuilder.UseNServiceBus(endpointName, configuration);
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            Action<ServiceBusTriggeredEndpointConfiguration> configuration = null)
        {
            var serviceBusTriggeredEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName);
            configuration?.Invoke(serviceBusTriggeredEndpointConfiguration);

            RegisterEndpointFactory(hostBuilder, serviceBusTriggeredEndpointConfiguration);
            return hostBuilder;
        }

        static void RegisterEndpointFactory(IHostBuilder hostBuilder,
            ServiceBusTriggeredEndpointConfiguration serviceBusTriggeredEndpointConfiguration)
        {
            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var hostConfiguration = hostBuilderContext.Configuration;
                var endpointFactory = Configure(serviceBusTriggeredEndpointConfiguration, hostConfiguration, serviceCollection);

                // for backward compatibility
                serviceCollection.AddSingleton(endpointFactory);
                serviceCollection.AddSingleton<IFunctionEndpoint>(sp => sp.GetRequiredService<FunctionEndpoint>());
            });
        }

        internal static Func<IServiceProvider, FunctionEndpoint> Configure(
            ServiceBusTriggeredEndpointConfiguration serviceBusTriggeredEndpointConfiguration,
            IConfiguration hostConfiguration,
            IServiceCollection serviceCollection)
        {
            var endpointConfiguration =
                serviceBusTriggeredEndpointConfiguration.CreateEndpointConfiguration(hostConfiguration);

            var startableEndpoint = EndpointWithExternallyManagedServiceProvider.Create(
                    endpointConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, serviceBusTriggeredEndpointConfiguration, serviceProvider);
        }
    }
}