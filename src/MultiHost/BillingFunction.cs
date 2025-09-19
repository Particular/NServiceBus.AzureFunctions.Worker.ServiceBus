namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public partial class BillingFunctions
{
    [Function(nameof(Billing))]
    public partial Task Billing(
        [ServiceBusTrigger("billing", Connection = "ServiceBusConnection2", AutoCompleteMessages = false)] //Using a separate namespace would need the bridge for the individual endpoints to talk to each other
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    partial void Configure(BillingInitializationContext context)
    {
        context.Configuration.AddHandler<OrderAcceptedHandler>();
    }

    [Function(nameof(Invoices))]
    public partial Task Invoices(
        [ServiceBusTrigger("invoices", Connection = "ServiceBusConnection2", AutoCompleteMessages = false)] //Using a separate namespace would need the bridge for the individual endpoints to talk to each other
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    partial void Configure(InvoicesInitializationContext context)
    {
        context.Configuration.AddHandler<OrderAcceptedHandler>();
    }
}

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
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        await billing.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    partial void Configure(BillingInitializationContext context);

    public class InvoicesInitializationContext : InitializationContext;

    public async partial Task Invoices(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        await invoices.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
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