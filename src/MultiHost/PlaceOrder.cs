namespace MultiHost;

[BelongsTo(nameof(SalesFunction.Sales))]
public class PlaceOrder : ICommand;