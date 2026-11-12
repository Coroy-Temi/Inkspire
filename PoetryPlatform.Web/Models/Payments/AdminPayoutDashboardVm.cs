namespace PoetryPlatform.Web.Models.Payments;

public class AdminPayoutDashboardVm
{
    public CheckoutRoutingMode CheckoutRoutingMode { get; set; }
    public string DefaultCheckoutProvider { get; set; } = "Paystack";
    public string PayoutProvider { get; set; } = "Paystack";
    public bool PaystackTransfersConfigured { get; set; }
    public bool ReaderSubscriptionsEnabled { get; set; }
    public decimal PendingEarningsAmount { get; set; }
    public int PendingEarningsCount { get; set; }
    public decimal AvailableEarningsAmount { get; set; }
    public int AvailableEarningsCount { get; set; }
    public decimal InPayoutAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int ActivePayoutAccounts { get; set; }
    public int PendingPayoutAccounts { get; set; }
    public int NeedsReviewPayoutAccounts { get; set; }
    public int DraftBatches { get; set; }
    public int ProcessingBatches { get; set; }
    public int SubscriptionPlanCount { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int AutoSettledEarningsCount { get; set; }
    public decimal AutoSettledEarningsAmount { get; set; }
    public List<AdminPayoutAuthorVm> AuthorsNeedingSetup { get; set; } = [];
    public List<AdminPayoutAuthorVm> TopEarningAuthors { get; set; } = [];
    public List<AdminPayoutBatchVm> RecentBatches { get; set; } = [];
}

public class AdminPayoutAuthorVm
{
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "US";
    public PaymentProviderType? PreferredCheckoutProvider { get; set; }
    public AuthorPayoutAccountStatus? PayoutStatus { get; set; }
    public decimal Amount { get; set; }
    public int Items { get; set; }
}

public class AdminPayoutBatchVm
{
    public string BatchReference { get; set; } = string.Empty;
    public PayoutBatchStatus Status { get; set; }
    public string Currency { get; set; } = "usd";
    public decimal TotalAmount { get; set; }
    public int Items { get; set; }
    public DateTime CreatedAt { get; set; }
}
