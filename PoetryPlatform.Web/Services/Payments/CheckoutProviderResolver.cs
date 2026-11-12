using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class CheckoutProviderResolver(
    IPaymentProviderFactory paymentProviderFactory,
    IOptions<PaymentPlatformOptions> options) : ICheckoutProviderResolver
{
    private readonly PaymentPlatformOptions _options = options.Value;

    public PaymentProviderType DetermineCheckoutProviderType(ApplicationUser? author)
    {
        var defaultProvider = ParseProviderType(_options.DefaultProvider);
        var normalizedCountry = author?.CountryCode?.Trim().ToUpperInvariant();
        var paystackCountries = _options.PaystackCountryCodes
            .Select(code => code.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _options.CheckoutRoutingMode switch
        {
            CheckoutRoutingMode.AuthorPreference when author?.PreferredCheckoutProvider.HasValue == true
                => author.PreferredCheckoutProvider.Value,
            CheckoutRoutingMode.AuthorCountry when !string.IsNullOrWhiteSpace(normalizedCountry) && paystackCountries.Contains(normalizedCountry)
                => PaymentProviderType.Paystack,
            CheckoutRoutingMode.AuthorPreference when !string.IsNullOrWhiteSpace(normalizedCountry) && paystackCountries.Contains(normalizedCountry)
                => PaymentProviderType.Paystack,
            _ => defaultProvider
        };
    }

    public IPaymentProvider ResolveCheckoutProvider(ApplicationUser? author)
    {
        var preferredType = DetermineCheckoutProviderType(author);
        var fallbackTypes = new[]
        {
            preferredType,
            ParseProviderType(_options.DefaultProvider),
            PaymentProviderType.Paystack
        }.Distinct();

        foreach (var providerType in fallbackTypes)
        {
            var provider = paymentProviderFactory.GetByType(providerType);
            if (provider.IsConfigured)
            {
                return provider;
            }
        }

        return paymentProviderFactory.GetByType(preferredType);
    }

    private static PaymentProviderType ParseProviderType(string? providerName)
    {
        return Enum.TryParse<PaymentProviderType>(providerName, true, out var parsed)
            ? parsed
            : PaymentProviderType.Paystack;
    }
}
