namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public partial class BillingFunctions : IConfigureEndpoint
{
    readonly FunctionEndpoint billing;
    readonly FunctionEndpoint invoices;

    public BillingFunctions([FromKeyedServices(nameof(Billing))] FunctionEndpoint billing, [FromKeyedServices(nameof(Invoices))] FunctionEndpoint invoices)
    {
        this.billing = billing;
        this.invoices = invoices;
    }

    // for configuration
    public BillingFunctions()
    {
    }

    public class BillingInitializationContext : InitializationContext;

    public async partial Task Billing(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        await billing.Process(context, message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    partial void Configure(BillingInitializationContext context);

    public class InvoicesInitializationContext : InitializationContext;

    public async partial Task Invoices(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        await invoices.Process(context, message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    partial void Configure(InvoicesInitializationContext context);

    public void Configure(InitializationContext context)
    {
        switch (context)
        {
            case BillingInitializationContext billingContext:
                Configure(billingContext);
                break;
            case InvoicesInitializationContext invoicesContext:
                Configure(invoicesContext);
                break;
            default:
                break;
        }
    }
}