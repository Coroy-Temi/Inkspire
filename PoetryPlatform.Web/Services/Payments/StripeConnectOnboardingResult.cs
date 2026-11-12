using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class StripeConnectOnboardingResult
{
    public bool Succeeded { get; init; }
    public string? OnboardingUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public AuthorPayoutAccount? Account { get; init; }
}
