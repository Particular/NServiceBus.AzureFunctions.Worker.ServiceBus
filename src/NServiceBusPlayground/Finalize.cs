namespace AssemblyScanningPlayground;

using NServiceBus.Settings;

public class Finalize : IWantToRunBeforeConfigurationIsFinalized
{
    public void Run(SettingsHolder settings)
    {
        Console.WriteLine("Finalize ran");
    }
}