namespace PoetryPlatform.Web.Models.Payments;

public class PurchaseReceiptVm
{
    public BookOrder Order { get; set; } = null!;
    public bool CanAccessNow { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}
