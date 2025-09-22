namespace MultiHost;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class BelongsToAttribute(string functionName) : Attribute
{
    public string FunctionName { get; } = functionName;
}