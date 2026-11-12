namespace PoetryPlatform.Web.Services.Payments;

public interface IPaymentFeeCalculator
{
    PaymentFeeBreakdown Calculate(decimal grossAmount);
}
