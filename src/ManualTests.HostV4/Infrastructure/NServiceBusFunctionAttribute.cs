using Microsoft.Azure.Functions.Worker;

public class NServiceBusFunctionAttribute(string name) : FunctionAttribute(name);