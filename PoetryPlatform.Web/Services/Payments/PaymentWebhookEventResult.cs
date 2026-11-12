using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaymentWebhookEventResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string ExternalEventId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public int? OrderId { get; init; }
    public string? ExternalCheckoutSessionId { get; init; }
    public string? ExternalPaymentIntentId { get; init; }
    public string? ExternalChargeId { get; init; }
    public string? ExternalReference { get; init; }
    public string ObjectType { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public string Currency { get; init; } = "usd";
    public OrderStatus? OrderStatus { get; init; }
    public PaymentRecordStatus? PaymentStatus { get; init; }
    public PayoutItemStatus? PayoutItemStatus { get; init; }
    public string? ExternalTransferId { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? FailureReason { get; init; }
    public string RawPayloadJson { get; init; } = string.Empty;
}
