namespace NServiceBus.AzureFunctions.Worker.Analyzer.Tests;

using System.Threading.Tasks;
using NUnit.Framework;
using Particular.AnalyzerTesting;
using static AzureFunctionsDiagnostics;

[TestFixture]
public class TransportConfigurationAnalyzerTests : AnalyzerTestFixture<ConfigurationAnalyzer>
{
    [TestCase("TransportTransactionMode", "TransportTransactionMode.None", TransportTransactionModeNotAllowedId)]
    [TestCase("MaxAutoLockRenewalDuration", "new System.TimeSpan(0, 0, 5, 0)", MaxAutoLockRenewalDurationNotAllowedId)]
    [TestCase("PrefetchCount", "5", PrefetchCountNotAllowedId)]
    [TestCase("PrefetchMultiplier", "5", PrefetchMultiplierNotAllowedId)]
    [TestCase("TimeToWaitBeforeTriggeringCircuitBreaker", "new System.TimeSpan(0, 0, 5, 0)", TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId)]
    public Task DiagnosticIsReportedTransportConfigurationDirect(string configName, string configValue, string diagnosticId)
    {
        var source =
            $@"using NServiceBus;
using System;
using System.Threading.Tasks;
class Foo
{{
    void Direct(ServiceBusTriggeredEndpointConfiguration endpointConfig)
    {{
        [|endpointConfig.Transport.{configName}|] = {configValue};

        var transportConfig = endpointConfig.Transport;
        [|transportConfig.{configName}|] = {configValue};
    }}
}}";

        return Assert(source, diagnosticId);
    }

    [TestCase("Transactions", "TransportTransactionMode.None", TransportTransactionModeNotAllowedId)]
    [TestCase("MaxAutoLockRenewalDuration", "new System.TimeSpan(0, 0, 5, 0)", MaxAutoLockRenewalDurationNotAllowedId)]
    [TestCase("PrefetchCount", "5", PrefetchCountNotAllowedId)]
    [TestCase("PrefetchMultiplier", "5", PrefetchMultiplierNotAllowedId)]
    [TestCase("TimeToWaitBeforeTriggeringCircuitBreaker", "new System.TimeSpan(0, 0, 5, 0)", TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId)]
    public Task DiagnosticIsReportedTransportConfigurationExtension(string configName, string configValue, string diagnosticId)
    {
        var source =
            $@"using NServiceBus;
using System;
using System.Threading.Tasks;
class Foo
{{
    void Extension(TransportExtensions<AzureServiceBusTransport> transportExtension)
    {{
        [|transportExtension.{configName}({configValue})|];
    }}
}}";

        return Assert(source, diagnosticId);
    }

    [TestCase("MaxAutoLockRenewalDuration", "new System.TimeSpan(0, 0, 5, 0)", MaxAutoLockRenewalDurationNotAllowedId)]
    [TestCase("PrefetchCount", "5", PrefetchCountNotAllowedId)]
    [TestCase("PrefetchMultiplier", "5", PrefetchMultiplierNotAllowedId)]
    [TestCase("TimeToWaitBeforeTriggeringCircuitBreaker", "new System.TimeSpan(0, 0, 5, 0)", TimeToWaitBeforeTriggeringCircuitBreakerNotAllowedId)]
    public Task DiagnosticIsNotReportedForNonTransportConfiguration(string configName, string configValue, string diagnosticId)
    {
        var source =
            $@"using NServiceBus;
using System;
using System.Threading.Tasks;

class SomeOtherClass
{{
    internal int EntityMaximumSize {{ get; set; }}
    internal bool EnablePartitioning {{ get; set; }}
    internal TimeSpan MaxAutoLockRenewalDuration {{ get; set; }}
    internal int PrefetchCount {{ get; set; }}
    internal int PrefetchMultiplier {{ get; set; }}
    internal TimeSpan TimeToWaitBeforeTriggeringCircuitBreaker {{ get; set; }}
}}

class Foo
{{
    void Direct(SomeOtherClass endpointConfig)
    {{
        endpointConfig.{configName} = {configValue};
    }}
}}";

        return Assert(source, diagnosticId);
    }
}