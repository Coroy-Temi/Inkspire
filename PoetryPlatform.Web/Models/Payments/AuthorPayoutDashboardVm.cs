namespace PoetryPlatform.Web.Models.Payments;

public class AuthorPayoutDashboardVm
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "US";
    public string Currency { get; set; } = "ngn";
    public PaymentProviderType? PreferredCheckoutProvider { get; set; }
    public PaymentProviderType EffectiveCheckoutProvider { get; set; } = PaymentProviderType.Paystack;
    public CheckoutRoutingMode CheckoutRoutingMode { get; set; }
    public PaymentProviderType PayoutProvider { get; set; } = PaymentProviderType.Paystack;
    public bool StripeCheckoutConfigured { get; set; }
    public bool PaystackCheckoutConfigured { get; set; }
    public bool PaystackTransfersConfigured { get; set; }
    public bool StripePayoutConfigured { get; set; }
    public bool ReaderSubscriptionsEnabled { get; set; }
    public AuthorPayoutAccount? PayoutAccount { get; set; }
    public List<PaystackBankOption> SupportedBanks { get; set; } = [];
    public int PendingEarningsCount { get; set; }
    public decimal PendingEarningsAmount { get; set; }
    public int AvailableEarningsCount { get; set; }
    public decimal AvailableEarningsAmount { get; set; }
    public int PaidEarningsCount { get; set; }
    public decimal PaidEarningsAmount { get; set; }
    public int SubscriptionPlanCount { get; set; }
    public int ActiveReaderSubscriptions { get; set; }
    public List<AuthorPayoutEarningVm> RecentEarnings { get; set; } = [];
}

public class AuthorPayoutEarningVm
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Currency { get; set; } = "usd";
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public AuthorEarningStatus Status { get; set; }
    public DateTime EarnedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
