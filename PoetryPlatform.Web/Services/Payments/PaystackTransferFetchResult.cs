namespace PoetryPlatform.Web.Services.Payments;

public class PaystackTransferFetchResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string TransferCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public DateTime? TransferredAt { get; init; }
}
