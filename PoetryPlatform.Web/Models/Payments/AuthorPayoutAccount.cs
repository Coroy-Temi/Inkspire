using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Models.Payments;

public class AuthorPayoutAccount
{
    public int Id { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }
    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public AuthorPayoutAccountStatus Status { get; set; } = AuthorPayoutAccountStatus.NotStarted;
    public string CountryCode { get; set; } = "US";
    public string Currency { get; set; } = "ngn";
    public string? BusinessName { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? PaystackRecipientCode { get; set; }
    public string? PaystackRecipientType { get; set; }
    public string? PaystackSubaccountCode { get; set; }
    public string? PaystackSubaccountId { get; set; }
    public string? StripeAccountId { get; set; }
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }
    public string? LastOnboardingUrl { get; set; }
    public string? RequirementsSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastOnboardingAt { get; set; }

    public ICollection<AuthorEarning> Earnings { get; set; } = new List<AuthorEarning>();
    public ICollection<PayoutItem> PayoutItems { get; set; } = new List<PayoutItem>();
}
