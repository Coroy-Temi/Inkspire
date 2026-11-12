namespace PoetryPlatform.Web.Models.Payments;

public class PaymentWebhookLog
{
    public int Id { get; set; }

    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Paystack;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? SignatureHeader { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public WebhookLogStatus Status { get; set; } = WebhookLogStatus.Received;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
