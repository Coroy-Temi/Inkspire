using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public interface IPaymentProvider
{
    PaymentProviderType ProviderType { get; }
    bool IsConfigured { get; }

    Task<PaymentCheckoutSessionResult> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentCheckoutStatusResult> GetCheckoutStatusAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default);

    PaymentWebhookEventResult ParseWebhook(string payloadJson, string? signatureHeader);
}
