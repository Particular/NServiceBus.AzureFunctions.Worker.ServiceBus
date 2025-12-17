namespace NServiceBus.AzureFunctions.Worker.ServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport;

class AzureServiceBusServerlessTransport() : TransportDefinition(
    TransportTransactionMode.ReceiveOnly,
    true,
    true,
    true)
{
    internal IMessageProcessor MessageProcessor { get; private set; }

    internal IServiceProvider ServiceProvider { get; set; }

    public override async Task<TransportInfrastructure> Initialize(
        HostSettings hostSettings,
        ReceiveSettings[] receivers,
        string[] sendingAddresses,
        CancellationToken cancellationToken = default)
    {
        var configuredTransport = ConfigureTransportConnection(null,
            DefaultServiceBusConnectionName,
            ServiceProvider.GetRequiredService<IConfiguration>(),
            ServiceProvider.GetRequiredService<AzureComponentFactory>());

        var baseTransportInfrastructure = await configuredTransport.Initialize(
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

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => [TransportTransactionMode.ReceiveOnly];

    static AzureServiceBusTransport ConfigureTransportConnection(string connectionString, string connectionName, IConfiguration configuration, AzureComponentFactory azureComponentFactory)
    {
        if (connectionString != null)
        {
            return new AzureServiceBusTransport(connectionString, TopicTopology.Default);
        }

        var serviceBusConnectionName = string.IsNullOrWhiteSpace(connectionName) ? DefaultServiceBusConnectionName : connectionName;
        IConfigurationSection connectionSection = configuration.GetSection(serviceBusConnectionName);
        if (!connectionSection.Exists())
        {
            throw new Exception($"Azure Service Bus connection string/section has not been configured. Specify a connection string through IConfiguration, an environment variable named {serviceBusConnectionName} or passing it to `UseNServiceBus(ENDPOINTNAME,CONNECTIONSTRING)`");
        }

        if (!string.IsNullOrWhiteSpace(connectionSection.Value))
        {
            return new AzureServiceBusTransport(connectionSection.Value, TopicTopology.Default);
        }

        string fullyQualifiedNamespace = connectionSection["fullyQualifiedNamespace"];
        if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            throw new Exception("Connection should have an 'fullyQualifiedNamespace' property or be a string representing a connection string.");
        }

        var credential = azureComponentFactory.CreateTokenCredential(connectionSection);

        return new AzureServiceBusTransport(fullyQualifiedNamespace, credential, TopicTopology.Default);
    }

    const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";
    // HINT: This constant is defined in NServiceBus but is not exposed
    const string MainReceiverId = "Main";
    const string SendOnlyConfigKey = "Endpoint.SendOnly";
}