namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

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