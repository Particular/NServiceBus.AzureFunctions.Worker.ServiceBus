namespace Sales;

public class PlaceOrder : ICommand
{
    public Guid OrderId { get; set; }
}

public class OrderPlaced : IEvent
{
    public Guid OrderId { get; set; }
}