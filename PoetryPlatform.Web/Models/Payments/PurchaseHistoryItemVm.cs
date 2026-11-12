namespace PoetryPlatform.Web.Models.Payments;

public class PurchaseHistoryItemVm
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int PoemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorProfileImagePath { get; set; }
    public OrderStatus Status { get; set; }
    public string Currency { get; set; } = "usd";
    public decimal GrossAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
