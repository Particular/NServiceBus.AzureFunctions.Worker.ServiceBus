using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;

public static class FunctionsHostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Configures an NServiceBus endpoint that can be injected into a function trigger as a <see cref="IFunctionEndpoint"/> via dependency injection.
        /// </summary>
        public IHostApplicationBuilder AddNServiceBus2(Action<EndpointConfiguration> commonConfiguration = null)
        {
            IConfigureEndpoint salesFunctions = new SalesFunctions();
            var startable = MultiEndpoint.Create(builder.Services, mc =>
                {
                    var sales= mc.AddEndpoint("sales");

                    salesFunctions.Configure(sales);
                }
            );

            builder.Services.AddSingleton(salesFunctions);
            builder.Services.AddSingleton(startable);
            builder.Services.AddHostedService<MultiEndpointHostedService>();
            return builder;
        }
    }
}

public class SalesFunctions : IConfigureEndpoint
{
    public void Configure(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());
    }
}

public interface IConfigureEndpoint
{
    void Configure(EndpointConfiguration endpointConfiguration);
}