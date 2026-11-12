using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaystackPaymentProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<PaystackPaymentOptions> options) : IPaymentProvider
{
    private readonly PaystackPaymentOptions _options = options.Value;

    public PaymentProviderType ProviderType => PaymentProviderType.Paystack;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<PaymentCheckoutSessionResult> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PaymentCheckoutSessionResult
            {
                ErrorMessage = "Paystack secret key is not configured."
            };
        }

        if (string.IsNullOrWhiteSpace(request.BuyerEmail))
        {
            return new PaymentCheckoutSessionResult
            {
                ErrorMessage = "Paystack checkout needs a buyer email address."
            };
        }

        var reference = request.OrderNumber;
        var payload = new
        {
            email = request.BuyerEmail,
            amount = (int)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero),
            currency = request.Currency.ToUpperInvariant(),
            reference,
            subaccount = request.RecipientSubaccountCode,
            transaction_charge = request.PlatformFeeAmount > 0
                ? (int?)Math.Round(request.PlatformFeeAmount * 100m, MidpointRounding.AwayFromZero)
                : null,
            callback_url = request.SuccessUrl,
            metadata = new
            {
                order_id = request.OrderId,
                order_number = request.OrderNumber,
                buyer_id = request.BuyerId,
                author_id = request.AuthorId,
                item_name = request.ItemName,
                split_mode = string.IsNullOrWhiteSpace(request.RecipientSubaccountCode) ? "platform" : "subaccount"
            }
        };

        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/transaction/initialize", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaymentCheckoutSessionResult
            {
                ErrorMessage = $"Paystack initialize request failed with {(int)response.StatusCode}.",
                RawPayloadJson = json
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        var authorizationUrl = GetString(data, "authorization_url");
        var accessCode = GetString(data, "access_code");
        var externalReference = GetString(data, "reference") ?? reference;
        var message = GetString(document.RootElement, "message");

        return string.IsNullOrWhiteSpace(authorizationUrl)
            ? new PaymentCheckoutSessionResult
            {
                ErrorMessage = string.IsNullOrWhiteSpace(message) ? "Paystack did not return an authorization URL." : message,
                RawPayloadJson = json
            }
            : new PaymentCheckoutSessionResult
            {
                Succeeded = true,
                CheckoutUrl = authorizationUrl,
                ExternalCheckoutSessionId = externalReference,
                ExternalPaymentIntentId = accessCode,
                RawPayloadJson = json
            };
    }

    public async Task<PaymentCheckoutStatusResult> GetCheckoutStatusAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PaymentCheckoutStatusResult
            {
                ErrorMessage = "Paystack secret key is not configured."
            };
        }

        var client = CreateClient();
        using var response = await client.GetAsync($"/transaction/verify/{Uri.EscapeDataString(checkoutSessionId)}", cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaymentCheckoutStatusResult
            {
                ErrorMessage = $"Paystack verification failed with {(int)response.StatusCode}.",
                RawPayloadJson = json
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        var metadata = GetProperty(data, "metadata");
        var status = GetString(data, "status");
        var gatewayResponse = GetString(data, "gateway_response");
        var currency = GetString(data, "currency")?.ToLowerInvariant() ?? "usd";
        var amountInMinorUnits = GetDecimal(data, "amount");
        var reference = GetString(data, "reference") ?? checkoutSessionId;
        var transactionId = GetString(data, "id");
        var orderId = GetInt(metadata, "order_id");
        var succeeded = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

        return new PaymentCheckoutStatusResult
        {
            Succeeded = true,
            OrderId = orderId,
            ExternalCheckoutSessionId = reference,
            ExternalPaymentIntentId = transactionId,
            ExternalReference = reference,
            ObjectType = "transaction",
            Amount = amountInMinorUnits / 100m,
            Currency = currency,
            OrderStatus = succeeded ? OrderStatus.Paid : OrderStatus.CheckoutCreated,
            PaymentStatus = succeeded ? PaymentRecordStatus.Succeeded : PaymentRecordStatus.Pending,
            FailureReason = succeeded ? null : gatewayResponse ?? "Paystack has not marked this transaction as successful yet.",
            RawPayloadJson = json
        };
    }

    public PaymentWebhookEventResult ParseWebhook(string payloadJson, string? signatureHeader)
    {
        if (!IsConfigured)
        {
            return new PaymentWebhookEventResult
            {
                ErrorMessage = "Paystack webhook secret is not configured.",
                RawPayloadJson = payloadJson
            };
        }

        if (!IsValidSignature(payloadJson, signatureHeader))
        {
            return new PaymentWebhookEventResult
            {
                ErrorMessage = "Paystack webhook signature verification failed.",
                RawPayloadJson = payloadJson
            };
        }

        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        var eventType = GetString(root, "event") ?? string.Empty;
        var data = GetProperty(root, "data");
        var metadata = GetProperty(data, "metadata");
        var status = GetString(data, "status") ?? string.Empty;
        var amountInMinorUnits = GetDecimal(data, "amount");

        if (eventType.StartsWith("transfer.", StringComparison.OrdinalIgnoreCase))
        {
            var payoutStatus = eventType switch
            {
                "transfer.success" => PayoutItemStatus.Paid,
                "transfer.failed" => PayoutItemStatus.Failed,
                "transfer.reversed" => PayoutItemStatus.Failed,
                _ => PayoutItemStatus.Submitted
            };

            return new PaymentWebhookEventResult
            {
                IsValid = true,
                ExternalEventId = GetString(data, "id") ?? GetString(data, "transfer_code") ?? Guid.NewGuid().ToString("N"),
                EventType = eventType,
                ExternalTransferId = GetString(data, "transfer_code"),
                ExternalReference = GetString(data, "reference"),
                ObjectType = "transfer",
                Amount = amountInMinorUnits / 100m,
                Currency = GetString(data, "currency")?.ToLowerInvariant() ?? "ngn",
                PayoutItemStatus = payoutStatus,
                PaidAt = GetDateTime(data, "transferred_at"),
                FailureReason = GetString(data, "failures"),
                RawPayloadJson = payloadJson
            };
        }

        var succeeded = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

        return new PaymentWebhookEventResult
        {
            IsValid = true,
            ExternalEventId = GetString(data, "id") ?? GetString(data, "reference") ?? Guid.NewGuid().ToString("N"),
            EventType = eventType,
            OrderId = GetInt(metadata, "order_id"),
            ExternalCheckoutSessionId = GetString(data, "reference"),
            ExternalPaymentIntentId = GetString(data, "id"),
            ExternalReference = GetString(data, "reference"),
            ObjectType = "transaction",
            Amount = amountInMinorUnits / 100m,
            Currency = GetString(data, "currency")?.ToLowerInvariant() ?? "usd",
            OrderStatus = succeeded && string.Equals(eventType, "charge.success", StringComparison.OrdinalIgnoreCase)
                ? OrderStatus.Paid
                : null,
            PaymentStatus = succeeded && string.Equals(eventType, "charge.success", StringComparison.OrdinalIgnoreCase)
                ? PaymentRecordStatus.Succeeded
                : null,
            FailureReason = succeeded ? null : GetString(data, "gateway_response"),
            RawPayloadJson = payloadJson
        };
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("Paystack");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private bool IsValidSignature(string payloadJson, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var secretBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        using var hmac = new HMACSHA512(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, signatureHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement GetProperty(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property
            : default;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsedValue)
            ? parsedValue
            : null;
    }

    private static decimal GetDecimal(JsonElement element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numericValue))
        {
            return numericValue;
        }

        return property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out var parsedValue)
            ? parsedValue
            : 0m;
    }

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        if (property.ValueKind == JsonValueKind.String && DateTime.TryParse(property.GetString(), out var parsedValue))
        {
            return DateTime.SpecifyKind(parsedValue, DateTimeKind.Utc);
        }

        return null;
    }
}
