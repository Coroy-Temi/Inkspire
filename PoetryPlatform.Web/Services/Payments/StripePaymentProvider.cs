using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models.Payments;
using Stripe;
using Stripe.Checkout;

namespace PoetryPlatform.Web.Services.Payments;

public class StripePaymentProvider(IOptions<StripePaymentOptions> options) : IPaymentProvider
{
    private readonly StripePaymentOptions _options = options.Value;

    public PaymentProviderType ProviderType => PaymentProviderType.Stripe;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<PaymentCheckoutSessionResult> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return new PaymentCheckoutSessionResult
            {
                ErrorMessage = "Stripe secret key is not configured."
            };
        }

        try
        {
            StripeConfiguration.ApiKey = _options.SecretKey;

            var service = new SessionService();
            var session = await service.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    CustomerEmail = request.BuyerEmail,
                    ClientReferenceId = request.OrderId.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        ["order_id"] = request.OrderId.ToString(),
                        ["order_number"] = request.OrderNumber,
                        ["buyer_id"] = request.BuyerId,
                        ["author_id"] = request.AuthorId
                    },
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLowerInvariant(),
                                UnitAmountDecimal = request.Amount * 100m,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.ItemName,
                                    Description = request.ItemDescription
                                }
                            }
                        }
                    ]
                },
                requestOptions: null,
                cancellationToken: cancellationToken);

            return new PaymentCheckoutSessionResult
            {
                Succeeded = true,
                CheckoutUrl = session.Url,
                ExternalCheckoutSessionId = session.Id,
                ExternalPaymentIntentId = session.PaymentIntentId,
                RawPayloadJson = session.StripeResponse?.Content
            };
        }
        catch (StripeException ex)
        {
            return new PaymentCheckoutSessionResult
            {
                ErrorMessage = ex.StripeError?.Message ?? ex.Message
            };
        }
    }

    public async Task<PaymentCheckoutStatusResult> GetCheckoutStatusAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return new PaymentCheckoutStatusResult
            {
                ErrorMessage = "Stripe secret key is not configured."
            };
        }

        try
        {
            StripeConfiguration.ApiKey = _options.SecretKey;

            var service = new SessionService();
            var session = await service.GetAsync(
                checkoutSessionId,
                cancellationToken: cancellationToken);

            var amount = session.AmountTotal.HasValue
                ? Math.Round(session.AmountTotal.Value / 100m, 2, MidpointRounding.AwayFromZero)
                : (decimal?)null;

            var paid = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase);
            var complete = string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase);

            return new PaymentCheckoutStatusResult
            {
                Succeeded = true,
                OrderId = TryResolveOrderId(session),
                ExternalCheckoutSessionId = session.Id,
                ExternalPaymentIntentId = session.PaymentIntentId,
                ExternalReference = session.PaymentIntentId ?? session.Id,
                ObjectType = session.Object,
                Amount = amount,
                Currency = session.Currency ?? "usd",
                OrderStatus = paid || complete ? OrderStatus.Paid : OrderStatus.CheckoutCreated,
                PaymentStatus = paid ? PaymentRecordStatus.Succeeded : PaymentRecordStatus.Pending,
                FailureReason = paid ? null : "Stripe has not marked this checkout session as paid yet.",
                RawPayloadJson = session.StripeResponse?.Content
            };
        }
        catch (StripeException ex)
        {
            return new PaymentCheckoutStatusResult
            {
                ErrorMessage = ex.StripeError?.Message ?? ex.Message
            };
        }
    }

    public PaymentWebhookEventResult ParseWebhook(string payloadJson, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return new PaymentWebhookEventResult
            {
                ErrorMessage = "Stripe webhook secret is not configured.",
                RawPayloadJson = payloadJson
            };
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payloadJson, signatureHeader, _options.WebhookSecret);
            var session = stripeEvent.Data.Object as Session;

            if (session == null)
            {
                return new PaymentWebhookEventResult
                {
                    IsValid = true,
                    ExternalEventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    ObjectType = stripeEvent.Data.Object?.Object ?? "unknown",
                    RawPayloadJson = payloadJson
                };
            }

            var orderId = TryResolveOrderId(session);
            var amount = session.AmountTotal.HasValue
                ? Math.Round(session.AmountTotal.Value / 100m, 2, MidpointRounding.AwayFromZero)
                : (decimal?)null;

            return stripeEvent.Type switch
            {
                EventTypes.CheckoutSessionCompleted => BuildResult(
                    stripeEvent,
                    session,
                    orderId,
                    amount,
                    OrderStatus.Paid,
                    PaymentRecordStatus.Succeeded,
                    payloadJson),
                EventTypes.CheckoutSessionAsyncPaymentSucceeded => BuildResult(
                    stripeEvent,
                    session,
                    orderId,
                    amount,
                    OrderStatus.Paid,
                    PaymentRecordStatus.Succeeded,
                    payloadJson),
                EventTypes.CheckoutSessionAsyncPaymentFailed => BuildResult(
                    stripeEvent,
                    session,
                    orderId,
                    amount,
                    OrderStatus.Failed,
                    PaymentRecordStatus.Failed,
                    payloadJson,
                    "Stripe marked the checkout session as failed."),
                EventTypes.CheckoutSessionExpired => BuildResult(
                    stripeEvent,
                    session,
                    orderId,
                    amount,
                    OrderStatus.Cancelled,
                    PaymentRecordStatus.Cancelled,
                    payloadJson,
                    "Stripe checkout session expired."),
                _ => new PaymentWebhookEventResult
                {
                    IsValid = true,
                    ExternalEventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    OrderId = orderId,
                    ExternalCheckoutSessionId = session.Id,
                    ExternalPaymentIntentId = session.PaymentIntentId,
                    ExternalReference = session.PaymentIntentId ?? session.Id ?? stripeEvent.Id,
                    ObjectType = session.Object,
                    Amount = amount,
                    Currency = session.Currency ?? "usd",
                    RawPayloadJson = payloadJson
                }
            };
        }
        catch (Exception ex) when (ex is StripeException || ex is ArgumentException)
        {
            return new PaymentWebhookEventResult
            {
                ErrorMessage = ex.Message,
                RawPayloadJson = payloadJson
            };
        }
    }

    private static int? TryResolveOrderId(Session session)
    {
        if (session.Metadata != null
            && session.Metadata.TryGetValue("order_id", out var metadataValue)
            && int.TryParse(metadataValue, out var metadataOrderId))
        {
            return metadataOrderId;
        }

        return int.TryParse(session.ClientReferenceId, out var clientReferenceOrderId)
            ? clientReferenceOrderId
            : null;
    }

    private static PaymentWebhookEventResult BuildResult(
        Event stripeEvent,
        Session session,
        int? orderId,
        decimal? amount,
        OrderStatus orderStatus,
        PaymentRecordStatus paymentStatus,
        string rawPayloadJson,
        string? failureReason = null)
    {
        return new PaymentWebhookEventResult
        {
            IsValid = true,
            ExternalEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            OrderId = orderId,
            ExternalCheckoutSessionId = session.Id,
            ExternalPaymentIntentId = session.PaymentIntentId,
            ExternalReference = session.PaymentIntentId ?? session.Id ?? stripeEvent.Id,
            ObjectType = session.Object,
            Amount = amount,
            Currency = session.Currency ?? "usd",
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus,
            FailureReason = failureReason,
            RawPayloadJson = rawPayloadJson
        };
    }
}
