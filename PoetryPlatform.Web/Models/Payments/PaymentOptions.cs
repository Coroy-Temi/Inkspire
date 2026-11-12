namespace PoetryPlatform.Web.Models.Payments;

public class PaymentPlatformOptions
{
    public const string SectionName = "Payments";

    public string DefaultProvider { get; set; } = "Paystack";
    public string PayoutProvider { get; set; } = "Paystack";
    public string DefaultCurrency { get; set; } = "ngn";
    public decimal PlatformCommissionPercent { get; set; } = 12m;
    public CheckoutRoutingMode CheckoutRoutingMode { get; set; } = CheckoutRoutingMode.AuthorPreference;
    public List<string> PaystackCountryCodes { get; set; } = ["NG", "GH", "ZA", "KE"];
    public bool ReaderSubscriptionsEnabled { get; set; }
}

public class StripePaymentOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public class PaystackPaymentOptions
{
    public const string SectionName = "Paystack";

    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.paystack.co";
    public bool BulkTransfersEnabled { get; set; } = true;
}
