using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiEndpoint.Services;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Logging;
using NServiceBus.TransactionalSession;

var builder = FunctionsApplication.CreateBuilder(args);

LogManager.Use<DefaultFactory>().Level(LogLevel.Debug);

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

        var endpointConfiguration = new EndpointConfiguration("SenderEndpoint");
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

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, "SenderEndpoint");
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, InitializationService>(s =>
            new InitializationService("SenderEndpoint", keyedServices, s, startableEndpoint, serverlessTransport));
        builder.Services.AddKeyedSingleton<IMessageSession>("SenderEndpoint", (_, __) => startableEndpoint.MessageSession.Value);

        builder.UseMiddleware<TransactionalSessionMiddleware>();
    }
}

public static class ReceiverEndpointConfigurationExtensions
{
    public static void Receiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("ReceiverEndpoint");
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

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, "ReceiverEndpoint");
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, InitializationService>(s =>
            new InitializationService("ReceiverEndpoint", keyedServices, s, startableEndpoint, serverlessTransport));
        builder.Services.AddKeyedSingleton<IMessageSession>("ReceiverEndpoint", (_, __) => startableEndpoint.MessageSession.Value);
        builder.Services.AddKeyedSingleton<IMessageProcessor>("ReceiverEndpoint", (_, __) => new MessageProcessor(serverlessTransport));
    }
}

public static class AnotherReceiverEndpointConfigurationExtensions
{
    public static void AnotherReceiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("AnotherReceiverEndpoint");
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

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, "AnotherReceiverEndpoint");
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        // unfortunately AddHostedServices dedups
        builder.Services.AddSingleton<IHostedService, InitializationService>(s =>
            new InitializationService("AnotherReceiverEndpoint", keyedServices, s, startableEndpoint, serverlessTransport));
        builder.Services.AddKeyedSingleton<IMessageSession>("AnotherReceiverEndpoint", (_, __) => startableEndpoint.MessageSession.Value);
        builder.Services.AddKeyedSingleton<IMessageProcessor>("AnotherReceiverEndpoint", (_, __) => new MessageProcessor(serverlessTransport));
    }
}

class InitializationService(
    string serviceKey,
    KeyedServiceCollectionAdapter services,
    IServiceProvider provider,
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    ServerlessTransport serverlessTransport) : IHostedService
{
    private IEndpointInstance? endpointInstance;
    private KeyedServiceProviderAdapter? keyedServices;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        keyedServices = new KeyedServiceProviderAdapter(provider, serviceKey, services);
        serverlessTransport.ServiceProvider = keyedServices;

        endpointInstance = await startableEndpoint.Start(keyedServices, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (endpointInstance != null && keyedServices != null)
        {
            await endpointInstance.Stop(cancellationToken);
            await keyedServices.DisposeAsync();
        }
    }
}

public interface IMessageProcessor
{
    Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, FunctionContext functionContext, CancellationToken cancellationToken = default);
}

class MessageProcessor(ServerlessTransport transport) : IMessageProcessor
{
    public Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        return transport.MessageProcessor.Process(message, messageActions, cancellationToken);
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