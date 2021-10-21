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
            var callingAssembly = Assembly.GetCallingAssembly();
            RegisterEndpointFactory(hostBuilder, null, callingAssembly, (_, c) => configurationFactory?.Invoke(c));

            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            RegisterEndpointFactory(hostBuilder, null, callingAssembly, configurationFactory);
            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            string connectionString = default,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            RegisterEndpointFactory(hostBuilder, endpointName, null, (_, c) => configurationFactory?.Invoke(c), connectionString);
            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            RegisterEndpointFactory(hostBuilder, endpointName, null, configurationFactory);
            return hostBuilder;
        }

        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            string connectionString,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationFactory)
        {
            Guard.AgainstNullAndEmpty(nameof(endpointName), endpointName);

            RegisterEndpointFactory(hostBuilder, endpointName, null, configurationFactory, connectionString);
            return hostBuilder;
        }

        static void RegisterEndpointFactory(
            IHostBuilder hostBuilder,
            string endpointName,
            Assembly callingAssembly,
            Action<IConfiguration, ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
            string connectionString = default)
        {
            hostBuilder.ConfigureServices((hostBuilderContext, serviceCollection) =>
            {
                var configuration = hostBuilderContext.Configuration;
                endpointName ??= configuration.GetValue<string>("ENDPOINT_NAME")
                               ?? callingAssembly
                                   ?.GetCustomAttribute<NServiceBusTriggerFunctionAttribute>()
                                   ?.EndpointName;

                if (string.IsNullOrWhiteSpace(endpointName))
                {
                    throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configuration)` 
- Add a configuration or environment variable with the key ENDPOINT_NAME");
                }

                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, configuration, connectionString);
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
            var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
                    configuration.AdvancedConfiguration,
                    serviceCollection);

            return serviceProvider => new FunctionEndpoint(startableEndpoint, configuration, serviceProvider);
        }
    }
}