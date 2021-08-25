namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class ServiceBusTriggeredEndpointConfigurationTests
    {
        [Test]
        public void ConfigurationCallbackMethodOrder()
        {
            var config = new ServiceBusTriggeredEndpointConfiguration("MyEndpoint");
            var callOrder = new List<string>();

            config.Advanced(cfg => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.Advanced)));
            config.Routing(cfg => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.Routing)));
            config.ConfigureTransport(cfg => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.ConfigureTransport)));
            config.UseSerialization<NewtonsoftSerializer>(cfg => callOrder.Add(nameof(ServiceBusTriggeredEndpointConfiguration.UseSerialization)));

            config.CreateEndpointConfiguration();

            Approver.Verify(string.Join(Environment.NewLine, callOrder));
        }
    }
}
