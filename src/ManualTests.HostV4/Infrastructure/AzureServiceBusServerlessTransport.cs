using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class AzureServiceBusServerlessTransport : TransportDefinition
{
    public AzureServiceBusServerlessTransport(TopicTopology topology) : base(TransportTransactionMode.ReceiveOnly, true, true, true)
    {
    }

    public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = new CancellationToken()) => throw new System.NotImplementedException();

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => throw new System.NotImplementedException();
}