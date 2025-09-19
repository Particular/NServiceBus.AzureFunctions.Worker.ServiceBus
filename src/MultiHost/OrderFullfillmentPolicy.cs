namespace MultiHost;

public class OrderFullfillmentPolicy : Saga<OrderFullfillmentPolicyData>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderFullfillmentPolicyData> mapper)
    {
    }
}