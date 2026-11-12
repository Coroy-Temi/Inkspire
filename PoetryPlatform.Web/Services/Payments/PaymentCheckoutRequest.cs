namespace PoetryPlatform.Web.Services.Payments;

public class PaymentCheckoutRequest
{
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string BuyerId { get; init; } = string.Empty;
    public string? BuyerEmail { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string? ItemDescription { get; init; }
    public decimal Amount { get; init; }
    public decimal PlatformFeeAmount { get; init; }
    public string Currency { get; init; } = "usd";
    public string? RecipientSubaccountCode { get; init; }
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}
