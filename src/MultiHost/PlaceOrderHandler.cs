namespace Sales;

public class PlaceOrderHandler : IHandleMessages<PlaceOrder>
{
    public Task Handle(PlaceOrder message, IMessageHandlerContext context)
        => context.Publish(new OrderPlaced { OrderId = message.OrderId, });
}