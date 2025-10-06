using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;

public static class FunctionsHostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// TBD
        /// </summary>
        public IHostApplicationBuilder AddNServiceBus2(Action<EndpointConfiguration> commonConfiguration = null)
        {
            var startable = MultiEndpoint.Create(builder.Services, mc => EndpointRegistry.RegisterEndpoints(mc, commonConfiguration));

            builder.Services.AddSingleton(startable);
            builder.Services.AddHostedService<MultiEndpointHostedService>();
            return builder;
        }
    }
}