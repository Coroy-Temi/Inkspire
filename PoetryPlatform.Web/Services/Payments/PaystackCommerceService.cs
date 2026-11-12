using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Services.Payments;

public class PaystackCommerceService(
    IHttpClientFactory httpClientFactory,
    IOptions<PaystackPaymentOptions> options,
    ILogger<PaystackCommerceService> logger) : IPaystackCommerceService
{
    private readonly PaystackPaymentOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<IReadOnlyList<PaystackBankOption>> ListBanksAsync(
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var normalizedCountry = NormalizeCountryCode(countryCode);
        var countryName = ResolveCountryName(normalizedCountry);
        var currency = ResolveCurrency(normalizedCountry);
        var channelType = ResolveRecipientType(normalizedCountry) switch
        {
            "ghipss" => "ghipss",
            "mobile_money" => "mobile_money",
            _ => null
        };

        var url = $"/bank?country={Uri.EscapeDataString(countryName)}&currency={Uri.EscapeDataString(currency)}";
        if (!string.IsNullOrWhiteSpace(channelType))
        {
            url += $"&type={Uri.EscapeDataString(channelType)}";
        }

        var client = CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Paystack bank list failed for {CountryCode} with {StatusCode}.", normalizedCountry, response.StatusCode);
            return [];
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        if (data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return data.EnumerateArray()
            .Select(bank => new PaystackBankOption
            {
                Code = GetString(bank, "code") ?? string.Empty,
                Name = GetString(bank, "name") ?? string.Empty,
                Currency = GetString(bank, "currency") ?? currency,
                Type = GetString(bank, "type") ?? ResolveRecipientType(normalizedCountry),
                Country = GetString(bank, "country") ?? countryName
            })
            .Where(bank => !string.IsNullOrWhiteSpace(bank.Code) && !string.IsNullOrWhiteSpace(bank.Name))
            .OrderBy(bank => bank.Name)
            .ToList();
    }

    public async Task<PaystackAuthorPayoutSetupResult> SyncAuthorPayoutAccountAsync(
        ApplicationUser author,
        AuthorPayoutAccount? existingAccount,
        string countryCode,
        string businessName,
        string bankCode,
        string accountNumber,
        decimal platformCommissionPercent,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PaystackAuthorPayoutSetupResult
            {
                ErrorMessage = "Paystack is not configured yet. Add your Paystack secret key before setting up author payouts."
            };
        }

        var normalizedCountry = NormalizeCountryCode(countryCode);
        var normalizedBusinessName = string.IsNullOrWhiteSpace(businessName)
            ? string.IsNullOrWhiteSpace(author.AuthorName) ? author.UserName ?? "Inkspire Author" : author.AuthorName
            : businessName.Trim();
        var normalizedBankCode = bankCode.Trim();
        var normalizedAccountNumber = accountNumber.Trim();
        var currency = ResolveCurrency(normalizedCountry);
        var recipientType = ResolveRecipientType(normalizedCountry);

        var resolvedAccountName = await ResolveAccountNameAsync(
            normalizedCountry,
            normalizedBankCode,
            normalizedAccountNumber,
            cancellationToken);

        var recipientResult = await CreateTransferRecipientAsync(
            normalizedBusinessName,
            normalizedBankCode,
            normalizedAccountNumber,
            currency,
            recipientType,
            resolvedAccountName,
            cancellationToken);

        if (!recipientResult.Succeeded)
        {
            return new PaystackAuthorPayoutSetupResult
            {
                ErrorMessage = recipientResult.ErrorMessage
            };
        }

        var subaccountResult = await UpsertSubaccountAsync(
            existingAccount?.PaystackSubaccountCode,
            normalizedBusinessName,
            author,
            normalizedBankCode,
            normalizedAccountNumber,
            platformCommissionPercent,
            cancellationToken);

        if (!subaccountResult.Succeeded)
        {
            return new PaystackAuthorPayoutSetupResult
            {
                ErrorMessage = subaccountResult.ErrorMessage
            };
        }

        var account = existingAccount ?? new AuthorPayoutAccount
        {
            AuthorId = author.Id,
            Provider = PaymentProviderType.Paystack,
            CreatedAt = DateTime.UtcNow
        };

        account.CountryCode = normalizedCountry;
        account.Currency = currency.ToLowerInvariant();
        account.BusinessName = normalizedBusinessName;
        account.BankCode = normalizedBankCode;
        account.BankName = recipientResult.BankName;
        account.AccountNumber = normalizedAccountNumber;
        account.AccountName = resolvedAccountName ?? recipientResult.AccountName ?? subaccountResult.AccountName;
        account.PaystackRecipientCode = recipientResult.RecipientCode;
        account.PaystackRecipientType = recipientType;
        account.PaystackSubaccountCode = subaccountResult.SubaccountCode;
        account.PaystackSubaccountId = subaccountResult.SubaccountId;
        account.Status = AuthorPayoutAccountStatus.Active;
        account.ChargesEnabled = true;
        account.PayoutsEnabled = true;
        account.DetailsSubmitted = true;
        account.RequirementsSummary = null;
        account.UpdatedAt = DateTime.UtcNow;

        return new PaystackAuthorPayoutSetupResult
        {
            Succeeded = true,
            Account = account
        };
    }

    public async Task<PaystackTransferBatchSubmitResult> SubmitAvailablePayoutBatchAsync(
        PayoutBatch batch,
        IReadOnlyList<PayoutItem> items,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "Paystack is not configured yet. Add your Paystack secret key before submitting author payout transfers."
            };
        }

        if (!_options.BulkTransfersEnabled)
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "Bulk transfers are disabled in Paystack options for this environment."
            };
        }

        if (items.Count == 0)
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "There are no payout items ready to transfer."
            };
        }

        var payoutCurrency = items[0].Currency.ToUpperInvariant();
        if (items.Any(item => !string.Equals(item.Currency, items[0].Currency, StringComparison.OrdinalIgnoreCase)))
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "A payout batch can only contain one currency at a time."
            };
        }

        if (items.Any(item => string.IsNullOrWhiteSpace(item.AuthorPayoutAccount?.PaystackRecipientCode)))
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "One or more payout items are missing a Paystack recipient code."
            };
        }

        var payload = new
        {
            source = "balance",
            currency = payoutCurrency,
            transfers = items.Select(item => new
            {
                amount = ToMinorUnits(item.Amount, payoutCurrency),
                recipient = item.AuthorPayoutAccount!.PaystackRecipientCode,
                reference = item.ExternalReference,
                reason = $"Inkspire author payout {batch.BatchReference}"
            })
        };

        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/transfer/bulk", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = BuildApiErrorMessage("Paystack bulk transfer failed", json, response.StatusCode)
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        if (data.ValueKind != JsonValueKind.Array)
        {
            return new PaystackTransferBatchSubmitResult
            {
                ErrorMessage = "Paystack did not return transfer data for the payout batch."
            };
        }

        var transfers = data.EnumerateArray()
            .Select(item => new PaystackTransferSubmission
            {
                Reference = GetString(item, "reference") ?? string.Empty,
                TransferCode = GetString(item, "transfer_code"),
                Status = GetString(item, "status") ?? string.Empty,
                FailureReason = GetFailures(item)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Reference))
            .ToDictionary(item => item.Reference, item => item, StringComparer.OrdinalIgnoreCase);

        return new PaystackTransferBatchSubmitResult
        {
            Succeeded = transfers.Count > 0,
            ErrorMessage = transfers.Count > 0 ? null : "Paystack did not return any transfer submissions for this batch.",
            Transfers = transfers
        };
    }

    public async Task<PaystackTransferFetchResult> FetchTransferAsync(
        string transferCode,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PaystackTransferFetchResult
            {
                ErrorMessage = "Paystack is not configured yet."
            };
        }

        var client = CreateClient();
        using var response = await client.GetAsync($"/transfer/{Uri.EscapeDataString(transferCode)}", cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaystackTransferFetchResult
            {
                ErrorMessage = BuildApiErrorMessage("Paystack transfer fetch failed", json, response.StatusCode)
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");

        return new PaystackTransferFetchResult
        {
            Succeeded = true,
            Reference = GetString(data, "reference") ?? string.Empty,
            TransferCode = GetString(data, "transfer_code") ?? transferCode,
            Status = GetString(data, "status") ?? string.Empty,
            FailureReason = GetFailures(data),
            TransferredAt = GetDateTime(data, "transferred_at")
        };
    }

    private async Task<string?> ResolveAccountNameAsync(
        string countryCode,
        string bankCode,
        string accountNumber,
        CancellationToken cancellationToken)
    {
        if (countryCode is not ("NG" or "GH"))
        {
            return null;
        }

        var client = CreateClient();
        using var response = await client.GetAsync(
            $"/bank/resolve?account_number={Uri.EscapeDataString(accountNumber)}&bank_code={Uri.EscapeDataString(bankCode)}",
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(BuildApiErrorMessage("Paystack could not verify this account number", json, response.StatusCode));
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        return GetString(data, "account_name");
    }

    private async Task<PaystackRecipientCreateResult> CreateTransferRecipientAsync(
        string businessName,
        string bankCode,
        string accountNumber,
        string currency,
        string recipientType,
        string? resolvedAccountName,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = recipientType,
            name = resolvedAccountName ?? businessName,
            account_number = accountNumber,
            bank_code = bankCode,
            currency = currency.ToUpperInvariant()
        };

        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/transferrecipient", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaystackRecipientCreateResult
            {
                ErrorMessage = BuildApiErrorMessage("Paystack transfer recipient creation failed", json, response.StatusCode)
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");
        var details = GetProperty(data, "details");

        return new PaystackRecipientCreateResult
        {
            Succeeded = true,
            RecipientCode = GetString(data, "recipient_code"),
            BankName = GetString(details, "bank_name"),
            AccountName = GetString(details, "account_name") ?? resolvedAccountName
        };
    }

    private async Task<PaystackSubaccountSyncResult> UpsertSubaccountAsync(
        string? existingSubaccountCode,
        string businessName,
        ApplicationUser author,
        string bankCode,
        string accountNumber,
        decimal platformCommissionPercent,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            author_id = author.Id,
            author_name = string.IsNullOrWhiteSpace(author.AuthorName) ? author.UserName : author.AuthorName,
            inkspire = true
        });

        var payload = new
        {
            business_name = businessName,
            description = $"Inkspire author settlement profile for {businessName}",
            bank_code = bankCode,
            settlement_bank = bankCode,
            account_number = accountNumber,
            percentage_charge = Math.Round(platformCommissionPercent, 2, MidpointRounding.AwayFromZero),
            primary_contact_email = author.Email,
            primary_contact_name = businessName,
            primary_contact_phone = author.PhoneNumber,
            metadata
        };

        var client = CreateClient();
        using var response = string.IsNullOrWhiteSpace(existingSubaccountCode)
            ? await client.PostAsJsonAsync("/subaccount", payload, cancellationToken)
            : await client.PutAsJsonAsync($"/subaccount/{Uri.EscapeDataString(existingSubaccountCode)}", payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new PaystackSubaccountSyncResult
            {
                ErrorMessage = BuildApiErrorMessage("Paystack subaccount sync failed", json, response.StatusCode)
            };
        }

        using var document = JsonDocument.Parse(json);
        var data = GetProperty(document.RootElement, "data");

        return new PaystackSubaccountSyncResult
        {
            Succeeded = true,
            SubaccountCode = GetString(data, "subaccount_code"),
            SubaccountId = GetString(data, "id"),
            AccountName = GetString(data, "account_name")
        };
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("Paystack");
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static int ToMinorUnits(decimal amount, string currency)
    {
        var multiplier = currency.ToUpperInvariant() switch
        {
            "NGN" => 100,
            "GHS" => 100,
            "KES" => 100,
            "ZAR" => 100,
            _ => 100
        };

        return (int)Math.Round(amount * multiplier, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        return string.IsNullOrWhiteSpace(countryCode)
            ? "NG"
            : countryCode.Trim().ToUpperInvariant();
    }

    private static string ResolveCountryName(string countryCode)
    {
        return countryCode switch
        {
            "GH" => "ghana",
            "KE" => "kenya",
            "ZA" => "south africa",
            _ => "nigeria"
        };
    }

    private static string ResolveCurrency(string countryCode)
    {
        return countryCode switch
        {
            "GH" => "GHS",
            "KE" => "KES",
            "ZA" => "ZAR",
            _ => "NGN"
        };
    }

    private static string ResolveRecipientType(string countryCode)
    {
        return countryCode switch
        {
            "GH" => "ghipss",
            "KE" => "kepss",
            "ZA" => "basa",
            _ => "nuban"
        };
    }

    private static string BuildApiErrorMessage(string prefix, string responseJson, System.Net.HttpStatusCode statusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var message = GetString(document.RootElement, "message");
            return string.IsNullOrWhiteSpace(message)
                ? $"{prefix} with {(int)statusCode}."
                : $"{prefix}: {message}";
        }
        catch
        {
            return $"{prefix} with {(int)statusCode}.";
        }
    }

    private static string? GetFailures(JsonElement element)
    {
        var property = GetProperty(element, "failures");
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Array => string.Join(", ", property.EnumerateArray().Select(item => item.ToString())),
            JsonValueKind.Object => property.ToString(),
            _ => null
        };
    }

    private static JsonElement GetProperty(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property
            : default;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private sealed class PaystackRecipientCreateResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
        public string? RecipientCode { get; init; }
        public string? BankName { get; init; }
        public string? AccountName { get; init; }
    }

    private sealed class PaystackSubaccountSyncResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
        public string? SubaccountCode { get; init; }
        public string? SubaccountId { get; init; }
        public string? AccountName { get; init; }
    }
}
