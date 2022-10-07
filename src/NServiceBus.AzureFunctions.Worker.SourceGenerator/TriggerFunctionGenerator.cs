namespace NServiceBus.AzureFunctions.SourceGenerator
{
    using System;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class TriggerFunctionGenerator : ISourceGenerator
    {
        internal static readonly DiagnosticDescriptor InvalidEndpointNameError = new DiagnosticDescriptor(
            id: "NSBWFUNC 001",
            title: "Invalid Endpoint Name",
            messageFormat: "Endpoint name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidTriggerFunctionNameError = new DiagnosticDescriptor(
            id: "NSBWFUNC 002",
            title: "Invalid Trigger Function Name",
            messageFormat: "Trigger function name is invalid and cannot be used to generate trigger function",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidBindingExpression = new DiagnosticDescriptor(
            id: "NSBWFUNC 003",
            title: "Invalid binding expression pattern use",
            messageFormat: "Binding expression patterns require that a TriggerFunctionName be specified",
            category: "TriggerFunctionGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);


        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            internal string endpointName;
            internal string triggerFunctionName;
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

                    // 2nd parameter was triggerFunctionName
                    triggerFunctionName = AttributeParameterAtPosition(1);
                }

                bool IsNServiceBusEndpointNameAttribute(string value) => value?.Equals("NServiceBus.NServiceBusTriggerFunctionAttribute") ?? false;
                string AttributeParameterAtPosition(int position) => context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[position].Expression).ToString();
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
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointNameError, Location.None, syntaxReceiver.endpointName));
                return;
            }

            // Generate an error if a binding expression is provided with no trigger function name
            if (syntaxReceiver.isInvalidBindingExpression)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidBindingExpression, Location.None, syntaxReceiver.endpointName));
                return;
            }

            // Generate an error if empty/null/space is used as trigger function name
            if (string.IsNullOrWhiteSpace(syntaxReceiver.triggerFunctionName))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidTriggerFunctionNameError, Location.None, syntaxReceiver.triggerFunctionName));
                return;
            }

            var source =
$@"// <autogenerated/>
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using NServiceBus;

public class FunctionEndpointTrigger
{{
    IFunctionEndpoint endpoint;

        public FunctionEndpointTrigger(IFunctionEndpoint endpoint)
        {{
            this.endpoint = endpoint;
        }}


        [Function(""{syntaxReceiver.triggerFunctionName}"")]
        public async Task Run(
            [ServiceBusTrigger(""{syntaxReceiver.endpointName}"")] byte[] messageBody,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            FunctionContext context)
        {{
            await endpoint.Process(messageBody, userProperties, messageId, deliveryCount, replyTo, correlationId, context);
        }}
}}";
            context.AddSource("NServiceBus__FunctionEndpointTrigger", SourceText.From(source, Encoding.UTF8));
        }
    }
}
