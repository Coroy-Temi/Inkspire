using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public interface IPaystackCommerceService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<PaystackBankOption>> ListBanksAsync(
        string countryCode,
        CancellationToken cancellationToken = default);

    Task<PaystackAuthorPayoutSetupResult> SyncAuthorPayoutAccountAsync(
        ApplicationUser author,
        AuthorPayoutAccount? existingAccount,
        string countryCode,
        string businessName,
        string bankCode,
        string accountNumber,
        decimal platformCommissionPercent,
        CancellationToken cancellationToken = default);

    Task<PaystackTransferBatchSubmitResult> SubmitAvailablePayoutBatchAsync(
        PayoutBatch batch,
        IReadOnlyList<PayoutItem> items,
        CancellationToken cancellationToken = default);

    Task<PaystackTransferFetchResult> FetchTransferAsync(
        string transferCode,
        CancellationToken cancellationToken = default);
}
