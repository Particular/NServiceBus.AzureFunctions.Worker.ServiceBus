using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Logging;

public static class FunctionsHostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// TBD
        /// </summary>
        public IHostApplicationBuilder UseNServiceBus(Action<ServerLessOptions> configuration)
        {
            LogManager.UseFactory(FunctionsLoggerFactory.Instance);

            var options = new ServerLessOptions();

            configuration.Invoke(options);

            var startable = MultiEndpoint.Create(builder.Services, mc => options.Apply(mc));

            builder.Services.AddSingleton(startable);
            builder.Services.AddHostedService<MultiEndpointHostedService>();
            return builder;
        }
    }
}