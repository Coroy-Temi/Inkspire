using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Models.Payments;

public class AuthorEarning
{
    public int Id { get; set; }
    public int BookOrderId { get; set; }
    public BookOrder BookOrder { get; set; } = null!;
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }
    public PaymentProviderType PayoutProvider { get; set; } = PaymentProviderType.Paystack;
    public int? AuthorPayoutAccountId { get; set; }
    public AuthorPayoutAccount? AuthorPayoutAccount { get; set; }
    public AuthorEarningStatus Status { get; set; } = AuthorEarningStatus.Pending;
    public string Currency { get; set; } = "usd";
    public decimal GrossAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AvailableAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ExternalSettlementReference { get; set; }
    public PayoutItem? PayoutItem { get; set; }
}
