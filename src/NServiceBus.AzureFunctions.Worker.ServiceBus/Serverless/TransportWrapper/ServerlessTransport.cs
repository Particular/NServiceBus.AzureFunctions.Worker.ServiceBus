namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class ServerlessTransport : TransportDefinition
    {
        // HINT: This constant is defined in NServiceBus but is not exposed
        const string MainReceiverId = "Main";

        public PipelineInvoker PipelineInvoker { get; private set; }

        public ServerlessTransport(TransportDefinition baseTransport) : base(
            TransportTransactionMode.ReceiveOnly,
            baseTransport.SupportsDelayedDelivery,
            baseTransport.SupportsPublishSubscribe,
            baseTransport.SupportsTTBR)
        {
            this.baseTransport = baseTransport;
        }

        readonly TransportDefinition baseTransport;

        public override async Task<TransportInfrastructure> Initialize(
            HostSettings hostSettings,
            ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var baseTransportInfrastructure = await baseTransport.Initialize(
                    hostSettings,
                    receivers,
                    sendingAddresses,
                    cancellationToken)
                .ConfigureAwait(false);

            var serverlessTransportInfrastructure = new ServerlessTransportInfrastructure(baseTransportInfrastructure);
            PipelineInvoker = (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId];
            return serverlessTransportInfrastructure;
        }

#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => supportedTransactionModes;
        readonly TransportTransactionMode[] supportedTransactionModes =
        {
            TransportTransactionMode.ReceiveOnly
        };
    }
}