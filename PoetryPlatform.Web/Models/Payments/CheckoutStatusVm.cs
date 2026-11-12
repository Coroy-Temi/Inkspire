namespace PoetryPlatform.Web.Models.Payments;

public class CheckoutStatusVm
{
    public BookOrder Order { get; set; } = null!;
    public bool CanAccessNow { get; set; }
    public string? StatusMessage { get; set; }
}
