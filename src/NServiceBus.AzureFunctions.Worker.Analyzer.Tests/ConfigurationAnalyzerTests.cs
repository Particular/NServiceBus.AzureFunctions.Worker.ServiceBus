namespace NServiceBus.AzureFunctions.Worker.Analyzer.Tests;

using System.Threading.Tasks;
using NUnit.Framework;
using static AzureFunctionsDiagnostics;

[TestFixture]
public class ConfigurationAnalyzerTests : AnalyzerTestFixture<ConfigurationAnalyzer>
{
    [TestCase("DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)", DefineCriticalErrorActionNotAllowedId)]
    [TestCase("LimitMessageProcessingConcurrencyTo(5)", LimitMessageProcessingToNotAllowedId)]
    [TestCase("MakeInstanceUniquelyAddressable(null)", MakeInstanceUniquelyAddressableNotAllowedId)]
    [TestCase("OverrideLocalAddress(null)", OverrideLocalAddressNotAllowedId)]
    [TestCase("PurgeOnStartup(true)", PurgeOnStartupNotAllowedId)]
    [TestCase("SetDiagnosticsPath(null)", SetDiagnosticsPathNotAllowedId)]
    [TestCase("UseTransport(new AzureServiceBusTransport(null, default(TopicTopology)))", UseTransportNotAllowedId)]
    public Task DiagnosticIsReportedForEndpointConfiguration(string configuration, string diagnosticId)
    {
        var source =
            $@"using NServiceBus;
using System;
using System.Threading.Tasks;
class Foo
{{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.AdvancedConfiguration.{configuration}|];

        var advancedConfig = endpointConfig.AdvancedConfiguration;
        [|advancedConfig.{configuration}|];
    }}
}}";

        return Assert(diagnosticId, source);
    }

    [TestCase("DefineCriticalErrorAction((errorContext, cancellationToken) => Task.CompletedTask)", DefineCriticalErrorActionNotAllowedId)]
    [TestCase("LimitMessageProcessingConcurrencyTo(5)", LimitMessageProcessingToNotAllowedId)]
    [TestCase("MakeInstanceUniquelyAddressable(null)", MakeInstanceUniquelyAddressableNotAllowedId)]
    [TestCase("OverrideLocalAddress(null)", OverrideLocalAddressNotAllowedId)]
    [TestCase("PurgeOnStartup(true)", PurgeOnStartupNotAllowedId)]
    [TestCase("SetDiagnosticsPath(null)", SetDiagnosticsPathNotAllowedId)]
    [TestCase("UseTransport(new AzureServiceBusTransport(null, default(TopicTopology)))", UseTransportNotAllowedId)]
    public Task DiagnosticIsNotReportedForOtherEndpointConfiguration(string configuration, string diagnosticId)
    {
        var source =
            $@"using NServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

class SomeOtherClass
{{
    internal void DefineCriticalErrorAction(Func<ICriticalErrorContext, CancellationToken, Task> onCriticalError) {{ }}
    internal void LimitMessageProcessingConcurrencyTo(int Number) {{ }}
    internal void MakeInstanceUniquelyAddressable(string someProperty) {{ }}
    internal void OverrideLocalAddress(string someProperty) {{ }}
    internal void PurgeOnStartup(bool purge) {{ }}
    internal void SetDiagnosticsPath(string someProperty) {{ }}
    internal void UseTransport(AzureServiceBusTransport transport) {{ }}
}}

class Foo
{{
    void Bar(SomeOtherClass endpointConfig)
    {{
        endpointConfig.{configuration};
    }}
}}";

        return Assert(diagnosticId, source);
    }

    [Test]
    public Task DiagnosticIsReportedForLogDiagnostics()
    {
        var source =
            @"using NServiceBus;
using System;
class Foo
{
    void Bar(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {
        [|endpointConfig.LogDiagnostics()|];
    }
}";

        return Assert(LogDiagnosticsInfoId, source);
    }

    [Test]
    public Task DiagnosticIsNotReportedForLogDiagnosticsOnOtherClass()
    {
        var source =
            @"using NServiceBus;
using System;

class SomeOtherClass
{
    internal void LogDiagnostics() { }
}

class Foo
{
    void Bar(SomeOtherClass otherClass)
    {
        otherClass.LogDiagnostics();
    }
}";

        return Assert(LogDiagnosticsInfoId, source);
    }
}