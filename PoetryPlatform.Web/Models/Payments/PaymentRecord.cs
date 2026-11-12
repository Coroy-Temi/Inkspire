namespace PoetryPlatform.Web.Models.Payments;

public class PaymentRecord
{
    public int Id { get; set; }

    public int BookOrderId { get; set; }
    public BookOrder BookOrder { get; set; } = null!;

    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public PaymentRecordStatus Status { get; set; } = PaymentRecordStatus.Pending;

    public string ExternalReference { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? RawPayloadJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
