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
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            RegisterEndpointFactory(hostBuilder, null, (_, c) => configurationFactory?.Invoke(c));

            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            RegisterEndpointFactory(hostBuilder, null, configurationFactory);
            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            RegisterEndpointFactory(hostBuilder, endpointName, (_, c) => configurationFactory?.Invoke(c));
            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            RegisterEndpointFactory(hostBuilder, endpointName, configurationFactory);
            return hostBuilder;
        }

        static void RegisterEndpointFactory(
            IHostBuilder hostBuilder,
            string endpointName,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationCustomization)
        {
            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var configuration = hostBuilderContext.Configuration;
                endpointName ??= configuration.GetValue<string>("ENDPOINT_NAME")
                               ?? Assembly.GetCallingAssembly()
                                   .GetCustomAttribute<NServiceBusTriggerFunctionAttribute>()
                                   ?.EndpointName;

                if (string.IsNullOrWhiteSpace(endpointName))
                {
                    throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configuration)` 
- Add a configuration or environment variable with the key ENDPOINT_NAME");
                }

                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, configuration);
                configurationCustomization?.Invoke(configuration, functionEndpointConfiguration);

                var endpointFactory = Configure(functionEndpointConfiguration, serviceCollection);

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