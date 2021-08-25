namespace NServiceBus.AzureFunctions.InProcess.ServiceBus
{
    using Settings;
    using Transport;

    class ServerlessTransport<TBaseTransport> : TransportDefinition
        where TBaseTransport : TransportDefinition, new()
    {
        public ServerlessTransport()
        {
            baseTransport = new TBaseTransport();
        }

        public override string ExampleConnectionStringForErrorMessage { get; } = string.Empty;

        // HINT: Prevent core from throwing a generic exception
        public override bool RequiresConnectionString => false;

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            var baseTransportInfrastructure = baseTransport.Initialize(settings, connectionString);
            return new ServerlessTransportInfrastructure(baseTransportInfrastructure, settings);
        }

        readonly TBaseTransport baseTransport;
    }
}