using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaystackAuthorPayoutSetupResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public AuthorPayoutAccount? Account { get; init; }
}
