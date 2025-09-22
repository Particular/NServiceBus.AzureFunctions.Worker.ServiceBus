namespace MultiHost;

public static partial class SalesInitializationContextExtensions
{
    public static void AddHandlers(this SalesFunction.SalesInitializationContext context) =>
        context.Configuration.AddHandler<PlaceOrderHandler>();
}