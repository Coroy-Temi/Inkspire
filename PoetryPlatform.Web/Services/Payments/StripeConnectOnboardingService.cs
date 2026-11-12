using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;
using Stripe;

namespace PoetryPlatform.Web.Services.Payments;

public class StripeConnectOnboardingService(IOptions<StripePaymentOptions> options) : IStripeConnectOnboardingService
{
    private readonly StripePaymentOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<StripeConnectOnboardingResult> StartOnboardingAsync(
        ApplicationUser author,
        AuthorPayoutAccount? existingAccount,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new StripeConnectOnboardingResult
            {
                ErrorMessage = "Stripe Connect is not configured yet."
            };
        }

        StripeConfiguration.ApiKey = _options.SecretKey;
        var payoutAccount = existingAccount ?? new AuthorPayoutAccount
        {
            AuthorId = author.Id,
            Provider = PaymentProviderType.Stripe,
            CountryCode = NormalizeCountryCode(author.CountryCode)
        };

        try
        {
            var accountService = new AccountService();
            Account stripeAccount;

            if (string.IsNullOrWhiteSpace(payoutAccount.StripeAccountId))
            {
                stripeAccount = await accountService.CreateAsync(
                    new AccountCreateOptions
                    {
                        Type = "express",
                        Country = NormalizeCountryCode(author.CountryCode),
                        Email = author.Email,
                        BusinessType = "individual",
                        Metadata = new Dictionary<string, string>
                        {
                            ["author_id"] = author.Id,
                            ["author_name"] = string.IsNullOrWhiteSpace(author.AuthorName) ? author.UserName ?? author.Id : author.AuthorName
                        },
                        Capabilities = new AccountCapabilitiesOptions
                        {
                            CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                            Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                        }
                    },
                    cancellationToken: cancellationToken);

                payoutAccount.StripeAccountId = stripeAccount.Id;
            }
            else
            {
                stripeAccount = await accountService.GetAsync(
                    payoutAccount.StripeAccountId,
                    cancellationToken: cancellationToken);
            }

            var accountLinkService = new AccountLinkService();
            var accountLink = await accountLinkService.CreateAsync(
                new AccountLinkCreateOptions
                {
                    Account = payoutAccount.StripeAccountId,
                    RefreshUrl = refreshUrl,
                    ReturnUrl = returnUrl,
                    Type = "account_onboarding"
                },
                cancellationToken: cancellationToken);

            UpdateLocalStatus(payoutAccount, stripeAccount);
            payoutAccount.Status = payoutAccount.PayoutsEnabled && payoutAccount.DetailsSubmitted
                ? AuthorPayoutAccountStatus.Active
                : AuthorPayoutAccountStatus.Pending;
            payoutAccount.LastOnboardingUrl = accountLink.Url;
            payoutAccount.LastOnboardingAt = DateTime.UtcNow;
            payoutAccount.UpdatedAt = DateTime.UtcNow;

            return new StripeConnectOnboardingResult
            {
                Succeeded = true,
                OnboardingUrl = accountLink.Url,
                Account = payoutAccount
            };
        }
        catch (StripeException ex)
        {
            return new StripeConnectOnboardingResult
            {
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                Account = payoutAccount
            };
        }
    }

    public async Task<AuthorPayoutAccount?> RefreshAccountAsync(
        AuthorPayoutAccount? payoutAccount,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || payoutAccount == null || string.IsNullOrWhiteSpace(payoutAccount.StripeAccountId))
        {
            return payoutAccount;
        }

        StripeConfiguration.ApiKey = _options.SecretKey;

        var accountService = new AccountService();
        var stripeAccount = await accountService.GetAsync(
            payoutAccount.StripeAccountId,
            cancellationToken: cancellationToken);

        UpdateLocalStatus(payoutAccount, stripeAccount);
        payoutAccount.UpdatedAt = DateTime.UtcNow;

        return payoutAccount;
    }

    private static void UpdateLocalStatus(AuthorPayoutAccount payoutAccount, Account stripeAccount)
    {
        payoutAccount.ChargesEnabled = stripeAccount.ChargesEnabled;
        payoutAccount.PayoutsEnabled = stripeAccount.PayoutsEnabled;
        payoutAccount.DetailsSubmitted = stripeAccount.DetailsSubmitted;

        var requirements = stripeAccount.Requirements?.CurrentlyDue ?? [];
        payoutAccount.RequirementsSummary = requirements.Count == 0
            ? null
            : string.Join(", ", requirements.Take(4));

        payoutAccount.Status =
            payoutAccount.PayoutsEnabled && payoutAccount.DetailsSubmitted
                ? AuthorPayoutAccountStatus.Active
                : requirements.Count > 0
                    ? AuthorPayoutAccountStatus.NeedsReview
                    : AuthorPayoutAccountStatus.Pending;
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        return string.IsNullOrWhiteSpace(countryCode)
            ? "US"
            : countryCode.Trim().ToUpperInvariant();
    }
}
