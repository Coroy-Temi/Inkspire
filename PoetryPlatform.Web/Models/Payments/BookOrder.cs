using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Models.Payments;

public class BookOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser? Buyer { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string Currency { get; set; } = "usd";
    public decimal GrossAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal AuthorNetAmount { get; set; }

    public string? ExternalCheckoutSessionId { get; set; }
    public string? ExternalPaymentIntentId { get; set; }
    public string? ExternalChargeId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public ICollection<PaymentRecord> PaymentRecords { get; set; } = new List<PaymentRecord>();
}
