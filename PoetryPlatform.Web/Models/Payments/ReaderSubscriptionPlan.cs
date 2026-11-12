namespace PoetryPlatform.Web.Models.Payments;

public class ReaderSubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Currency { get; set; } = "usd";
    public decimal Price { get; set; }
    public SubscriptionPlanInterval Interval { get; set; } = SubscriptionPlanInterval.Monthly;
    public bool IsActive { get; set; } = true;
    public int MonthlyReadingCredits { get; set; }
    public bool IncludesPremiumLibrary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReaderSubscription> Subscriptions { get; set; } = new List<ReaderSubscription>();
}
