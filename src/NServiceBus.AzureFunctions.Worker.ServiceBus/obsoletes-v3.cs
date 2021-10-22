﻿#pragma warning disable 1591

namespace NServiceBus
{
    public partial class ServiceBusTriggeredEndpointConfiguration
    {
        [ObsoleteEx(ReplacementTypeOrMember = "UseNServiceBus(ENDPOINTNAME, CONNECTIONSTRING)",
                    TreatAsErrorFromVersion = "3",
                    RemoveInVersion = "4")]
        public string ServiceBusConnectionString { get; set; }
    }
}