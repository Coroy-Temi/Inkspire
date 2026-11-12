using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public interface IStripeConnectOnboardingService
{
    bool IsConfigured { get; }

    Task<StripeConnectOnboardingResult> StartOnboardingAsync(
        ApplicationUser author,
        AuthorPayoutAccount? existingAccount,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken = default);

    Task<AuthorPayoutAccount?> RefreshAccountAsync(
        AuthorPayoutAccount? payoutAccount,
        CancellationToken cancellationToken = default);
}
