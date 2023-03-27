namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
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

            PipelineInvoker = receivers.Length > 0
                ? (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId]
                : new PipelineInvoker(new SendOnlyReceiver()); // send-only endpoint

            //PipelineInvoker = (PipelineInvoker)serverlessTransportInfrastructure.Receivers[MainReceiverId];
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

    class SendOnlyReceiver : IMessageReceiver
    {
        static readonly InvalidOperationException SendOnlyEndpointException = new($"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'");

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw SendOnlyEndpointException;

        public Task StartReceive(CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public Task StopReceive(CancellationToken cancellationToken = new CancellationToken()) => throw SendOnlyEndpointException;

        public ISubscriptionManager Subscriptions => throw SendOnlyEndpointException;
        public string Id => throw SendOnlyEndpointException;
        public string ReceiveAddress => throw SendOnlyEndpointException;
    }
}