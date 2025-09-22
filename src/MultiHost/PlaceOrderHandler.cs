namespace MultiHost;

[BelongsTo(nameof(SalesFunction.Sales))]
public class PlaceOrderHandler : IHandleMessages<PlaceOrder>
{
    public Task Handle(PlaceOrder message, IMessageHandlerContext context)
    {
        Console.WriteLine("Placing order");
        return Task.CompletedTask;
    }
}