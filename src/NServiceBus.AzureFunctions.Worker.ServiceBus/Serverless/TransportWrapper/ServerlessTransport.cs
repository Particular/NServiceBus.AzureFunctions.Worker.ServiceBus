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
            baseTransport.TransportTransactionMode,
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

        public override string ToTransportAddress(QueueAddress address) => baseTransport.ToTransportAddress(address);

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => supportedTransactionModes;
        readonly TransportTransactionMode[] supportedTransactionModes =
        {
            TransportTransactionMode.ReceiveOnly
        };
    }
}