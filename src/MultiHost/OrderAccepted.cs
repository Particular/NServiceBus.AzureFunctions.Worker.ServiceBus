namespace MultiHost;

[BelongsTo(nameof(BillingFunctions.Billing))]
public class OrderAccepted : IEvent;