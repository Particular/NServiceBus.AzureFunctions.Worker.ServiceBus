using System.Reflection;

public static class EndpointConfigurationExtensions
{
    public static void SetTypesToScan(this EndpointConfiguration busConfiguration, IEnumerable<Type> typesToScan)
    {
        var methodInfo = typeof(EndpointConfiguration).GetMethod("TypesToScanInternal", BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo.Invoke(busConfiguration, [
            typesToScan
        ]);
    }
}