namespace PoetryPlatform.Web.Services.Payments;

public class PaymentCheckoutSessionResult
{
    public bool Succeeded { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? ExternalCheckoutSessionId { get; init; }
    public string? ExternalPaymentIntentId { get; init; }
    public string? RawPayloadJson { get; init; }
    public string? ErrorMessage { get; init; }
}
