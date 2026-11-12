namespace PoetryPlatform.Web.Models.Payments;

public class PaystackBankOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "NGN";
    public string Type { get; set; } = "nuban";
    public string Country { get; set; } = string.Empty;
}
