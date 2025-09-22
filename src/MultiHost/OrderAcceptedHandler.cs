namespace MultiHost;

[BelongsTo(nameof(BillingFunctions.Billing))]
public class OrderAcceptedHandler : IHandleMessages<OrderAccepted>
{
    public Task Handle(OrderAccepted message, IMessageHandlerContext context)
    {
        Console.WriteLine("Order accepted");
        return Task.CompletedTask;
    }
}