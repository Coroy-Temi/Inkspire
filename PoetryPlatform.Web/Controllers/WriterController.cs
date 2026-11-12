using System.Security.Claims;
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

[Authorize]
public class WriterController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<PaymentPlatformOptions> paymentOptions,
    IPaymentProviderFactory paymentProviderFactory,
    ICheckoutProviderResolver checkoutProviderResolver,
    IPaystackCommerceService paystackCommerceService,
    IStripeConnectOnboardingService stripeConnectOnboardingService) : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    public async Task<IActionResult> Poems()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poems = await db.Poems
            .Where(p => p.AuthorId == userId)
            .Select(p => new WriterPoemVm
            {
                Id = p.Id,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                Views = p.Views,
                Likes = db.PoemLikes.Count(l => l.PoemId == p.Id),
                Comments = db.Comments.Count(c => c.PoemId == p.Id)
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(poems);
    }

    public async Task<IActionResult> Payouts(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var payoutProvider = ParseProviderType(paymentOptions.Value.PayoutProvider);
        var payoutAccount = await db.AuthorPayoutAccounts
            .AsTracking()
            .FirstOrDefaultAsync(
                account => account.AuthorId == user.Id && account.Provider == payoutProvider,
                cancellationToken);

        if (payoutProvider == PaymentProviderType.Stripe
            && payoutAccount != null
            && !string.IsNullOrWhiteSpace(payoutAccount.StripeAccountId)
            && stripeConnectOnboardingService.IsConfigured)
        {
            await stripeConnectOnboardingService.RefreshAccountAsync(payoutAccount, cancellationToken);
            await PromotePendingEarningsAsync(user.Id, payoutAccount, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var supportedBanks = payoutProvider == PaymentProviderType.Paystack
            ? await paystackCommerceService.ListBanksAsync(user.CountryCode, cancellationToken)
            : [];

        var earnings = await db.AuthorEarnings
            .AsNoTracking()
            .Where(e => e.AuthorId == user.Id)
            .Include(e => e.BookOrder)
                .ThenInclude(order => order.Poem)
            .OrderByDescending(e => e.EarnedAt)
            .ToListAsync(cancellationToken);

        var model = new AuthorPayoutDashboardVm
        {
            DisplayName = string.IsNullOrWhiteSpace(user.AuthorName) ? user.UserName ?? "Author" : user.AuthorName,
            Email = user.Email ?? string.Empty,
            CountryCode = user.CountryCode,
            Currency = payoutAccount?.Currency ?? ResolveCurrency(user.CountryCode),
            PreferredCheckoutProvider = user.PreferredCheckoutProvider,
            EffectiveCheckoutProvider = checkoutProviderResolver.ResolveCheckoutProvider(user).ProviderType,
            CheckoutRoutingMode = paymentOptions.Value.CheckoutRoutingMode,
            PayoutProvider = payoutProvider,
            StripeCheckoutConfigured = paymentProviderFactory.GetByType(PaymentProviderType.Stripe).IsConfigured,
            PaystackCheckoutConfigured = paymentProviderFactory.GetByType(PaymentProviderType.Paystack).IsConfigured,
            PaystackTransfersConfigured = paystackCommerceService.IsConfigured,
            StripePayoutConfigured = stripeConnectOnboardingService.IsConfigured,
            ReaderSubscriptionsEnabled = paymentOptions.Value.ReaderSubscriptionsEnabled,
            PayoutAccount = payoutAccount,
            SupportedBanks = supportedBanks.ToList(),
            PendingEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Pending),
            PendingEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Pending).Sum(e => e.NetAmount),
            AvailableEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Available),
            AvailableEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Available).Sum(e => e.NetAmount),
            PaidEarningsCount = earnings.Count(e => e.Status == AuthorEarningStatus.Paid),
            PaidEarningsAmount = earnings.Where(e => e.Status == AuthorEarningStatus.Paid).Sum(e => e.NetAmount),
            SubscriptionPlanCount = await db.ReaderSubscriptionPlans.CountAsync(cancellationToken),
            ActiveReaderSubscriptions = await db.ReaderSubscriptions.CountAsync(
                s => s.Status == ReaderSubscriptionStatus.Active,
                cancellationToken),
            RecentEarnings = earnings
                .Take(8)
                .Select(e => new AuthorPayoutEarningVm
                {
                    OrderNumber = e.BookOrder.OrderNumber,
                    Title = e.BookOrder.Poem.Title,
                    Currency = e.Currency,
                    GrossAmount = e.GrossAmount,
                    NetAmount = e.NetAmount,
                    Status = e.Status,
                    EarnedAt = e.EarnedAt,
                    PaidAt = e.PaidAt
                })
                .ToList()
        };

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new Poem());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Poem poem, IFormFile? pdfFile)
    {
        NormalizeMonetization(poem);

        if (!ValidateMonetization(poem, pdfFile, null))
        {
            return View(poem);
        }

        if (!ModelState.IsValid)
        {
            return View(poem);
        }

        poem.AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        poem.CreatedAt = DateTime.UtcNow;
        poem.IsPublished = true;

        if (pdfFile != null && pdfFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/pdfs");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await pdfFile.CopyToAsync(stream);

            poem.PdfPath = "/uploads/pdfs/" + fileName;
        }

        db.Poems.Add(poem);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePayoutSettings(
        string countryCode,
        PaymentProviderType? preferredCheckoutProvider,
        string? businessName,
        string? bankCode,
        string? accountNumber,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        user.CountryCode = NormalizeCountryCode(countryCode);
        user.PreferredCheckoutProvider = preferredCheckoutProvider;

        var payoutProvider = ParseProviderType(paymentOptions.Value.PayoutProvider);
        var payoutAccount = await db.AuthorPayoutAccounts
            .FirstOrDefaultAsync(
                account => account.AuthorId == user.Id && account.Provider == payoutProvider,
                cancellationToken);

        if (payoutProvider == PaymentProviderType.Paystack)
        {
            if (string.IsNullOrWhiteSpace(bankCode) || string.IsNullOrWhiteSpace(accountNumber))
            {
                TempData["WriterPayoutStatusMessage"] = "Choose a settlement bank and account number so Inkspire can connect your Paystack payout profile.";
                return RedirectToAction(nameof(Payouts));
            }

            var setupResult = await paystackCommerceService.SyncAuthorPayoutAccountAsync(
                user,
                payoutAccount,
                user.CountryCode,
                businessName ?? string.Empty,
                bankCode,
                accountNumber,
                paymentOptions.Value.PlatformCommissionPercent,
                cancellationToken);

            if (!setupResult.Succeeded || setupResult.Account == null)
            {
                TempData["WriterPayoutStatusMessage"] = setupResult.ErrorMessage ?? "Inkspire could not sync your Paystack payout details right now.";
                return RedirectToAction(nameof(Payouts));
            }

            if (payoutAccount == null)
            {
                payoutAccount = setupResult.Account;
                db.AuthorPayoutAccounts.Add(payoutAccount);
            }
            else
            {
                payoutAccount.CountryCode = setupResult.Account.CountryCode;
                payoutAccount.Currency = setupResult.Account.Currency;
                payoutAccount.BusinessName = setupResult.Account.BusinessName;
                payoutAccount.BankCode = setupResult.Account.BankCode;
                payoutAccount.BankName = setupResult.Account.BankName;
                payoutAccount.AccountNumber = setupResult.Account.AccountNumber;
                payoutAccount.AccountName = setupResult.Account.AccountName;
                payoutAccount.PaystackRecipientCode = setupResult.Account.PaystackRecipientCode;
                payoutAccount.PaystackRecipientType = setupResult.Account.PaystackRecipientType;
                payoutAccount.PaystackSubaccountCode = setupResult.Account.PaystackSubaccountCode;
                payoutAccount.PaystackSubaccountId = setupResult.Account.PaystackSubaccountId;
                payoutAccount.Status = setupResult.Account.Status;
                payoutAccount.ChargesEnabled = setupResult.Account.ChargesEnabled;
                payoutAccount.PayoutsEnabled = setupResult.Account.PayoutsEnabled;
                payoutAccount.DetailsSubmitted = setupResult.Account.DetailsSubmitted;
                payoutAccount.RequirementsSummary = setupResult.Account.RequirementsSummary;
                payoutAccount.UpdatedAt = DateTime.UtcNow;
            }

            await PromotePendingEarningsAsync(user.Id, payoutAccount, cancellationToken);
        }
        else if (payoutAccount != null && string.IsNullOrWhiteSpace(payoutAccount.StripeAccountId))
        {
            payoutAccount.CountryCode = user.CountryCode;
            payoutAccount.UpdatedAt = DateTime.UtcNow;
        }

        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(cancellationToken);

        TempData["WriterPayoutStatusMessage"] = "Your payout settings have been updated.";
        return RedirectToAction(nameof(Payouts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartPayoutOnboarding(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!stripeConnectOnboardingService.IsConfigured)
        {
            TempData["WriterPayoutStatusMessage"] = "Stripe is parked for later. Paystack is the active payment path on Inkspire right now.";
            return RedirectToAction(nameof(Payouts));
        }

        var payoutProvider = ParseProviderType(paymentOptions.Value.PayoutProvider);
        if (payoutProvider != PaymentProviderType.Stripe)
        {
            TempData["WriterPayoutStatusMessage"] = "Paystack is the active commerce flow right now, so Stripe onboarding is parked until you choose to revive it later.";
            return RedirectToAction(nameof(Payouts));
        }

        var payoutAccount = await db.AuthorPayoutAccounts
            .FirstOrDefaultAsync(
                account => account.AuthorId == user.Id && account.Provider == payoutProvider,
                cancellationToken);

        var payoutsUrl = Url.Action(nameof(Payouts), "Writer", values: null, Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build the author payout URL.");

        var onboardingResult = await stripeConnectOnboardingService.StartOnboardingAsync(
            user,
            payoutAccount,
            payoutsUrl,
            payoutsUrl,
            cancellationToken);

        if (onboardingResult.Account != null && payoutAccount == null)
        {
            db.AuthorPayoutAccounts.Add(onboardingResult.Account);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!onboardingResult.Succeeded || string.IsNullOrWhiteSpace(onboardingResult.OnboardingUrl))
        {
            TempData["WriterPayoutStatusMessage"] = onboardingResult.ErrorMessage ?? "Inkspire could not start Stripe Connect onboarding right now.";
            return RedirectToAction(nameof(Payouts));
        }

        return Redirect(onboardingResult.OnboardingUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshPayoutStatus(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var payoutProvider = ParseProviderType(paymentOptions.Value.PayoutProvider);
        var payoutAccount = await db.AuthorPayoutAccounts
            .FirstOrDefaultAsync(
                account => account.AuthorId == user.Id && account.Provider == payoutProvider,
                cancellationToken);

        if (payoutAccount == null)
        {
            TempData["WriterPayoutStatusMessage"] = "Save your Paystack payment settings first so Inkspire can treat your payout profile as ready.";
            return RedirectToAction(nameof(Payouts));
        }

        if (payoutProvider != PaymentProviderType.Stripe)
        {
            if (string.IsNullOrWhiteSpace(payoutAccount.BankCode) || string.IsNullOrWhiteSpace(payoutAccount.AccountNumber))
            {
                TempData["WriterPayoutStatusMessage"] = "Your Paystack payout profile still needs a bank and account number before it can be refreshed.";
                return RedirectToAction(nameof(Payouts));
            }

            var refreshResult = await paystackCommerceService.SyncAuthorPayoutAccountAsync(
                user,
                payoutAccount,
                payoutAccount.CountryCode,
                payoutAccount.BusinessName ?? user.AuthorName,
                payoutAccount.BankCode,
                payoutAccount.AccountNumber,
                paymentOptions.Value.PlatformCommissionPercent,
                cancellationToken);

            if (!refreshResult.Succeeded || refreshResult.Account == null)
            {
                TempData["WriterPayoutStatusMessage"] = refreshResult.ErrorMessage ?? "Inkspire could not refresh your Paystack payout profile right now.";
                return RedirectToAction(nameof(Payouts));
            }

            payoutAccount.Currency = refreshResult.Account.Currency;
            payoutAccount.BusinessName = refreshResult.Account.BusinessName;
            payoutAccount.BankName = refreshResult.Account.BankName;
            payoutAccount.AccountName = refreshResult.Account.AccountName;
            payoutAccount.PaystackRecipientCode = refreshResult.Account.PaystackRecipientCode;
            payoutAccount.PaystackRecipientType = refreshResult.Account.PaystackRecipientType;
            payoutAccount.PaystackSubaccountCode = refreshResult.Account.PaystackSubaccountCode;
            payoutAccount.PaystackSubaccountId = refreshResult.Account.PaystackSubaccountId;
            payoutAccount.Status = refreshResult.Account.Status;
            payoutAccount.ChargesEnabled = refreshResult.Account.ChargesEnabled;
            payoutAccount.PayoutsEnabled = refreshResult.Account.PayoutsEnabled;
            payoutAccount.DetailsSubmitted = refreshResult.Account.DetailsSubmitted;
            payoutAccount.RequirementsSummary = refreshResult.Account.RequirementsSummary;
            payoutAccount.UpdatedAt = DateTime.UtcNow;
            await PromotePendingEarningsAsync(user.Id, payoutAccount, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            TempData["WriterPayoutStatusMessage"] = "Your Paystack recipient and subaccount details have been refreshed.";
            return RedirectToAction(nameof(Payouts));
        }

        await stripeConnectOnboardingService.RefreshAccountAsync(payoutAccount, cancellationToken);
        await PromotePendingEarningsAsync(user.Id, payoutAccount, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        TempData["WriterPayoutStatusMessage"] = "Your payout account status has been refreshed.";
        return RedirectToAction(nameof(Payouts));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == userId);

        if (poem == null)
        {
            return NotFound();
        }

        return View(poem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Poem poem, IFormFile? pdfFile)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var existingPoem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == poem.Id && p.AuthorId == userId);

        if (existingPoem == null)
        {
            return NotFound();
        }

        NormalizeMonetization(poem);

        if (!ValidateMonetization(poem, pdfFile, existingPoem.PdfPath))
        {
            poem.PdfPath = existingPoem.PdfPath;
            return View(poem);
        }

        if (!ModelState.IsValid)
        {
            poem.PdfPath = existingPoem.PdfPath;
            return View(poem);
        }

        existingPoem.Title = poem.Title;
        existingPoem.Content = poem.Content;
        existingPoem.AccessType = poem.AccessType;
        existingPoem.Price = poem.Price;
        existingPoem.ExternalPurchaseUrl = poem.ExternalPurchaseUrl;

        if (pdfFile != null && pdfFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/pdfs");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await pdfFile.CopyToAsync(stream);

            existingPoem.PdfPath = "/uploads/pdfs/" + fileName;
        }

        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var poem = await db.Poems
            .FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == userId);

        if (poem == null)
        {
            return NotFound();
        }

        db.Poems.Remove(poem);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Poems));
    }

    private bool ValidateMonetization(Poem poem, IFormFile? pdfFile, string? existingPdfPath)
    {
        if (poem.AccessType == ContentAccessType.PlatformPaid && poem.Price <= 0)
        {
            ModelState.AddModelError(nameof(Poem.Price), "Set a price greater than 0 for platform-paid books.");
        }

        if (poem.AccessType == ContentAccessType.PlatformPaid
            && pdfFile == null
            && string.IsNullOrWhiteSpace(existingPdfPath))
        {
            ModelState.AddModelError("pdfFile", "Platform-paid books need a full PDF upload alongside the preview text.");
        }

        if (poem.AccessType == ContentAccessType.ExternalLink
            && (string.IsNullOrWhiteSpace(poem.ExternalPurchaseUrl)
                || !Uri.TryCreate(poem.ExternalPurchaseUrl, UriKind.Absolute, out _)))
        {
            ModelState.AddModelError(nameof(Poem.ExternalPurchaseUrl), "Enter a valid external purchase URL.");
        }

        return ModelState.IsValid;
    }

    private static void NormalizeMonetization(Poem poem)
    {
        poem.ExternalPurchaseUrl = string.IsNullOrWhiteSpace(poem.ExternalPurchaseUrl)
            ? null
            : poem.ExternalPurchaseUrl.Trim();

        if (poem.AccessType != ContentAccessType.PlatformPaid)
        {
            poem.Price = 0;
        }

        if (poem.AccessType != ContentAccessType.ExternalLink)
        {
            poem.ExternalPurchaseUrl = null;
        }
    }

    private async Task PromotePendingEarningsAsync(
        string authorId,
        AuthorPayoutAccount? payoutAccount,
        CancellationToken cancellationToken)
    {
        if (payoutAccount?.Status != AuthorPayoutAccountStatus.Active)
        {
            return;
        }

        var pendingEarnings = await db.AuthorEarnings
            .Where(e => e.AuthorId == authorId && e.Status == AuthorEarningStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var earning in pendingEarnings)
        {
            earning.AuthorPayoutAccountId = payoutAccount.Id;
            earning.Status = AuthorEarningStatus.Available;
            earning.AvailableAt ??= DateTime.UtcNow;
        }
    }

    private static PaymentProviderType ParseProviderType(string? providerName)
    {
        return Enum.TryParse<PaymentProviderType>(providerName, true, out var parsed)
            ? parsed
            : PaymentProviderType.Paystack;
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        return string.IsNullOrWhiteSpace(countryCode)
            ? "NG"
            : countryCode.Trim().ToUpperInvariant();
    }

    private static string ResolveCurrency(string? countryCode)
    {
        return countryCode?.Trim().ToUpperInvariant() switch
        {
            "GH" => "ghs",
            "KE" => "kes",
            "ZA" => "zar",
            _ => "ngn"
        };
    }
}
