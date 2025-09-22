namespace MultiHost;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)] // can something belong to multiple functions?
public sealed class BelongsToAttribute(string functionName) : Attribute
{
    public string FunctionName { get; } = functionName;
}