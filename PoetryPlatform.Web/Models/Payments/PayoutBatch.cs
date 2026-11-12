namespace PoetryPlatform.Web.Models.Payments;

public class PayoutBatch
{
    public int Id { get; set; }
    public string BatchReference { get; set; } = string.Empty;
    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public PayoutBatchStatus Status { get; set; } = PayoutBatchStatus.Draft;
    public string Currency { get; set; } = "usd";
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public ICollection<PayoutItem> Items { get; set; } = new List<PayoutItem>();
}
