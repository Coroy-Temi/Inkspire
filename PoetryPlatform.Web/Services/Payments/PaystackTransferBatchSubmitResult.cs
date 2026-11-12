namespace PoetryPlatform.Web.Services.Payments;

public class PaystackTransferBatchSubmitResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, PaystackTransferSubmission> Transfers { get; init; } =
        new Dictionary<string, PaystackTransferSubmission>();
}

public class PaystackTransferSubmission
{
    public string Reference { get; init; } = string.Empty;
    public string? TransferCode { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
}
