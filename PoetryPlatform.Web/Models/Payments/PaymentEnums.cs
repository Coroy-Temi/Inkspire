namespace PoetryPlatform.Web.Models.Payments;

public enum PaymentProviderType
{
    Stripe = 1,
    Paystack = 2
}

public enum OrderStatus
{
    Pending = 1,
    CheckoutCreated = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum PaymentRecordStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum WebhookLogStatus
{
    Received = 1,
    Processed = 2,
    Duplicate = 3,
    Failed = 4
}

public enum CheckoutRoutingMode
{
    PlatformDefault = 1,
    AuthorCountry = 2,
    AuthorPreference = 3
}

public enum AuthorPayoutAccountStatus
{
    NotStarted = 1,
    Pending = 2,
    NeedsReview = 3,
    Active = 4,
    Disabled = 5
}

public enum AuthorEarningStatus
{
    Pending = 1,
    Available = 2,
    InPayout = 3,
    Paid = 4,
    Cancelled = 5
}

public enum PayoutBatchStatus
{
    Draft = 1,
    Processing = 2,
    Paid = 3,
    Failed = 4
}

public enum PayoutItemStatus
{
    Pending = 1,
    Submitted = 2,
    Paid = 3,
    Failed = 4
}

public enum SubscriptionPlanInterval
{
    Monthly = 1,
    Yearly = 2
}

public enum ReaderSubscriptionStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    Cancelled = 4,
    Expired = 5
}
