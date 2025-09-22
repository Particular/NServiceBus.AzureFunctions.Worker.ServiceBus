namespace MultiHost;

[AttributeUsage(AttributeTargets.Class)]
public class NServiceBusEndpointFunctionAttribute : Attribute
{
    public string EndpointName { get; }
    public string QueueName { get; set; }

    public NServiceBusEndpointFunctionAttribute(string endpointName)
    {
        EndpointName = endpointName;
        QueueName = endpointName;
    }
}