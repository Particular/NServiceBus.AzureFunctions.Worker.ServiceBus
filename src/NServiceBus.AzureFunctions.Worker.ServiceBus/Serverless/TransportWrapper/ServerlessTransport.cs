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

        public ServerlessTransport(AzureServiceBusTransport proxyTransport, string connectionString) : base(
            TransportTransactionMode.ReceiveOnly,
            proxyTransport.SupportsDelayedDelivery,
            proxyTransport.SupportsPublishSubscribe,
            proxyTransport.SupportsTTBR)
        {
            this.proxyTransport = proxyTransport;
            this.connectionString = connectionString;
        }

        readonly AzureServiceBusTransport proxyTransport;
        readonly string connectionString;

        public IServiceProvider ServiceProvider { get; set; }

        public override async Task<TransportInfrastructure> Initialize(
            HostSettings hostSettings,
            ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            var actualTransport = CreateTransport(connectionString, ServiceProvider.GetRequiredService<IConfiguration>(), proxyTransport,
                ServiceProvider.GetRequiredService<AzureComponentFactory>());

            var baseTransportInfrastructure = await actualTransport.Initialize(
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

        internal const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";

        static AzureServiceBusTransport CreateTransport(string connectionString, IConfiguration configuration,
            AzureServiceBusTransport proxyTransport, AzureComponentFactory azureComponentFactory)
        {
            AzureServiceBusTransport transport;
            if (connectionString != null)
            {
                transport = new AzureServiceBusTransport(connectionString);
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
                    transport = new AzureServiceBusTransport(connectionSection.Value);
                }
                else
                {
                    string fullyQualifiedNamespace = connectionSection["fullyQualifiedNamespace"];
                    if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
                    {
                        throw new Exception("Connection should have an 'fullyQualifiedNamespace' property or be a string representing a connection string.");
                    }

                    var credential = azureComponentFactory.CreateTokenCredential(connectionSection);
                    transport = new AzureServiceBusTransport(fullyQualifiedNamespace, credential);
                }
            }

            // TODO map all settings
            if (proxyTransport.WebProxy != null)
            {
                transport.WebProxy = proxyTransport.WebProxy;
            }
            transport.PrefetchCount = proxyTransport.PrefetchCount;
            transport.Topology = proxyTransport.Topology;
            transport.EnablePartitioning = proxyTransport.EnablePartitioning;

            return transport;
        }

        readonly TransportTransactionMode[] supportedTransactionModes =
        {
            TransportTransactionMode.ReceiveOnly
        };
    }
}