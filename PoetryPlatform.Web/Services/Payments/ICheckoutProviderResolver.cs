using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public interface ICheckoutProviderResolver
{
    PaymentProviderType DetermineCheckoutProviderType(ApplicationUser? author);
    IPaymentProvider ResolveCheckoutProvider(ApplicationUser? author);
}
