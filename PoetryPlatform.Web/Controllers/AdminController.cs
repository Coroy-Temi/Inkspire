using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;
using PoetryPlatform.Web.Services.Payments;

namespace PoetryPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly PaymentPlatformOptions _paymentOptions;
    private readonly IPaystackCommerceService _paystackCommerceService;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<PaymentPlatformOptions> paymentOptions,
        IPaystackCommerceService paystackCommerceService)
    {
        _userManager = userManager;
        _db = db;
        _paymentOptions = paymentOptions.Value;
        _paystackCommerceService = paystackCommerceService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var authorPoems = await _db.Poems
            .Where(p => p.IsPublished)
            .Include(p => p.Author)
            .Select(p => new
            {
                p.Id,
                p.Views,
                p.AuthorId,
                AuthorName = p.Author!.AuthorName == string.Empty ? p.Author.UserName! : p.Author.AuthorName
            })
            .ToListAsync();

        var poemIds = authorPoems.Select(p => p.Id).ToList();

        var likesByPoem = poemIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.PoemLikes
                .Where(l => poemIds.Contains(l.PoemId))
                .GroupBy(l => l.PoemId)
                .Select(g => new { PoemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PoemId, x => x.Count);

        var commentsByPoem = poemIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.Comments
                .Where(c => poemIds.Contains(c.PoemId) && !c.IsDeleted && !c.IsFlagged)
                .GroupBy(c => c.PoemId)
                .Select(g => new { PoemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PoemId, x => x.Count);

        var model = new AdminDashboardVm
        {
            TotalUsers = await _userManager.Users.CountAsync(),
            PublishedPoems = await _db.Poems.CountAsync(p => p.IsPublished),
            TotalComments = await _db.Comments.CountAsync(c => !c.IsDeleted),
            FlaggedComments = await _db.Comments.CountAsync(c => c.IsFlagged && !c.IsDeleted),
            TotalLikes = await _db.PoemLikes.CountAsync(),
            TotalReads = await _db.PoemViews.CountAsync(),
            PdfListings = await _db.Poems.CountAsync(p => p.IsPublished && p.PdfPath != null),
            PaidListings = await _db.Poems.CountAsync(p => p.IsPublished && p.AccessType == ContentAccessType.PlatformPaid),
            ExternalListings = await _db.Poems.CountAsync(p => p.IsPublished && p.AccessType == ContentAccessType.ExternalLink),
            PlatformCommissionPercent = 12m,
            TopAuthors = authorPoems
                .GroupBy(p => new { p.AuthorId, p.AuthorName })
                .Select(g => new AdminTopAuthorVm
                {
                    AuthorName = g.Key.AuthorName,
                    Poems = g.Count(),
                    Views = g.Sum(p => p.Views),
                    Likes = g.Sum(p => likesByPoem.GetValueOrDefault(p.Id)),
                    Comments = g.Sum(p => commentsByPoem.GetValueOrDefault(p.Id))
                })
                .OrderByDescending(a => a.Views + (a.Likes * 3) + (a.Comments * 2))
                .Take(5)
                .ToList()
        };

        return View(model);
    }

    public IActionResult Users()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    public async Task<IActionResult> Payouts()
    {
        var payoutProvider = ParseProviderType(_paymentOptions.PayoutProvider);
        var earnings = await _db.AuthorEarnings
            .AsNoTracking()
            .Include(e => e.Author)
            .Include(e => e.PayoutItem)
            .ToListAsync();

        var payoutAccounts = await _db.AuthorPayoutAccounts
            .AsNoTracking()
            .Include(account => account.Author)
            .ToListAsync();

        var recentBatches = await _db.PayoutBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.CreatedAt)
            .Take(6)
            .Select(batch => new AdminPayoutBatchVm
            {
                BatchReference = batch.BatchReference,
                Status = batch.Status,
                Currency = batch.Currency,
                TotalAmount = batch.TotalAmount,
                Items = batch.Items.Count,
                CreatedAt = batch.CreatedAt
            })
            .ToListAsync();

        var authorsNeedingSetup = earnings
            .GroupBy(e => e.AuthorId)
            .Select(group =>
            {
                var sample = group.First();
                var payoutAccount = payoutAccounts.FirstOrDefault(account => account.AuthorId == group.Key && account.Provider == payoutProvider);
                return new AdminPayoutAuthorVm
                {
                    AuthorId = group.Key,
                    AuthorName = string.IsNullOrWhiteSpace(sample.Author?.AuthorName) ? sample.Author?.UserName ?? "Author" : sample.Author.AuthorName,
                    CountryCode = sample.Author?.CountryCode ?? "US",
                    PreferredCheckoutProvider = sample.Author?.PreferredCheckoutProvider,
                    PayoutStatus = payoutAccount?.Status,
                    Amount = group.Where(e => e.Status is AuthorEarningStatus.Pending or AuthorEarningStatus.Available).Sum(e => e.NetAmount),
                    Items = group.Count(e => e.Status is AuthorEarningStatus.Pending or AuthorEarningStatus.Available)
                };
            })
            .Where(author => author.Items > 0 && author.PayoutStatus != AuthorPayoutAccountStatus.Active)
            .OrderByDescending(author => author.Amount)
            .Take(8)
            .ToList();

        var topEarningAuthors = earnings
            .GroupBy(e => e.AuthorId)
            .Select(group =>
            {
                var sample = group.First();
                var payoutAccount = payoutAccounts.FirstOrDefault(account => account.AuthorId == group.Key && account.Provider == payoutProvider);
                return new AdminPayoutAuthorVm
                {
                    AuthorId = group.Key,
                    AuthorName = string.IsNullOrWhiteSpace(sample.Author?.AuthorName) ? sample.Author?.UserName ?? "Author" : sample.Author.AuthorName,
                    CountryCode = sample.Author?.CountryCode ?? "US",
                    PreferredCheckoutProvider = sample.Author?.PreferredCheckoutProvider,
                    PayoutStatus = payoutAccount?.Status,
                    Amount = group.Sum(e => e.NetAmount),
                    Items = group.Count()
                };
            })
            .OrderByDescending(author => author.Amount)
            .Take(8)
            .ToList();

        var model = new AdminPayoutDashboardVm
        {
            CheckoutRoutingMode = _paymentOptions.CheckoutRoutingMode,
            DefaultCheckoutProvider = _paymentOptions.DefaultProvider,
            PayoutProvider = _paymentOptions.PayoutProvider,
            PaystackTransfersConfigured = _paystackCommerceService.IsConfigured,
            ReaderSubscriptionsEnabled = _paymentOptions.ReaderSubscriptionsEnabled,
            PendingEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Pending),
            PendingEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Pending).Sum(e => e.NetAmount),
            AvailableEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Available),
            AvailableEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Available).Sum(e => e.NetAmount),
            InPayoutAmount = earnings.Where(e => e.Status == AuthorEarningStatus.InPayout).Sum(e => e.NetAmount),
            PaidAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Paid).Sum(e => e.NetAmount),
            AutoSettledEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Paid && e.PayoutItem == null),
            AutoSettledEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Paid && e.PayoutItem == null).Sum(e => e.NetAmount),
            ActivePayoutAccounts = payoutAccounts.Count(account => account.Status == AuthorPayoutAccountStatus.Active),
            PendingPayoutAccounts = payoutAccounts.Count(account => account.Status == AuthorPayoutAccountStatus.Pending || account.Status == AuthorPayoutAccountStatus.NotStarted),
            NeedsReviewPayoutAccounts = payoutAccounts.Count(account => account.Status == AuthorPayoutAccountStatus.NeedsReview),
            DraftBatches = await _db.PayoutBatches.CountAsync(batch => batch.Status == PayoutBatchStatus.Draft),
            ProcessingBatches = await _db.PayoutBatches.CountAsync(batch => batch.Status == PayoutBatchStatus.Processing),
            SubscriptionPlanCount = await _db.ReaderSubscriptionPlans.CountAsync(),
            ActiveSubscriptions = await _db.ReaderSubscriptions.CountAsync(subscription => subscription.Status == ReaderSubscriptionStatus.Active),
            AuthorsNeedingSetup = authorsNeedingSetup,
            TopEarningAuthors = topEarningAuthors,
            RecentBatches = recentBatches
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunAvailablePayouts(CancellationToken cancellationToken)
    {
        var payoutProvider = ParseProviderType(_paymentOptions.PayoutProvider);
        if (payoutProvider != PaymentProviderType.Paystack)
        {
            TempData["AdminPayoutStatusMessage"] = "Only the Paystack payout rail is active right now.";
            return RedirectToAction(nameof(Payouts));
        }

        if (!_paystackCommerceService.IsConfigured)
        {
            TempData["AdminPayoutStatusMessage"] = "Paystack transfers are not configured yet. Add your Paystack secret key before running payout batches.";
            return RedirectToAction(nameof(Payouts));
        }

        var earnings = await _db.AuthorEarnings
            .Include(e => e.AuthorPayoutAccount)
            .Where(e => e.Status == AuthorEarningStatus.Available && e.PayoutProvider == PaymentProviderType.Paystack)
            .OrderBy(e => e.EarnedAt)
            .ToListAsync(cancellationToken);

        if (earnings.Count == 0)
        {
            TempData["AdminPayoutStatusMessage"] = "There are no available author earnings waiting for a transfer batch.";
            return RedirectToAction(nameof(Payouts));
        }

        var createdBatches = 0;
        foreach (var currencyGroup in earnings.GroupBy(e => e.Currency.ToLowerInvariant()))
        {
            var readyEarnings = currencyGroup
                .Where(e => e.AuthorPayoutAccount?.Status == AuthorPayoutAccountStatus.Active
                    && !string.IsNullOrWhiteSpace(e.AuthorPayoutAccount.PaystackRecipientCode))
                .ToList();

            if (readyEarnings.Count == 0)
            {
                continue;
            }

            var batch = new PayoutBatch
            {
                BatchReference = CreateBatchReference(),
                Provider = PaymentProviderType.Paystack,
                Status = PayoutBatchStatus.Draft,
                Currency = currencyGroup.Key,
                TotalAmount = readyEarnings.Sum(e => e.NetAmount),
                Notes = "Automated Paystack author payout batch",
                CreatedAt = DateTime.UtcNow
            };

            foreach (var earning in readyEarnings)
            {
                var payoutItem = new PayoutItem
                {
                    AuthorEarning = earning,
                    AuthorPayoutAccountId = earning.AuthorPayoutAccountId,
                    AuthorPayoutAccount = earning.AuthorPayoutAccount,
                    Status = PayoutItemStatus.Pending,
                    Currency = earning.Currency,
                    Amount = earning.NetAmount,
                    ExternalReference = CreateTransferReference(earning)
                };

                batch.Items.Add(payoutItem);
                earning.Status = AuthorEarningStatus.InPayout;
            }

            _db.PayoutBatches.Add(batch);
            await _db.SaveChangesAsync(cancellationToken);

            var result = await _paystackCommerceService.SubmitAvailablePayoutBatchAsync(
                batch,
                batch.Items.ToList(),
                cancellationToken);

            if (!result.Succeeded)
            {
                batch.Status = PayoutBatchStatus.Failed;
                batch.ProcessedAt = DateTime.UtcNow;

                foreach (var item in batch.Items)
                {
                    item.Status = PayoutItemStatus.Failed;
                    item.FailureReason = result.ErrorMessage;
                    item.AuthorEarning.Status = AuthorEarningStatus.Available;
                }
            }
            else
            {
                batch.Status = PayoutBatchStatus.Processing;

                foreach (var item in batch.Items)
                {
                    if (!result.Transfers.TryGetValue(item.ExternalReference ?? string.Empty, out var submission))
                    {
                        item.Status = PayoutItemStatus.Failed;
                        item.FailureReason = "Paystack did not return this payout item in the transfer batch response.";
                        item.AuthorEarning.Status = AuthorEarningStatus.Available;
                        continue;
                    }

                    item.ExternalTransferId = submission.TransferCode;
                    item.FailureReason = submission.FailureReason;

                    if (string.Equals(submission.Status, "failed", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Status = PayoutItemStatus.Failed;
                        item.AuthorEarning.Status = AuthorEarningStatus.Available;
                    }
                    else
                    {
                        item.Status = PayoutItemStatus.Submitted;
                        item.AuthorEarning.Status = AuthorEarningStatus.InPayout;
                    }
                }
            }

            createdBatches += 1;
            await _db.SaveChangesAsync(cancellationToken);
        }

        TempData["AdminPayoutStatusMessage"] = createdBatches == 0
            ? "No payout batch could be created because no available earnings had an active Paystack recipient profile."
            : $"{createdBatches} Paystack payout batch{(createdBatches == 1 ? string.Empty : "es")} submitted.";
        return RedirectToAction(nameof(Payouts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshPayoutTransfers(CancellationToken cancellationToken)
    {
        var submittedItems = await _db.PayoutItems
            .Include(item => item.AuthorEarning)
            .Include(item => item.PayoutBatch)
                .ThenInclude(batch => batch.Items)
            .Where(item => item.Status == PayoutItemStatus.Submitted && item.ExternalTransferId != null)
            .ToListAsync(cancellationToken);

        if (submittedItems.Count == 0)
        {
            TempData["AdminPayoutStatusMessage"] = "There are no submitted Paystack transfers to refresh right now.";
            return RedirectToAction(nameof(Payouts));
        }

        foreach (var item in submittedItems)
        {
            var transferResult = await _paystackCommerceService.FetchTransferAsync(item.ExternalTransferId!, cancellationToken);
            if (!transferResult.Succeeded)
            {
                item.FailureReason = transferResult.ErrorMessage;
                continue;
            }

            item.ExternalReference ??= transferResult.Reference;
            item.FailureReason = transferResult.FailureReason;

            if (transferResult.TransferredAt.HasValue)
            {
                item.Status = PayoutItemStatus.Paid;
                item.PaidAt ??= transferResult.TransferredAt.Value;
                item.AuthorEarning.Status = AuthorEarningStatus.Paid;
                item.AuthorEarning.PaidAt ??= item.PaidAt;
                item.AuthorEarning.ExternalSettlementReference ??= item.ExternalTransferId ?? item.ExternalReference;
            }
            else if (string.Equals(transferResult.Status, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(transferResult.Status, "reversed", StringComparison.OrdinalIgnoreCase))
            {
                item.Status = PayoutItemStatus.Failed;
                item.AuthorEarning.Status = AuthorEarningStatus.Available;
                item.AuthorEarning.PaidAt = null;
            }

            UpdateBatchStatus(item.PayoutBatch);
        }

        await _db.SaveChangesAsync(cancellationToken);

        TempData["AdminPayoutStatusMessage"] = "Paystack payout transfer statuses have been refreshed.";
        return RedirectToAction(nameof(Payouts));
    }

    private static PaymentProviderType ParseProviderType(string? providerName)
    {
        return Enum.TryParse<PaymentProviderType>(providerName, true, out var parsed)
            ? parsed
            : PaymentProviderType.Paystack;
    }

    private static string CreateBatchReference()
    {
        return $"BAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25];
    }

    private static string CreateTransferReference(AuthorEarning earning)
    {
        return $"pay_{earning.BookOrderId}_{Guid.NewGuid():N}"[..30].ToLowerInvariant();
    }

    private static void UpdateBatchStatus(PayoutBatch payoutBatch)
    {
        if (payoutBatch.Items.Count == 0)
        {
            payoutBatch.Status = PayoutBatchStatus.Draft;
            return;
        }

        if (payoutBatch.Items.All(item => item.Status == PayoutItemStatus.Paid))
        {
            payoutBatch.Status = PayoutBatchStatus.Paid;
            payoutBatch.ProcessedAt ??= DateTime.UtcNow;
            return;
        }

        if (payoutBatch.Items.Any(item => item.Status == PayoutItemStatus.Failed))
        {
            payoutBatch.Status = payoutBatch.Items.Any(item => item.Status == PayoutItemStatus.Submitted)
                ? PayoutBatchStatus.Processing
                : PayoutBatchStatus.Failed;
            return;
        }

        payoutBatch.Status = PayoutBatchStatus.Processing;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.LockoutEnd = null;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> FlaggedComments()
    {
        var comments = await _db.Comments
            .Include(c => c.Poem)
            .Where(c => c.IsFlagged && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(comments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _db.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        comment.IsDeleted = true;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FlaggedComments));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreComment(int id)
    {
        var comment = await _db.Comments.FindAsync(id);

        if (comment == null)
            return NotFound();

        comment.IsDeleted = false;
        comment.IsFlagged = false;
        comment.FlagCount = 0;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FlaggedComments));
    }
}
