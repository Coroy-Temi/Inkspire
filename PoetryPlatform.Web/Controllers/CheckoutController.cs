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
public class CheckoutController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IPaymentProviderFactory paymentProviderFactory,
    ICheckoutProviderResolver checkoutProviderResolver,
    IPaymentFeeCalculator paymentFeeCalculator,
    IOptions<PaymentPlatformOptions> paymentOptions,
    ILogger<CheckoutController> logger) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int poemId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.GetUserAsync(User);

        if (userId == null || user == null)
        {
            return Challenge();
        }

        var poem = await db.Poems
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == poemId && p.IsPublished, cancellationToken);

        if (poem == null)
        {
            return NotFound();
        }

        if (poem.AccessType != ContentAccessType.PlatformPaid || poem.Price <= 0)
        {
            TempData["CheckoutStatusMessage"] = "This title is not available for platform checkout.";
            return RedirectToAction("Read", "Poems", new { id = poemId });
        }

        if (poem.AuthorId == userId)
        {
            TempData["CheckoutStatusMessage"] = "You already own this title as its author.";
            return RedirectToAction("Read", "Poems", new { id = poemId });
        }

        var provider = checkoutProviderResolver.ResolveCheckoutProvider(poem.Author);
        if (!provider.IsConfigured)
        {
            TempData["CheckoutStatusMessage"] = $"{provider.ProviderType} checkout is not configured yet. Add the provider keys before testing payments.";
            return RedirectToAction("Read", "Poems", new { id = poemId });
        }

        var existingPaidOrder = await db.BookOrders
            .Where(o => o.PoemId == poemId && o.BuyerId == userId && o.Status == OrderStatus.Paid)
            .OrderByDescending(o => o.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPaidOrder != null)
        {
            return RedirectToAction(nameof(Success), new { orderId = existingPaidOrder.Id });
        }

        var feeBreakdown = paymentFeeCalculator.Calculate(poem.Price);
        var configuredCurrency = paymentOptions.Value.DefaultCurrency.Trim().ToLowerInvariant();
        var authorPayoutAccount = provider.ProviderType == PaymentProviderType.Paystack
            ? await db.AuthorPayoutAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    account => account.AuthorId == poem.AuthorId
                        && account.Provider == PaymentProviderType.Paystack
                        && account.Status == AuthorPayoutAccountStatus.Active,
                    cancellationToken)
            : null;
        var order = new BookOrder
        {
            OrderNumber = CreateOrderNumber(),
            PoemId = poem.Id,
            BuyerId = userId,
            AuthorId = poem.AuthorId,
            Provider = provider.ProviderType,
            Status = OrderStatus.Pending,
            Currency = string.IsNullOrWhiteSpace(configuredCurrency) ? "usd" : configuredCurrency,
            GrossAmount = feeBreakdown.GrossAmount,
            PlatformFeeAmount = feeBreakdown.PlatformFeeAmount,
            AuthorNetAmount = feeBreakdown.AuthorNetAmount,
            CreatedAt = DateTime.UtcNow
        };

        db.BookOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var successBaseUrl = Url.Action(nameof(Success), "Checkout", new { orderId = order.Id }, Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build checkout success URL.");
        var cancelUrl = Url.Action(nameof(Cancel), "Checkout", new { orderId = order.Id }, Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build checkout cancel URL.");
        var successUrl = provider.ProviderType == PaymentProviderType.Stripe
            ? $"{successBaseUrl}&session_id={{CHECKOUT_SESSION_ID}}"
            : successBaseUrl;

        var checkoutResult = await provider.CreateCheckoutSessionAsync(
            new PaymentCheckoutRequest
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                BuyerId = userId,
                BuyerEmail = user.Email,
                AuthorId = poem.AuthorId,
                ItemName = poem.Title,
                ItemDescription = $"Digital access to {poem.Title}",
                Amount = order.GrossAmount,
                PlatformFeeAmount = order.PlatformFeeAmount,
                Currency = order.Currency,
                RecipientSubaccountCode = authorPayoutAccount?.PaystackSubaccountCode,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            },
            cancellationToken);

        if (!checkoutResult.Succeeded
            || string.IsNullOrWhiteSpace(checkoutResult.CheckoutUrl)
            || string.IsNullOrWhiteSpace(checkoutResult.ExternalCheckoutSessionId))
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = checkoutResult.ErrorMessage ?? "Unable to create a checkout session.";
            order.UpdatedAt = DateTime.UtcNow;

            db.PaymentRecords.Add(new PaymentRecord
            {
                BookOrderId = order.Id,
                Provider = order.Provider,
                Status = PaymentRecordStatus.Failed,
                ExternalReference = order.OrderNumber,
                ObjectType = "checkout_session",
                Amount = order.GrossAmount,
                Currency = order.Currency,
                RawPayloadJson = checkoutResult.RawPayloadJson
            });

            await db.SaveChangesAsync(cancellationToken);

            TempData["CheckoutStatusMessage"] = order.FailureReason;
            return RedirectToAction("Read", "Poems", new { id = poem.Id });
        }

        order.Status = OrderStatus.CheckoutCreated;
        order.ExternalCheckoutSessionId = checkoutResult.ExternalCheckoutSessionId;
        order.ExternalPaymentIntentId = checkoutResult.ExternalPaymentIntentId;
        order.CheckoutUrl = checkoutResult.CheckoutUrl;
        order.FailureReason = null;
        order.UpdatedAt = DateTime.UtcNow;

        db.PaymentRecords.Add(new PaymentRecord
        {
            BookOrderId = order.Id,
            Provider = order.Provider,
            Status = PaymentRecordStatus.Pending,
            ExternalReference = checkoutResult.ExternalCheckoutSessionId,
            ObjectType = "checkout_session",
            Amount = order.GrossAmount,
            Currency = order.Currency,
            RawPayloadJson = checkoutResult.RawPayloadJson
        });

        await db.SaveChangesAsync(cancellationToken);

        return Redirect(checkoutResult.CheckoutUrl);
    }

    public async Task<IActionResult> Success(
        int orderId,
        string? session_id,
        string? reference,
        string? trxref,
        CancellationToken cancellationToken)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken);
        if (order == null)
        {
            return NotFound();
        }

        string? statusMessage = null;
        var checkoutReference = order.Provider switch
        {
            PaymentProviderType.Stripe => session_id,
            PaymentProviderType.Paystack => reference ?? trxref ?? order.ExternalCheckoutSessionId,
            _ => null
        };

        if (order.Status != OrderStatus.Paid && !string.IsNullOrWhiteSpace(checkoutReference))
        {
            statusMessage = await TryFinalizeOrderFromReturnAsync(order, checkoutReference, cancellationToken);
        }

        return View(BuildCheckoutStatusVm(order, statusMessage));
    }

    public async Task<IActionResult> Cancel(int orderId, CancellationToken cancellationToken)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken);
        if (order == null)
        {
            return NotFound();
        }

        return View(BuildCheckoutStatusVm(order));
    }

    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = new PurchaseHistoryVm
        {
            Orders = await db.BookOrders
                .AsNoTracking()
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new PurchaseHistoryItemVm
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    PoemId = o.PoemId,
                    Title = o.Poem.Title,
                    AuthorName = string.IsNullOrWhiteSpace(o.Poem.Author!.AuthorName) ? o.Poem.Author.UserName! : o.Poem.Author.AuthorName,
                    AuthorProfileImagePath = o.Poem.Author!.ProfileImagePath,
                    Status = o.Status,
                    Currency = o.Currency,
                    GrossAmount = o.GrossAmount,
                    CreatedAt = o.CreatedAt,
                    CompletedAt = o.CompletedAt
                })
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Receipt(int orderId, CancellationToken cancellationToken)
    {
        var order = await GetAccessibleOrderAsync(orderId, cancellationToken);
        if (order == null)
        {
            return NotFound();
        }

        return View(new PurchaseReceiptVm
        {
            Order = order,
            CanAccessNow = order.Status == OrderStatus.Paid || order.AuthorId == User.FindFirstValue(ClaimTypes.NameIdentifier) || User.IsInRole("Admin"),
            BuyerName = string.IsNullOrWhiteSpace(order.Buyer?.AuthorName) ? order.Buyer?.UserName ?? "Reader" : order.Buyer.AuthorName,
            AuthorName = string.IsNullOrWhiteSpace(order.Poem.Author?.AuthorName) ? order.Poem.Author?.UserName ?? "Author" : order.Poem.Author.AuthorName
        });
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost("/checkout/webhooks/stripe")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        return await HandleWebhookAsync(
            PaymentProviderType.Stripe,
            Request.Headers["Stripe-Signature"].ToString(),
            cancellationToken);
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost("/checkout/webhooks/paystack")]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        return await HandleWebhookAsync(
            PaymentProviderType.Paystack,
            Request.Headers["x-paystack-signature"].ToString(),
            cancellationToken);
    }

    private async Task<IActionResult> HandleWebhookAsync(
        PaymentProviderType providerType,
        string signatureHeader,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payloadJson = await reader.ReadToEndAsync(cancellationToken);

        var webhookLog = new PaymentWebhookLog
        {
            Provider = providerType,
            SignatureHeader = signatureHeader,
            PayloadJson = payloadJson,
            Status = WebhookLogStatus.Received,
            ReceivedAt = DateTime.UtcNow
        };

        db.PaymentWebhookLogs.Add(webhookLog);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var provider = paymentProviderFactory.GetByType(providerType);
            var result = provider.ParseWebhook(payloadJson, signatureHeader);

            webhookLog.EventId = result.ExternalEventId;
            webhookLog.EventType = result.EventType;

            if (!result.IsValid)
            {
                webhookLog.Status = WebhookLogStatus.Failed;
                webhookLog.ErrorMessage = result.ErrorMessage;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return BadRequest();
            }

            var duplicateEvent = !string.IsNullOrWhiteSpace(result.ExternalEventId)
                && await db.PaymentWebhookLogs.AnyAsync(
                    log => log.Id != webhookLog.Id
                        && log.Provider == providerType
                        && log.EventId == result.ExternalEventId,
                    cancellationToken);

            if (duplicateEvent)
            {
                webhookLog.Status = WebhookLogStatus.Duplicate;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return Ok();
            }

            if (result.OrderId.HasValue && result.OrderStatus.HasValue && result.PaymentStatus.HasValue)
            {
                var order = await db.BookOrders
                    .Include(o => o.PaymentRecords)
                    .FirstOrDefaultAsync(o => o.Id == result.OrderId.Value, cancellationToken);

                if (order == null)
                {
                    webhookLog.Status = WebhookLogStatus.Failed;
                    webhookLog.ErrorMessage = $"Book order {result.OrderId.Value} was not found.";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return NotFound();
                }

                ApplyPaymentResultToOrder(
                    order,
                    result.OrderStatus.Value,
                    result.PaymentStatus.Value,
                    result.ExternalCheckoutSessionId,
                    result.ExternalPaymentIntentId,
                    result.ExternalChargeId,
                    result.ExternalReference,
                    result.ObjectType,
                    result.Amount,
                    result.Currency,
                    result.ExternalEventId,
                    result.FailureReason,
                    result.RawPayloadJson);

                await SyncAuthorEarningAsync(order, cancellationToken);
            }
            else if (result.PayoutItemStatus.HasValue)
            {
                var payoutItem = await db.PayoutItems
                    .Include(item => item.AuthorEarning)
                    .Include(item => item.PayoutBatch)
                        .ThenInclude(batch => batch.Items)
                    .FirstOrDefaultAsync(
                        item => (!string.IsNullOrWhiteSpace(result.ExternalTransferId) && item.ExternalTransferId == result.ExternalTransferId)
                            || (!string.IsNullOrWhiteSpace(result.ExternalReference) && item.ExternalReference == result.ExternalReference),
                        cancellationToken);

                if (payoutItem == null)
                {
                    webhookLog.Status = WebhookLogStatus.Failed;
                    webhookLog.ErrorMessage = $"Payout item for transfer '{result.ExternalTransferId ?? result.ExternalReference}' was not found.";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return NotFound();
                }

                ApplyPayoutResultToItem(
                    payoutItem,
                    result.PayoutItemStatus.Value,
                    result.ExternalTransferId,
                    result.ExternalReference,
                    result.FailureReason,
                    result.PaidAt);

                UpdateBatchStatus(payoutItem.PayoutBatch);
            }

            webhookLog.Status = WebhookLogStatus.Processed;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Provider} webhook processing failed.", providerType);
            webhookLog.Status = WebhookLogStatus.Failed;
            webhookLog.ErrorMessage = ex.Message;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<BookOrder?> GetAccessibleOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var order = await db.BookOrders
            .Include(o => o.Poem)
                .ThenInclude(p => p.Author)
            .Include(o => o.Buyer)
            .Include(o => o.PaymentRecords)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return null;
        }

        if (!isAdmin && order.BuyerId != userId && order.AuthorId != userId)
        {
            return null;
        }

        return order;
    }

    private async Task<string?> TryFinalizeOrderFromReturnAsync(
        BookOrder order,
        string checkoutReference,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(order.ExternalCheckoutSessionId)
            && order.Provider == PaymentProviderType.Stripe
            && !string.Equals(order.ExternalCheckoutSessionId, checkoutReference, StringComparison.Ordinal))
        {
            return "The returned checkout session did not match this order.";
        }

        var provider = paymentProviderFactory.GetByType(order.Provider);
        var result = await provider.GetCheckoutStatusAsync(checkoutReference, cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorMessage ?? $"Inkspire could not verify the returned {order.Provider} checkout yet.";
        }

        if (result.OrderId.HasValue && result.OrderId.Value != order.Id)
        {
            return $"{order.Provider} returned a different order than the one requested.";
        }

        ApplyPaymentResultToOrder(
            order,
            result.OrderStatus,
            result.PaymentStatus,
            result.ExternalCheckoutSessionId,
            result.ExternalPaymentIntentId,
            result.ExternalChargeId,
            result.ExternalReference,
            result.ObjectType,
            result.Amount,
            result.Currency,
            externalEventId: null,
            result.FailureReason,
            result.RawPayloadJson);

        await SyncAuthorEarningAsync(order, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return order.Status == OrderStatus.Paid
            ? $"Payment confirmed directly with {order.Provider}."
            : $"{order.Provider} returned you successfully, but the payment is still waiting for final confirmation.";
    }

    private CheckoutStatusVm BuildCheckoutStatusVm(BookOrder order, string? statusMessage = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CheckoutStatusVm
        {
            Order = order,
            CanAccessNow = order.Status == OrderStatus.Paid || order.AuthorId == userId || isAdmin,
            StatusMessage = statusMessage
        };
    }

    private static string CreateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25];
    }

    private async Task SyncAuthorEarningAsync(BookOrder order, CancellationToken cancellationToken)
    {
        var existingEarning = await db.AuthorEarnings
            .FirstOrDefaultAsync(e => e.BookOrderId == order.Id, cancellationToken);

        if (order.Status == OrderStatus.Paid)
        {
            var payoutProvider = ParseProviderType(paymentOptions.Value.PayoutProvider);
            var payoutAccount = await db.AuthorPayoutAccounts
                .FirstOrDefaultAsync(
                    account => account.AuthorId == order.AuthorId && account.Provider == payoutProvider,
                    cancellationToken);

            var earningStatus = payoutAccount?.Status == AuthorPayoutAccountStatus.Active
                ? AuthorEarningStatus.Available
                : AuthorEarningStatus.Pending;
            var autoSettledBySplit = payoutProvider == PaymentProviderType.Paystack
                && order.Provider == PaymentProviderType.Paystack
                && payoutAccount?.Status == AuthorPayoutAccountStatus.Active
                && !string.IsNullOrWhiteSpace(payoutAccount.PaystackSubaccountCode);

            if (autoSettledBySplit)
            {
                earningStatus = AuthorEarningStatus.Paid;
            }

            if (existingEarning == null)
            {
                existingEarning = new AuthorEarning
                {
                    BookOrderId = order.Id,
                    AuthorId = order.AuthorId,
                    PayoutProvider = payoutProvider,
                    AuthorPayoutAccountId = payoutAccount?.Id,
                    Status = earningStatus,
                    Currency = order.Currency,
                    GrossAmount = order.GrossAmount,
                    PlatformFeeAmount = order.PlatformFeeAmount,
                    NetAmount = order.AuthorNetAmount,
                    EarnedAt = order.CompletedAt ?? DateTime.UtcNow,
                    AvailableAt = autoSettledBySplit ? null : DateTime.UtcNow,
                    PaidAt = autoSettledBySplit ? order.CompletedAt ?? DateTime.UtcNow : null,
                    ExternalSettlementReference = autoSettledBySplit
                        ? order.ExternalCheckoutSessionId ?? order.ExternalPaymentIntentId ?? order.OrderNumber
                        : null
                };

                db.AuthorEarnings.Add(existingEarning);
            }
            else
            {
                existingEarning.PayoutProvider = payoutProvider;
                existingEarning.AuthorPayoutAccountId = payoutAccount?.Id;
                existingEarning.Currency = order.Currency;
                existingEarning.GrossAmount = order.GrossAmount;
                existingEarning.PlatformFeeAmount = order.PlatformFeeAmount;
                existingEarning.NetAmount = order.AuthorNetAmount;
                existingEarning.AvailableAt ??= autoSettledBySplit ? null : DateTime.UtcNow;
                existingEarning.PaidAt = autoSettledBySplit ? order.CompletedAt ?? DateTime.UtcNow : existingEarning.PaidAt;
                existingEarning.ExternalSettlementReference = autoSettledBySplit
                    ? order.ExternalCheckoutSessionId ?? order.ExternalPaymentIntentId ?? order.OrderNumber
                    : existingEarning.ExternalSettlementReference;

                if (existingEarning.Status != AuthorEarningStatus.Paid && existingEarning.Status != AuthorEarningStatus.InPayout)
                {
                    existingEarning.Status = earningStatus;
                }
            }

            return;
        }

        if (existingEarning != null && order.Status is OrderStatus.Failed or OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            existingEarning.Status = AuthorEarningStatus.Cancelled;
        }
    }

    private static void ApplyPaymentResultToOrder(
        BookOrder order,
        OrderStatus orderStatus,
        PaymentRecordStatus paymentStatus,
        string? externalCheckoutSessionId,
        string? externalPaymentIntentId,
        string? externalChargeId,
        string? externalReference,
        string? objectType,
        decimal? amount,
        string? currency,
        string? externalEventId,
        string? failureReason,
        string? rawPayloadJson)
    {
        order.ExternalCheckoutSessionId ??= externalCheckoutSessionId;
        order.ExternalPaymentIntentId ??= externalPaymentIntentId;
        order.ExternalChargeId ??= externalChargeId;
        order.UpdatedAt = DateTime.UtcNow;

        switch (orderStatus)
        {
            case OrderStatus.Paid:
                order.Status = OrderStatus.Paid;
                order.CompletedAt ??= DateTime.UtcNow;
                order.FailureReason = null;
                break;

            case OrderStatus.Failed:
                if (order.Status != OrderStatus.Paid)
                {
                    order.Status = OrderStatus.Failed;
                    order.FailureReason = failureReason;
                }
                break;

            case OrderStatus.Cancelled:
                if (order.Status != OrderStatus.Paid)
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancelledAt ??= DateTime.UtcNow;
                    order.FailureReason = failureReason;
                }
                break;

            case OrderStatus.CheckoutCreated:
            case OrderStatus.Pending:
                if (order.Status != OrderStatus.Paid)
                {
                    order.Status = orderStatus;
                    order.FailureReason = failureReason;
                }
                break;
        }

        UpsertPaymentRecord(
            order,
            paymentStatus,
            externalReference,
            externalCheckoutSessionId,
            externalPaymentIntentId,
            objectType,
            amount,
            currency,
            externalEventId,
            rawPayloadJson);
    }

    private static void UpsertPaymentRecord(
        BookOrder order,
        PaymentRecordStatus paymentStatus,
        string? externalReference,
        string? externalCheckoutSessionId,
        string? externalPaymentIntentId,
        string? objectType,
        decimal? amount,
        string? currency,
        string? externalEventId,
        string? rawPayloadJson)
    {
        var safeExternalReference = externalReference
            ?? externalPaymentIntentId
            ?? externalCheckoutSessionId
            ?? order.OrderNumber;

        var record = order.PaymentRecords
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p =>
                p.ExternalReference == safeExternalReference
                || (!string.IsNullOrWhiteSpace(externalCheckoutSessionId) && p.ExternalReference == externalCheckoutSessionId)
                || (!string.IsNullOrWhiteSpace(externalPaymentIntentId) && p.ExternalReference == externalPaymentIntentId));

        if (record == null)
        {
            record = new PaymentRecord
            {
                BookOrderId = order.Id,
                Provider = order.Provider,
                ExternalReference = safeExternalReference,
                ObjectType = string.IsNullOrWhiteSpace(objectType) ? "checkout_session" : objectType,
                Amount = amount ?? order.GrossAmount,
                Currency = string.IsNullOrWhiteSpace(currency) ? order.Currency : currency,
                CreatedAt = DateTime.UtcNow
            };

            order.PaymentRecords.Add(record);
        }

        record.Status = paymentStatus;
        record.ExternalEventId = externalEventId;
        record.Amount = amount ?? record.Amount;
        record.Currency = string.IsNullOrWhiteSpace(currency) ? record.Currency : currency;
        record.RawPayloadJson = rawPayloadJson;

        if (record.Status == PaymentRecordStatus.Succeeded)
        {
            record.ConfirmedAt ??= DateTime.UtcNow;
        }
    }

    private static void ApplyPayoutResultToItem(
        PayoutItem payoutItem,
        PayoutItemStatus payoutStatus,
        string? externalTransferId,
        string? externalReference,
        string? failureReason,
        DateTime? paidAt)
    {
        payoutItem.ExternalTransferId ??= externalTransferId;
        payoutItem.ExternalReference ??= externalReference;
        payoutItem.FailureReason = failureReason;
        payoutItem.Status = payoutStatus;

        switch (payoutStatus)
        {
            case PayoutItemStatus.Paid:
                payoutItem.PaidAt ??= paidAt ?? DateTime.UtcNow;
                payoutItem.AuthorEarning.Status = AuthorEarningStatus.Paid;
                payoutItem.AuthorEarning.PaidAt ??= payoutItem.PaidAt;
                payoutItem.AuthorEarning.ExternalSettlementReference ??= payoutItem.ExternalTransferId ?? payoutItem.ExternalReference;
                break;

            case PayoutItemStatus.Failed:
                payoutItem.AuthorEarning.Status = AuthorEarningStatus.Available;
                payoutItem.AuthorEarning.PaidAt = null;
                break;

            default:
                payoutItem.AuthorEarning.Status = AuthorEarningStatus.InPayout;
                break;
        }
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
            payoutBatch.Status = PayoutBatchStatus.Failed;
            payoutBatch.ProcessedAt ??= DateTime.UtcNow;
            return;
        }

        payoutBatch.Status = PayoutBatchStatus.Processing;
    }

    private static PaymentProviderType ParseProviderType(string? providerName)
    {
        return Enum.TryParse<PaymentProviderType>(providerName, true, out var parsed)
            ? parsed
            : PaymentProviderType.Paystack;
    }
}
