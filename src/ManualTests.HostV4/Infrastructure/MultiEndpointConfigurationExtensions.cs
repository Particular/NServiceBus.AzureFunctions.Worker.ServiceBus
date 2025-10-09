using System;
using NServiceBus;

public static class MultiEndpointConfigurationExtensions
{
    extension(MultiEndpointConfiguration multiEndpointConfiguration)
    {
        /// <summary>
        /// TBD
        /// </summary>
        public MultiEndpointConfiguration AddNServiceBus2(Action<EndpointConfiguration> commonConfiguration = null)
        {
            return multiEndpointConfiguration;
        }
    }
}