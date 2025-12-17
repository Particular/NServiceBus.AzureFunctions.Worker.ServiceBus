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
    public string ServiceBusConnectionName { get; set; } = DefaultServiceBusConnectionName;
    public string? ConnectionString { get; set; } = null;

    internal IMessageProcessor MessageProcessor { get; private set; }
    internal IServiceProvider ServiceProvider { get; set; }

    public override async Task<TransportInfrastructure> Initialize(
        HostSettings hostSettings,
        ReceiveSettings[] receivers,
        string[] sendingAddresses,
        CancellationToken cancellationToken = default)
    {
        var configuredTransport = BuildUnderlyingTransportDefinition();

        var baseTransportInfrastructure = await configuredTransport.Initialize(
                hostSettings,
                receivers,
                sendingAddresses,
                cancellationToken)
            .ConfigureAwait(false);

        var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure);

        var isSendOnly = hostSettings.CoreSettings != null && hostSettings.CoreSettings.GetOrDefault<bool>(SendOnlyConfigKey);

        MessageProcessor = isSendOnly
            ? new SendOnlyMessageProcessor()
            : (IMessageProcessor)serverlessTransportInfrastructure.Receivers[MainReceiverId];

        return serverlessTransportInfrastructure;
    }

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => [TransportTransactionMode.ReceiveOnly];

    AzureServiceBusTransport BuildUnderlyingTransportDefinition()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new AzureServiceBusTransport(ConnectionString, TopicTopology.Default);
        }

        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        // Look for a section OR an actual value 
        // https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cqueue%2Cextensionv5&pivots=programming-language-csharp#connections
        IConfigurationSection connectionSection = configuration.GetSection(ServiceBusConnectionName);
        if (!connectionSection.Exists())
        {
            throw new Exception($"Azure Service Bus connection string/section has not been configured. Specify a connection string through IConfiguration, an environment variable named {ServiceBusConnectionName} or passing it to `new AzureServiceBusTransport(ConnectionString)`.`");
        }

        if (!string.IsNullOrWhiteSpace(connectionSection.Value))
        {
            return new AzureServiceBusTransport(connectionSection.Value, TopicTopology.Default);
        }

        string? fullyQualifiedNamespace = connectionSection["fullyQualifiedNamespace"];
        if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            throw new Exception("Connection should have an 'fullyQualifiedNamespace' property or be a string representing a connection string.");
        }

        var azureComponentFactory = ServiceProvider.GetRequiredService<AzureComponentFactory>();

        var credential = azureComponentFactory.CreateTokenCredential(connectionSection);

        return new AzureServiceBusTransport(fullyQualifiedNamespace, credential, TopicTopology.Default);
    }

    const string DefaultServiceBusConnectionName = "AzureWebJobsServiceBus";

    // HINT: This constant is defined in NServiceBus but is not exposed
    const string MainReceiverId = "Main";
    const string SendOnlyConfigKey = "Endpoint.SendOnly";
}