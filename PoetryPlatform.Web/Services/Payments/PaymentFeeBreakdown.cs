namespace PoetryPlatform.Web.Services.Payments;

public readonly record struct PaymentFeeBreakdown(
    decimal GrossAmount,
    decimal PlatformFeeAmount,
    decimal AuthorNetAmount);
