#pragma warning disable 1591

namespace NServiceBus
{
    using Microsoft.Extensions.Configuration;

    //using Microsoft.Extensions.Configuration;

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
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, IConfiguration configuration = null)
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName, string connectionStringName = null)
        {
        }

        /// <summary>
        /// Creates a serverless NServiceBus endpoint.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        public ServiceBusTriggeredEndpointConfiguration(string endpointName)
        {
        }
    }
}