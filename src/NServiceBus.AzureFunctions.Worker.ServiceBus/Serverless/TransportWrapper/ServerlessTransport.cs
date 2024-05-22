namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Azure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;

    class ServerlessTransport(TransportDefinition baseTransport) : TransportDefinition(
        TransportTransactionMode.ReceiveOnly,
        baseTransport.SupportsDelayedDelivery,
        baseTransport.SupportsPublishSubscribe,
        baseTransport.SupportsTTBR)
    {
        // HINT: This constant is defined in NServiceBus but is not exposed
        const string MainReceiverId = "Main";
        const string SendOnlyConfigKey = "Endpoint.SendOnly";

        public IMessageProcessor MessageProcessor { get; private set; }

        public ServerlessTransport(AzureServiceBusTransport baseTransport, string connectionString) : base(
            TransportTransactionMode.ReceiveOnly,
            baseTransport.SupportsDelayedDelivery,
            baseTransport.SupportsPublishSubscribe,
            baseTransport.SupportsTTBR)
        {
            this.baseTransport = baseTransport;
            this.connectionString = connectionString;
        }

        readonly AzureServiceBusTransport baseTransport;
        readonly string connectionString;

        public IServiceProvider ServiceProvider { get; set; }

        public override async Task<TransportInfrastructure> Initialize(
            HostSettings hostSettings,
            ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            ConfigureTransportConnection(connectionString, ServiceProvider.GetRequiredService<IConfiguration>(), baseTransport,
                ServiceProvider.GetRequiredService<AzureComponentFactory>());

            var baseTransportInfrastructure = await baseTransport.Initialize(
                    hostSettings,
                    receivers,
                    sendingAddresses,
                    cancellationToken)
                .ConfigureAwait(false);

            var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure);

            var isSendOnly = hostSettings.CoreSettings.GetOrDefault<bool>(SendOnlyConfigKey);

            MessageProcessor = isSendOnly
                ? new SendOnlyMessageProcessor()
                : (IMessageProcessor)serverlessTransportInfrastructure.Receivers[MainReceiverId];

            return serverlessTransportInfrastructure;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => supportedTransactionModes;

        static void ConfigureTransportConnection(string connectionString, IConfiguration configuration,
            AzureServiceBusTransport baseTransport, AzureComponentFactory azureComponentFactory)
        {
            // We are deliberately using the old way of configuring a transport here because it allows us configuring
            // the uninitialized transport with a connection string or a fully qualified name and a token provider.
            // Once we deprecate the old way we can for example add make the internal ConnectionString, FQDN or
            // TokenProvider properties visible to functions or the code base has already moved into a different direction.
            var transport = new TransportExtensions<AzureServiceBusTransport>(baseTransport, null);
            if (connectionString != null)
            {
                _ = transport.ConnectionString(connectionString);
            }
            else
            {
                IConfigurationSection connectionSection = configuration.GetSection(DefaultServiceBusConnectionName);
                if (!connectionSection.Exists())
                {
                    throw new Exception($"Azure Service Bus connection string/section has not been configured. Specify a connection string through IConfiguration, an environment variable named {DefaultServiceBusConnectionName} or passing it to `UseNServiceBus(ENDPOINTNAME,CONNECTIONSTRING)`");
                }

                if (!string.IsNullOrWhiteSpace(connectionSection.Value))
                {
                    _ = transport.ConnectionString(connectionSection.Value);
                }
                else
                {
                    string fullyQualifiedNamespace = connectionSection["fullyQualifiedNamespace"];
                    if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
                    {
                        throw new Exception("Connection should have an 'fullyQualifiedNamespace' property or be a string representing a connection string.");
                    }

                    var credential = azureComponentFactory.CreateTokenCredential(connectionSection);
                    _ = transport.CustomTokenCredential(fullyQualifiedNamespace, credential);
                }
            }
        }

        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";

        readonly TransportTransactionMode[] supportedTransactionModes = [TransportTransactionMode.ReceiveOnly];
    }
}