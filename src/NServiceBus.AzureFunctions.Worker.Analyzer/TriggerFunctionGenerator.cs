namespace NServiceBus.AzureFunctions.Worker.Analyzer
{
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class TriggerFunctionGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            internal string endpointName;
            internal string triggerFunctionName;
            internal string connection;
            internal bool attributeFound;
            internal bool isInvalidBindingExpression = false;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is AttributeSyntax attributeSyntax
                    && IsNServiceBusEndpointNameAttribute(context.SemanticModel.GetTypeInfo(attributeSyntax).Type?.ToDisplayString()))
                {
                    attributeFound = true;

                    // Assign guaranteed endpoint/queue name and handle the defaults
                    endpointName = AttributeParameterAtPosition(0);
                    triggerFunctionName = $"NServiceBusFunctionEndpointTrigger-{endpointName}";

                    var attributeParametersCount = AttributeParametersCount();

                    if (attributeParametersCount == 1)
                    {
                        if (IsBindingExpression(endpointName))
                        {
                            isInvalidBindingExpression = true;
                        }
                        return;
                    }

                    var triggerFunctionNameAttribute = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.GetFirstToken().ValueText == "TriggerFunctionName");
                    if (triggerFunctionNameAttribute != null)
                    {
                        triggerFunctionName = context.SemanticModel.GetConstantValue(triggerFunctionNameAttribute.Expression).Value?.ToString();
                    }

                    var connectionAttribute = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(arg => arg.GetFirstToken().ValueText == "Connection");
                    if (connectionAttribute != null)
                    {
                        connection = context.SemanticModel.GetConstantValue(connectionAttribute.Expression).Value?.ToString();
                    }
                }

                bool IsNServiceBusEndpointNameAttribute(string value) => value?.Equals("NServiceBus.NServiceBusTriggerFunctionAttribute") ?? false;
                string AttributeParameterAtPosition(int position) => context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[position].Expression).Value?.ToString();
                int AttributeParametersCount() => attributeSyntax.ArgumentList.Arguments.Count;
                bool IsBindingExpression(string endpointName) => !string.IsNullOrWhiteSpace(endpointName) && endpointName[0] == '%' && endpointName[0] == endpointName[endpointName.Length - 1];
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Short circuit if this is a different syntax receiver
            if (!(context.SyntaxContextReceiver is SyntaxReceiver syntaxReceiver))
            {
                return;
            }

            // Skip processing if no attribute was found
            if (!syntaxReceiver.attributeFound)
            {
                return;
            }

            // Generate an error if empty/null/space is used as endpoint name
            if (string.IsNullOrWhiteSpace(syntaxReceiver.endpointName))
            {
                context.ReportDiagnostic(Diagnostic.Create(AzureFunctionsDiagnostics.InvalidEndpointNameError, Location.None, syntaxReceiver.endpointName));
                return;
            }

            // Generate an error if a binding expression is provided with no trigger function name
            if (syntaxReceiver.isInvalidBindingExpression)
            {
                context.ReportDiagnostic(Diagnostic.Create(AzureFunctionsDiagnostics.InvalidBindingExpression, Location.None, syntaxReceiver.endpointName));
                return;
            }

            // Generate an error if empty/null/space is used as trigger function name
            if (string.IsNullOrWhiteSpace(syntaxReceiver.triggerFunctionName))
            {
                context.ReportDiagnostic(Diagnostic.Create(AzureFunctionsDiagnostics.InvalidTriggerFunctionNameError, Location.None, syntaxReceiver.triggerFunctionName));
                return;
            }

            var connectionParam = string.IsNullOrWhiteSpace(syntaxReceiver.connection)
                ? ""
                : $", Connection=\"{syntaxReceiver.connection}\"";
            var source =
$@"// <autogenerated/>
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using NServiceBus;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
public class FunctionEndpointTrigger
{{
        readonly IFunctionEndpoint endpoint;

        public FunctionEndpointTrigger(IFunctionEndpoint endpoint)
        {{
            this.endpoint = endpoint;
        }}

        [Function(""{syntaxReceiver.triggerFunctionName}"")]
        public async Task Run(
            [ServiceBusTrigger(""{syntaxReceiver.endpointName}""{connectionParam})] ServiceBusReceivedMessage message, FunctionContext context, CancellationToken cancellationToken)
        {{
            await endpoint.Process(message, context, cancellationToken);
        }}
}}";
            context.AddSource("NServiceBus__FunctionEndpointTrigger", SourceText.From(source, Encoding.UTF8));
        }
    }
}
