namespace AssemblyScanningPlayground;
using NServiceBus.Installation;

public class Installer : INeedToInstallSomething
{
    public Task Install(string identity, CancellationToken cancellationToken = new CancellationToken())
    {
        Console.WriteLine("Installer ran");
        return Task.CompletedTask;
    }
}