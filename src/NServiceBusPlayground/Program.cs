using AssemblyScanningPlayground;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NServiceBus.Features;

var rootCollection = new ServiceCollection();
rootCollection.AddSingleton<ISingletonOnRoot, SingletonOnRoot>();
rootCollection.AddScoped<IScopedOnRoot, ScopedOnRoot>();
rootCollection.AddTransient<ITransientOnRoot, TransientOnRoot>();
rootCollection.AddSingleton(new FactoryOnRoot());
rootCollection.AddSingleton<IFactoryOnRoot>(p => p.GetRequiredService<FactoryOnRoot>());

var childCollection = new ServiceCollection();
childCollection.AddSingleton<ISingletonOnChild, SingletonOnChild>();

var endpointConfiguration = new EndpointConfiguration("AssemblyScanningPlayground");
endpointConfiguration.UsePersistence<LearningPersistence>();
endpointConfiguration.UseTransport<LearningTransport>();
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.EnableInstallers();

endpointConfiguration.SetTypesToScan([
    typeof(MyEvent1),
    typeof(HandlerPickedUp),
    typeof(MySaga),
    typeof(NeedInitialization),
    typeof(Finalize),
    typeof(Installer),
    typeof(TransactionSessionFeature)
]);

var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, childCollection);
ServiceProvider childProvider = null;
foreach (var service in childCollection)
{
    if (service is { ServiceType: { IsPublic: true, IsGenericType: false }, Lifetime: ServiceLifetime.Scoped or ServiceLifetime.Singleton })
    {
        rootCollection.Add(new ServiceDescriptor(service.ServiceType, "MyKey", (p, k) =>
        {
            return childProvider!.GetRequiredService(service.ServiceType);
        }, service.Lifetime));
    }
}
childProvider = childCollection.BuildServiceProvider();
var endpointInstance = await startableEndpoint.Start(childProvider).ConfigureAwait(false);

var rootServiceProvider = rootCollection.BuildServiceProvider();
var scope1 = rootServiceProvider.CreateAsyncScope();
var session1 = scope1.ServiceProvider.GetKeyedService<ITransactionalSession>("MyKey");
await scope1.DisposeAsync().ConfigureAwait(false);

var scope2 = rootServiceProvider.CreateAsyncScope();
var session2 = scope2.ServiceProvider.GetKeyedService<ITransactionalSession>("MyKey");
await scope2.DisposeAsync().ConfigureAwait(false);

await endpointInstance.Publish(new MyEvent1 { SomeProperty = "Hello" }).ConfigureAwait(false);
await endpointInstance.Publish(new MyEvent2()).ConfigureAwait(false);

Console.WriteLine("Press any key to exit");
Console.ReadKey();

await endpointInstance.Stop().ConfigureAwait(false);

class SingletonOnRoot : ISingletonOnRoot;
interface ISingletonOnRoot;
class SingletonOnChild : ISingletonOnChild;
interface ISingletonOnChild;

class ScopedOnRoot : IScopedOnRoot;
interface IScopedOnRoot;

class TransientOnRoot : ITransientOnRoot;

interface ITransientOnRoot;

class FactoryOnRoot : IFactoryOnRoot;

interface IFactoryOnRoot;

public interface ITransactionalSession : IAsyncDisposable;

class TransactionalSessionState
{
    public TransactionalSessionState() => State = state++;

    public int State { get; set; }

    static int state;
}

sealed class TransactionalSession : ITransactionalSession
{
    public TransactionalSession(TransactionalSessionState state)
    {
        Console.WriteLine("TransactionalSession created" + state.State);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

class TransactionSessionFeature : Feature
{
    public TransactionSessionFeature() => EnableByDefault();

    protected override void Setup(FeatureConfigurationContext context)
    {
        var state = new TransactionalSessionState();
        context.Services.AddSingleton(state);
        context.Services.AddScoped<ITransactionalSession>(p =>
        {
            var state = p.GetRequiredService<TransactionalSessionState>();
            return new TransactionalSession(state);
        });
    }
}