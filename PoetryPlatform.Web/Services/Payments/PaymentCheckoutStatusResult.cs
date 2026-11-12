using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaymentCheckoutStatusResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public int? OrderId { get; init; }
    public string ExternalCheckoutSessionId { get; init; } = string.Empty;
    public string? ExternalPaymentIntentId { get; init; }
    public string? ExternalChargeId { get; init; }
    public string? ExternalReference { get; init; }
    public string ObjectType { get; init; } = "checkout_session";
    public decimal? Amount { get; init; }
    public string Currency { get; init; } = "usd";
    public OrderStatus OrderStatus { get; init; } = OrderStatus.Pending;
    public PaymentRecordStatus PaymentStatus { get; init; } = PaymentRecordStatus.Pending;
    public string? FailureReason { get; init; }
    public string? RawPayloadJson { get; init; }
}
