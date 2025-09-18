namespace AssemblyScanningPlayground;

public class NeedInitialization : INeedInitialization
{
    public void Customize(EndpointConfiguration configuration)
    {
        Console.WriteLine("Customize ran");
    }
}