namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using AzureFunctions.Worker.ServiceBus;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Serialization;

    /// <summary>
    /// Represents a serverless NServiceBus endpoint.
    /// </summary>
    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        static ServiceBusTriggeredEndpointConfiguration()
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);
        }

        /// <summary>
        /// The Azure Service Bus transport configuration.
        /// </summary>
        public AzureServiceBusTransport Transport { get; }

        /// <summary>
        /// The routing configuration.
        /// </summary>
        public RoutingSettings<AzureServiceBusTransport> Routing { get; }

        /// <summary>
        /// Gives access to the underlying endpoint configuration for advanced configuration options.
        /// </summary>
        public EndpointConfiguration AdvancedConfiguration { get; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        internal ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null, string connectionString = default)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointName);

            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(5));
            recoverability.Delayed(settings => settings.NumberOfRetries(3));
            recoverabilityPolicy.SendFailedMessagesToErrorQueue = true;
            endpointConfiguration.Recoverability().CustomPolicy(recoverabilityPolicy.Invoke);

            // Disable diagnostics by default as it will fail to create the diagnostics file in the default path.
            // Can be overriden by ServerlessEndpointConfiguration.LogDiagnostics().
            endpointConfiguration.CustomDiagnosticsWriter((_, __) => Task.CompletedTask);

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

            if (connectionString == null)
            {
                connectionString = GetConfiguredValueOrFallback(configuration, DefaultServiceBusConnectionName, optional: true);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new Exception($@"Azure Service Bus connection string has not been configured. Specify a connection string through IConfiguration, an environment variable named {DefaultServiceBusConnectionName} or passing it to `UseNServiceBus(ENDPOINTNAME,CONNECTIONSTRING)`");
                }
            }

            Transport = new AzureServiceBusTransport(connectionString);
            Routing = endpointConfiguration.UseTransport(Transport);

            endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

            AdvancedConfiguration = endpointConfiguration;
        }

        internal ServerlessInterceptor MakeServerless()
        {
            var serverlessTransport = new ServerlessTransport(Transport);
            AdvancedConfiguration.UseTransport(serverlessTransport);
            return new ServerlessInterceptor(serverlessTransport);
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

        /// <summary>
        /// Define the serializer to be used.
        /// </summary>
        public SerializationExtensions<T> UseSerialization<T>() where T : SerializationDefinition, new()
        {
            return AdvancedConfiguration.UseSerialization<T>();
        }

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
            AdvancedConfiguration.CustomDiagnosticsWriter((diagnostics, _) =>
            {
                LogManager.GetLogger("StartupDiagnostics").Info(diagnostics);
                return Task.CompletedTask;
            });
        }

        readonly ServerlessRecoverabilityPolicy recoverabilityPolicy = new ServerlessRecoverabilityPolicy();
        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";
    }
}
