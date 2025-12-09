namespace MultiHost;

[BelongsTo(nameof(SalesFunction.Sales))]
public class OrderFullfillmentPolicy : Saga<OrderFullfillmentPolicyData>, IAmStartedByMessages<OrderPlaced>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderFullfillmentPolicyData> mapper) =>
        mapper.MapSaga(saga => saga.OrderId)
            .ToMessage<OrderPlaced>(msg => msg.OrderId);

    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        Data.OrderId = message.OrderId;
        return Task.CompletedTask;
    }
}