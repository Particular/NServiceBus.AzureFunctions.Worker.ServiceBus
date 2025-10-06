using System;
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
            var startable = MultiEndpoint.Create(builder.Services, EndpointRegistry.RegisterEndpoints);

            builder.Services.AddSingleton(startable);
            builder.Services.AddHostedService<MultiEndpointHostedService>();
            return builder;
        }
    }
}