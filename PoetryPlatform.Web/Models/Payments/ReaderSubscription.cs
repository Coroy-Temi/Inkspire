using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Models.Payments;

public class ReaderSubscription
{
    public int Id { get; set; }
    public string ReaderId { get; set; } = string.Empty;
    public ApplicationUser? Reader { get; set; }
    public int ReaderSubscriptionPlanId { get; set; }
    public ReaderSubscriptionPlan ReaderSubscriptionPlan { get; set; } = null!;
    public ReaderSubscriptionStatus Status { get; set; } = ReaderSubscriptionStatus.Draft;
    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public string Currency { get; set; } = "usd";
    public decimal Price { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
}
