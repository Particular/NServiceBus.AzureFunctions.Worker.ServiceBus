﻿using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Installation;

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
    public static partial class FunctionsHostBuilderExtensions
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
            Guard.AgainstNullAndEmpty(nameof(connectionString), connectionString);

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
                var endpointNameValue = callingAssembly
                    ?.GetCustomAttribute<NServiceBusTriggerFunctionAttribute>()
                    ?.EndpointName;

                endpointName ??= configuration.GetValue<string>("ENDPOINT_NAME")
                                 ?? TryResolveBindingExpression()
                                 ?? endpointNameValue;

                if (string.IsNullOrWhiteSpace(endpointName))
                {
                    throw new Exception($@"Endpoint name cannot be determined automatically. Use one of the following options to specify endpoint name: 
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(endpointName)` to generate a trigger
- Use `{nameof(NServiceBusTriggerFunctionAttribute)}(%ENDPOINT_NAME%, TriggerFunctionName = triggerName)` to use a setting or environment variable
- Use `functionsHostBuilder.UseNServiceBus(endpointName, configuration)`");
                }

                var installerConfig = new ServiceBusTriggeredEndpointConfiguration(endpointName, configuration, connectionString);
                configurationCustomization?.Invoke(configuration, installerConfig);
                // necessary because this happens in `CreateEndpointFactory` to prevent users overriding it. Might not be necessary when the API isn't accessible to users anymore.
                installerConfig.AdvancedConfiguration.UseTransport(new ServerlessTransport(installerConfig.Transport));
                var sc2 = new ServiceCollection(); // TODO!
                var installer = Installer.CreateInstallerWithExternallyManagedContainer(installerConfig.AdvancedConfiguration,
                    sc2);
                serviceCollection.AddHostedService(sp => new InstallerHost(installer, sc2.BuildServiceProvider()));

                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(endpointName, configuration, connectionString);

                configurationCustomization?.Invoke(configuration, functionEndpointConfiguration);

                var endpointFactory = functionEndpointConfiguration.CreateEndpointFactory(serviceCollection);

                // for backward compatibility
                serviceCollection.AddSingleton(endpointFactory);
                serviceCollection.AddSingleton<IFunctionEndpoint>(sp => sp.GetRequiredService<FunctionEndpoint>());

                string TryResolveBindingExpression()
                {
                    if (endpointNameValue != null && endpointNameValue[0] == '%' && endpointNameValue[0] == endpointNameValue[^1])
                    {
                        return configuration.GetValue<string>(endpointNameValue.Trim('%'));
                    }

                    return null;
                }
            });


        }
    }
}