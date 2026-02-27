namespace NServiceBus.AzureFunctions.Worker.Analyzer.Tests;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class SourceGeneratorApprovals
{
    [SetUp]
    public void Setup()
    {
        MetadataReference.CreateFromFile(typeof(FunctionContext).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(ServiceBusMessageActions).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(TriggerBindingAttribute).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(ServiceBusReceivedMessage).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(ServiceBusMessageActions).Assembly.Location);
        MetadataReference.CreateFromFile(typeof(NServiceBusTriggerFunctionAttribute).Assembly.Location);
    }

    [Test]
    public void UsingNamespace()
    {
        var source =
            @"using NServiceBus;

[assembly: NServiceBusTriggerFunction(Foo.Startup.EndpointName)]

namespace Foo
{
    public class Startup
    {
        public const string EndpointName = ""endpoint"";
    }
}";
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void UsingFullyQualifiedAttributeName()
    {
        var source =
            @"[assembly: NServiceBus.NServiceBusTriggerFunction(Foo.Startup.EndpointName)]

namespace Foo
{
    public class Startup
    {
        public const string EndpointName = ""endpoint"";
    }
}";
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void Endpoint_name_using_binding_expression_should_generate_compilation_error_when_no_trigger_function_is_given()
    {
        var source = @"[assembly: NServiceBus.NServiceBusTriggerFunction(""%ENDPOINT_NAME%"")]";

        // Approval shows AzureFunctionsDiagnostics.InvalidBindingExpressionId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .SuppressDiagnosticErrors()
            .Approve(callerMemberName: "BindingExpressionInEndpointName");
    }

    [Test]
    public void Binding_expression_with_trigger_function_should_not_generate_error()
    {
        var source = @"[assembly: NServiceBus.NServiceBusTriggerFunction(""%ENDPOINT_NAME%"", TriggerFunctionName = ""trigger"")]";

        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void NameIsStringValue()
    {
        var source = @"[assembly: NServiceBus.NServiceBusTriggerFunction(""endpoint"")]";

        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void No_attribute_should_not_generate_trigger_function()
    {
        var source = @"";

        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void No_attribute_should_not_generate_compilation_error()
    {
        var source = @"using NServiceBus;";

        // Approval shows no generated output but no diagnostic errors
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [Test]
    public void Can_override_trigger_function_name()
    {
        var source =
            @"using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", TriggerFunctionName = ""trigger"")]

public class Startup
{
}";
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Empty_name_should_cause_an_error(string endpointName)
    {
        var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""" + endpointName + @""")]
";
        // Approval shows no output and AzureFunctionsDiagnostics.InvalidEndpointNameErrorId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .SuppressDiagnosticErrors()
            .Approve();

    }

    [Test]
    public void Invalid_name_should_cause_an_error()
    {
        var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(null)]
";
        // Approval shows no output and AzureFunctionsDiagnostics.InvalidEndpointNameErrorId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .SuppressDiagnosticErrors()
            .Approve();
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Empty_trigger_function_name_should_cause_an_error(string triggerFunctionName)
    {
        var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", TriggerFunctionName = """ + triggerFunctionName + @""")]
";
        // Approval shows no output and AzureFunctionsDiagnostics.InvalidEndpointNameErrorId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .SuppressDiagnosticErrors()
            .Approve();
    }

    [Test]
    public void Invalid_trigger_function_name_should_cause_an_error()
    {
        var source = @"
using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", TriggerFunctionName = null)]
";
        // Approval shows no output and AzureFunctionsDiagnostics.InvalidEndpointNameErrorId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .SuppressDiagnosticErrors()
            .Approve();
    }

    [Test]
    public void Can_supply_connection_name()
    {
        var source =
            @"using NServiceBus;

[assembly: NServiceBusTriggerFunction(""endpoint"", Connection = ""FooBar"")]

public class Startup
{
}";
        // Approval shows no output and AzureFunctionsDiagnostics.InvalidEndpointNameErrorId diagnostic
        SourceGeneratorTest.ForIncrementalGenerator<TriggerFunctionGenerator>()
            .WithSource(source)
            .Approve();
    }
}

public class NServiceBusTriggerFunction
{
}