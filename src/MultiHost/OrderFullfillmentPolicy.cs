namespace MultiHost;

[BelongsTo(nameof(SalesFunction.Sales))]
public class OrderFullfillmentPolicy : Saga<OrderFullfillmentPolicyData>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderFullfillmentPolicyData> mapper)
    {
    }
}