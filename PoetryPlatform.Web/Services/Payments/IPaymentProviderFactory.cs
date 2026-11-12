using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetDefaultProvider();
    IPaymentProvider GetByName(string providerName);
    IPaymentProvider GetByType(PaymentProviderType providerType);
}
