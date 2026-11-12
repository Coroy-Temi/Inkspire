using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaymentProviderFactory(
    IEnumerable<IPaymentProvider> providers,
    IOptions<PaymentPlatformOptions> options) : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<PaymentProviderType, IPaymentProvider> _providers =
        providers.ToDictionary(p => p.ProviderType);

    public IPaymentProvider GetDefaultProvider()
    {
        return GetByName(options.Value.DefaultProvider);
    }

    public IPaymentProvider GetByType(PaymentProviderType providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"Payment provider '{providerType}' is not registered.");
    }

    public IPaymentProvider GetByName(string providerName)
    {
        var providerType = Enum.TryParse<PaymentProviderType>(providerName, true, out var parsed)
            ? parsed
            : PaymentProviderType.Paystack;

        return GetByType(providerType);
    }
}
