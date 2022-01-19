#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        [ObsoleteEx(ReplacementTypeOrMember = "UseNServiceBus(ENDPOINTNAME, CONNECTIONSTRING)",
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(IConfiguration configuration)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName)
            => throw new NotImplementedException();
    }

    public static partial class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="FunctionEndpoint"/> via dependency injection.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public static IHostBuilder UseNServiceBus(
            this IHostBuilder hostBuilder,
            string endpointName,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationFactory = null)
            => throw new NotImplementedException();
    }
}