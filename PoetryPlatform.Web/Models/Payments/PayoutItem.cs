namespace PoetryPlatform.Web.Models.Payments;

public class PayoutItem
{
    public int Id { get; set; }
    public int PayoutBatchId { get; set; }
    public PayoutBatch PayoutBatch { get; set; } = null!;
    public int AuthorEarningId { get; set; }
    public AuthorEarning AuthorEarning { get; set; } = null!;
    public int? AuthorPayoutAccountId { get; set; }
    public AuthorPayoutAccount? AuthorPayoutAccount { get; set; }
    public PayoutItemStatus Status { get; set; } = PayoutItemStatus.Pending;
    public string Currency { get; set; } = "usd";
    public decimal Amount { get; set; }
    public string? ExternalTransferId { get; set; }
    public string? ExternalReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
