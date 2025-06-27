namespace NServiceBus.AzureFunctions.Worker.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NServiceBus.AzureFunctions.Analyzer.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigurationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed,
            AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowed,
            AzureFunctionsDiagnostics.DefineCriticalErrorActionNotAllowed,
            AzureFunctionsDiagnostics.SetDiagnosticsPathNotAllowed,
            AzureFunctionsDiagnostics.MakeInstanceUniquelyAddressableNotAllowed,
            AzureFunctionsDiagnostics.UseTransportNotAllowed,
            AzureFunctionsDiagnostics.OverrideLocalAddressNotAllowed,
            AzureFunctionsDiagnostics.RouteReplyToThisInstanceNotAllowed,
            AzureFunctionsDiagnostics.RouteToThisInstanceNotAllowed,
            AzureFunctionsDiagnostics.MaxAutoLockRenewalDurationNotAllowed,
            AzureFunctionsDiagnostics.PrefetchCountNotAllowed,
            AzureFunctionsDiagnostics.PrefetchMultiplierNotAllowed,
            AzureFunctionsDiagnostics.TimeToWaitBeforeTriggeringCircuitBreakerNotAllowed,
            AzureFunctionsDiagnostics.TransportTransactionModeNotAllowed,
            AzureFunctionsDiagnostics.LogDiagnosticsInfo
        );

        static readonly Dictionary<string, DiagnosticDescriptor> NotAllowedEndpointConfigurationMethods
            = new Dictionary<string, DiagnosticDescriptor>
            {
                ["PurgeOnStartup"] = AzureFunctionsDiagnostics.PurgeOnStartupNotAllowed,
                ["LimitMessageProcessingConcurrencyTo"] = AzureFunctionsDiagnostics.LimitMessageProcessingToNotAllowed,
                ["DefineCriticalErrorAction"] = AzureFunctionsDiagnostics.DefineCriticalErrorActionNotAllowed,
                ["SetDiagnosticsPath"] = AzureFunctionsDiagnostics.SetDiagnosticsPathNotAllowed,
                ["MakeInstanceUniquelyAddressable"] = AzureFunctionsDiagnostics.MakeInstanceUniquelyAddressableNotAllowed,
                ["UseTransport"] = AzureFunctionsDiagnostics.UseTransportNotAllowed,
                ["OverrideLocalAddress"] = AzureFunctionsDiagnostics.OverrideLocalAddressNotAllowed,
            };

        static readonly Dictionary<string, DiagnosticDescriptor> NotAllowedSendAndReplyOptions
            = new Dictionary<string, DiagnosticDescriptor>
            {
                ["RouteReplyToThisInstance"] = AzureFunctionsDiagnostics.RouteReplyToThisInstanceNotAllowed,
                ["RouteToThisInstance"] = AzureFunctionsDiagnostics.RouteToThisInstanceNotAllowed,
            };

        static readonly Dictionary<string, DiagnosticDescriptor> NotAllowedTransportSettings
            = new Dictionary<string, DiagnosticDescriptor>
            {
                ["MaxAutoLockRenewalDuration"] = AzureFunctionsDiagnostics.MaxAutoLockRenewalDurationNotAllowed,
                ["PrefetchCount"] = AzureFunctionsDiagnostics.PrefetchCountNotAllowed,
                ["PrefetchMultiplier"] = AzureFunctionsDiagnostics.PrefetchMultiplierNotAllowed,
                ["TimeToWaitBeforeTriggeringCircuitBreaker"] = AzureFunctionsDiagnostics.TimeToWaitBeforeTriggeringCircuitBreakerNotAllowed,
                ["TransportTransactionMode"] = AzureFunctionsDiagnostics.TransportTransactionModeNotAllowed,
                ["Transactions"] = AzureFunctionsDiagnostics.TransportTransactionModeNotAllowed
            };

        static readonly Dictionary<string, DiagnosticDescriptor> InfoEndpointConfigurationMethods
            = new Dictionary<string, DiagnosticDescriptor>
            {
                ["LogDiagnostics"] = AzureFunctionsDiagnostics.LogDiagnosticsInfo
            };

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeTransport, SyntaxKind.SimpleMemberAccessExpression);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocationExpression)
            {
                return;
            }

            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            {
                return;
            }

            AnalyzeEndpointConfiguration(context, invocationExpression, memberAccessExpression);

            AnalyzeSendAndReplyOptions(context, invocationExpression, memberAccessExpression);

            AnalyzeTransportExtensions(context, invocationExpression, memberAccessExpression);

            AnalyzeLogDiagnostics(context, invocationExpression, memberAccessExpression);
        }

        static void AnalyzeTransport(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (!NotAllowedTransportSettings.TryGetValue(memberAccess.Name.ToString(), out var diagnosticDescriptor))
            {
                return;

            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);

            if (memberAccessSymbol.Symbol is not IPropertySymbol propertySymbol)
            {
                return;
            }

            if (propertySymbol.ContainingType.ToString() is "NServiceBus.AzureServiceBusTransport" or "NServiceBus.Transport.TransportDefinition")
            {
                context.ReportDiagnostic(diagnosticDescriptor, memberAccess);

            }
        }

        static void AnalyzeEndpointConfiguration(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (!NotAllowedEndpointConfigurationMethods.TryGetValue(memberAccessExpression.Name.Identifier.Text, out var diagnosticDescriptor))
            {
                return;
            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);

            if (memberAccessSymbol.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (methodSymbol.ReceiverType.ToString() == "NServiceBus.EndpointConfiguration")
            {
                context.ReportDiagnostic(diagnosticDescriptor, invocationExpression);
            }
        }

        static void AnalyzeSendAndReplyOptions(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (!NotAllowedSendAndReplyOptions.TryGetValue(memberAccessExpression.Name.Identifier.Text, out var diagnosticDescriptor))
            {
                return;
            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);

            if (memberAccessSymbol.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (methodSymbol.ReceiverType.ToString() is "NServiceBus.SendOptions" or "NServiceBus.ReplyOptions")
            {
                context.ReportDiagnostic(diagnosticDescriptor, invocationExpression);
            }
        }

        static void AnalyzeTransportExtensions(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (!NotAllowedTransportSettings.TryGetValue(memberAccessExpression.Name.Identifier.Text, out var diagnosticDescriptor))
            {
                return;
            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);

            if (memberAccessSymbol.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (methodSymbol.ReceiverType.ToString() == "NServiceBus.TransportExtensions<NServiceBus.AzureServiceBusTransport>")
            {
                context.ReportDiagnostic(diagnosticDescriptor, invocationExpression);
            }
        }

        static void AnalyzeLogDiagnostics(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression, MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (!InfoEndpointConfigurationMethods.TryGetValue(memberAccessExpression.Name.Identifier.Text, out var diagnosticDescriptor))
            {
                return;
            }

            var memberAccessSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);

            if ((memberAccessSymbol.Symbol is IMethodSymbol methodSymbol)
                && (methodSymbol.ReceiverType.ToString() == "NServiceBus.ServiceBusTriggeredEndpointConfiguration"))
            {
                context.ReportDiagnostic(diagnosticDescriptor, invocationExpression);
            }
        }
    }
}