using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaymentFeeCalculator(IOptions<PaymentPlatformOptions> options) : IPaymentFeeCalculator
{
    public PaymentFeeBreakdown Calculate(decimal grossAmount)
    {
        var safeGross = grossAmount < 0 ? 0 : grossAmount;
        var commissionPercent = Math.Clamp(options.Value.PlatformCommissionPercent, 0m, 100m);
        var platformFee = Math.Round(
            safeGross * (commissionPercent / 100m),
            2,
            MidpointRounding.AwayFromZero);

        return new PaymentFeeBreakdown(
            safeGross,
            platformFee,
            Math.Round(safeGross - platformFee, 2, MidpointRounding.AwayFromZero));
    }
}
