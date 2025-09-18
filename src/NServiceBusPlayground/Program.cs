using AssemblyScanningPlayground;
using Messages;

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
    typeof(Installer)
]);

var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

await endpointInstance.Publish(new MyEvent1 { SomeProperty = "Hello" }).ConfigureAwait(false);
await endpointInstance.Publish(new MyEvent2()).ConfigureAwait(false);

Console.WriteLine("Press any key to exit");
Console.ReadKey();

await endpointInstance.Stop().ConfigureAwait(false);
