namespace Sales;

public static partial class SalesInitializationContextExtensions
{
    public static void AddSagas(this SalesFunction.SalesInitializationContext context) =>
        context.Configuration.AddSaga<OrderFullfillmentPolicy>();
}