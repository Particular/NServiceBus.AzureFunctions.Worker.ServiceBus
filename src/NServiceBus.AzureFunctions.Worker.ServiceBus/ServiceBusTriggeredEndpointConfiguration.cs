namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Serialization;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint.
    /// </summary>
    public class ServiceBusTriggeredEndpointConfiguration
    {
        static ServiceBusTriggeredEndpointConfiguration()
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName)
        {
            this.endpointName = endpointName;
        }

        string endpointName;
        string connectionString;
        Action<EndpointConfiguration> advancedConfiguration;
        Action<RoutingSettings> configureRouting;
        Action<TransportExtensions<AzureServiceBusTransport>> configureTransport;
        ISerializationStrategy serializationStrategy = new SerializationStrategy<NewtonsoftSerializer>();
        Func<string, Task> customDiagnostics = _ => Task.CompletedTask;

        /// <summary>
        /// Configure the ServiceBus connection string used to send messages.
        /// </summary>
        public void ServiceBusConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Apply custom configuration to the NServiceBus Azure Service Bus transport.
        /// </summary>
        public void ConfigureTransport(Action<TransportExtensions<AzureServiceBusTransport>> configureTransport)
        {
            this.configureTransport = configureTransport;
        }

        /// <summary>
        /// Configure message routing.
        /// </summary>
        public void Routing(Action<RoutingSettings> configureRouting)
        {
            this.configureRouting = configureRouting;
        }

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public void UseSerialization<T>(Action<SerializationExtensions<T>> advancedConfiguration = null) where T : SerializationDefinition, new()
        {
            serializationStrategy = new SerializationStrategy<T>(advancedConfiguration);
        }

        /// <summary>
        /// Configure the underlying Endpoint Configuration directly.
        /// </summary>
        public void Advanced(Action<EndpointConfiguration> advancedConfiguration)
        {
            this.advancedConfiguration = advancedConfiguration;
        }

        internal EndpointConfiguration CreateEndpointConfiguration(IConfiguration configuration = null)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);

            endpointConfiguration.Recoverability().CustomPolicy(recoverabilityPolicy.Invoke);

            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));

            // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
            // Can be overriden by ServerlessEndpointConfiguration.LogDiagnostics().
            endpointConfiguration.CustomDiagnosticsWriter(customDiagnostics);

            // 'WEBSITE_SITE_NAME' represents an Azure Function App and the environment variable is set when hosting the function in Azure.
            var functionAppName = GetConfiguredValueOrFallback(configuration, "WEBSITE_SITE_NAME", true) ?? Environment.MachineName;
            endpointConfiguration.UniquelyIdentifyRunningInstance()
                .UsingCustomDisplayName(functionAppName)
                .UsingCustomIdentifier(DeterministicGuid.Create(functionAppName));

            var licenseText = GetConfiguredValueOrFallback(configuration, "NSERVICEBUS_LICENSE", optional: true);
            if (!string.IsNullOrWhiteSpace(licenseText))
            {
                endpointConfiguration.License(licenseText);
            }

            var transport = endpointConfiguration.UseTransport<ServerlessTransport<AzureServiceBusTransport>>();

            connectionString ??= GetConfiguredValueOrFallback(configuration, DefaultServiceBusConnectionName, optional: true);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception($@"Azure Service Bus connection string has not been configured. Specify a connection string through IConfiguration, an environment variable named {DefaultServiceBusConnectionName} or using:
  serviceBusTriggeredEndpointConfiguration.ServiceBusConnectionString(connectionString);");
            }

            transport.ConnectionString(connectionString);

            PipelineInvoker = transport.PipelineAccess();

            var baseTransportConfiguration = transport.BaseTransportConfiguration();
            configureTransport?.Invoke(baseTransportConfiguration);

            var routing = transport.Routing();
            configureRouting?.Invoke(routing);

            serializationStrategy.ApplyTo(endpointConfiguration);

            advancedConfiguration?.Invoke(endpointConfiguration);

            return endpointConfiguration;
        }

        static string GetConfiguredValueOrFallback(IConfiguration configuration, string key, bool optional)
        {
            if (configuration != null)
            {
                var configuredValue = configuration.GetValue<string>(key);
                if (configuredValue != null)
                {
                    return configuredValue;
                }
            }

            var environmentVariable = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(environmentVariable) && !optional)
            {
                throw new Exception($"Configuration or environment value for '{key}' was not set or was empty.");
            }
            return environmentVariable;
        }

        internal PipelineInvoker PipelineInvoker { get; private set; }

        /// <summary>
        /// Disables moving messages to the error queue even if an error queue name is configured.
        /// </summary>
        public void DoNotSendMessagesToErrorQueue()
        {
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = false;
        }

        /// <summary>
        /// Logs endpoint diagnostics information to the log. Diagnostics are logged on level <see cref="LogLevel.Info" />.
        /// </summary>
        public void LogDiagnostics()
        {
            customDiagnostics = diagnostics =>
            {
                LogManager.GetLogger("StartupDiagnostics").Info(diagnostics);
                return Task.CompletedTask;
            };
        }

        interface ISerializationStrategy
        {
            void ApplyTo(EndpointConfiguration endpointConfiguration);
        }

        class SerializationStrategy<T> : ISerializationStrategy where T : SerializationDefinition, new()
        {
            Action<SerializationExtensions<T>> advancedConfiguration;

            public SerializationStrategy(Action<SerializationExtensions<T>> advancedConfiguration = null)
            {
                this.advancedConfiguration = advancedConfiguration;
            }

            public void ApplyTo(EndpointConfiguration endpointConfiguration)
            {
                var settings = endpointConfiguration.UseSerialization<T>();
                advancedConfiguration?.Invoke(settings);
            }
        }

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";
    }
}
