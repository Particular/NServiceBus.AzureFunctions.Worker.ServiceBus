namespace MultiHost;

[BelongsTo(nameof(SalesFunction.Sales))]
public class PlaceOrder : ICommand
{
    public Guid OrderId { get; set; }
}

[BelongsTo(nameof(SalesFunction.Sales))]
public class OrderPlaced : IEvent
{
    public Guid OrderId { get; set; }
}