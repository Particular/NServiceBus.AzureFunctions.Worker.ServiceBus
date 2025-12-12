using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiEndpoint;
using MultiEndpoint.Logging;
using MultiEndpoint.Services;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Logging;
using NServiceBus.TransactionalSession;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

var builder = FunctionsApplication.CreateBuilder(args);

// as early as possible
LogManager.UseFactory(FunctionsLoggerFactory.Instance);

builder.Logging.AddSystemdConsole(options => options.IncludeScopes = true);

builder.Services.AddHostedService<InitializeLogger>();

builder.Sender();
builder.Receiver();
builder.AnotherReceiver();

var host = builder.Build();

await host.RunAsync();

public static class SenderEndpointConfigurationExtensions
{
    public static void Sender(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var serviceKey = "SenderEndpoint";
        var endpointConfiguration = new EndpointConfiguration(serviceKey);
        endpointConfiguration.SendOnly();
        endpointConfiguration.EnableOutbox();

        var assemblyScanner = endpointConfiguration.AssemblyScanner();
        assemblyScanner.Disable = true;

        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.DatabaseName("SharedBetweenSenderAndReceiver");
        persistence.EnableTransactionalSession(new TransactionalSessionOptions
        {
            ProcessorEndpoint = "ReceiverEndpoint"
        });
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default)
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };
        var serverlessTransport = new ServerlessTransport(transport, null, "AzureWebJobsServiceBus");
        endpointConfiguration.UseTransport(serverlessTransport);

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, serviceKey);
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        builder.Services.AddKeyedSingleton<EndpointStarter>(serviceKey, (sp, __) => new EndpointStarter(startableEndpoint, sp, serverlessTransport, serviceKey, keyedServices));
        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, NServiceBusHostedService>(s => new NServiceBusHostedService(s.GetRequiredKeyedService<EndpointStarter>(serviceKey)));
        builder.Services.AddKeyedSingleton<IMessageSession>(serviceKey, (sp, key) => new HostAwareMessageSession(sp.GetRequiredKeyedService<EndpointStarter>(key)));

        builder.UseMiddleware<TransactionalSessionMiddleware>();
    }
}

public static class ReceiverEndpointConfigurationExtensions
{
    public static void Receiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var serviceKey = "ReceiverEndpoint";

        var endpointConfiguration = new EndpointConfiguration(serviceKey);
        endpointConfiguration.EnableOutbox();

        endpointConfiguration.EnableInstallers();

        var assemblyScanner = endpointConfiguration.AssemblyScanner();
        assemblyScanner.Disable = true;

        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.DatabaseName("SharedBetweenSenderAndReceiver");
        persistence.EnableTransactionalSession();

        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        // hardcoded handlers
        endpointConfiguration.AddHandler<TriggerMessageHandler>();
        endpointConfiguration.AddHandler<SomeOtherMessageHandler>();
        endpointConfiguration.AddHandler<SomeEventMessageHandler>();

        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default)
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };
        var serverlessTransport = new ServerlessTransport(transport, null, "AzureWebJobsServiceBus");
        endpointConfiguration.UseTransport(serverlessTransport);

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, serviceKey);
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        builder.Services.AddKeyedSingleton<EndpointStarter>(serviceKey, (sp, __) => new EndpointStarter(startableEndpoint, sp, serverlessTransport, serviceKey, keyedServices));
        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, NServiceBusHostedService>(sp => new NServiceBusHostedService(sp.GetRequiredKeyedService<EndpointStarter>(serviceKey)));
        builder.Services.AddKeyedSingleton<IMessageSession>(serviceKey, (sp, key) => new HostAwareMessageSession(sp.GetRequiredKeyedService<EndpointStarter>(key)));
        builder.Services.AddKeyedSingleton<IMessageProcessor>(serviceKey, (sp, key) => new MessageProcessor(serverlessTransport, sp.GetRequiredKeyedService<EndpointStarter>(key)));
    }
}

public static class AnotherReceiverEndpointConfigurationExtensions
{
    public static void AnotherReceiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var serviceKey = "AnotherReceiverEndpoint";
        var endpointConfiguration = new EndpointConfiguration(serviceKey);
        endpointConfiguration.EnableOutbox();

        endpointConfiguration.EnableInstallers();

        var assemblyScanner = endpointConfiguration.AssemblyScanner();
        assemblyScanner.Disable = true;

        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession();

        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        // hardcoded handlers
        endpointConfiguration.AddHandler<SomeEventMessageHandler>();

        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default)
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };
        var serverlessTransport = new ServerlessTransport(transport, null, "AzureWebJobsServiceBus");
        endpointConfiguration.UseTransport(serverlessTransport);

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, serviceKey);
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        builder.Services.AddKeyedSingleton<EndpointStarter>(serviceKey, (sp, __) => new EndpointStarter(startableEndpoint, sp, serverlessTransport, serviceKey, keyedServices));
        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, NServiceBusHostedService>(sp => new NServiceBusHostedService(sp.GetRequiredKeyedService<EndpointStarter>(serviceKey)));
        builder.Services.AddKeyedSingleton<IMessageSession>(serviceKey, (sp, key) => new HostAwareMessageSession(sp.GetRequiredKeyedService<EndpointStarter>(key)));
        builder.Services.AddKeyedSingleton<IMessageProcessor>(serviceKey, (sp, key) => new MessageProcessor(serverlessTransport, sp.GetRequiredKeyedService<EndpointStarter>(key)));
    }
}

public class TransactionalSessionMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // brutally hardcoded for now
        if (context.FunctionDefinition.Name != "HttpSenderV4Transactional")
        {
            await next(context);
            return;
        }

        // this is a little weird, but it works
        await using var transactionalSession = context.InstanceServices.GetRequiredKeyedService<ITransactionalSession>("SenderEndpoint");
        await transactionalSession.Open(new MongoOpenSessionOptions(), context.CancellationToken);

        await next(context);

        await transactionalSession.Commit(context.CancellationToken);
    }
}

public class InitializeLogger(ILoggerFactory loggerFactory) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        FunctionsLoggerFactory.Instance.SetLoggerFactory(loggerFactory);
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}