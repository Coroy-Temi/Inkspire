# Inkspire

Inkspire is a platform for reading, publishing, and selling poetry and other writing. Readers can browse the library, discuss and like pieces, and unlock paid PDF editions. Authors can write and preview multi-page pieces, publish free work, sell PDF editions, or link to an external storefront.

## Run locally

Inkspire uses ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core, and SQLite. Install the .NET 9 SDK and the EF Core 9 CLI, then run these commands from the repository root:

```powershell
dotnet restore PoetryPlatform.sln
dotnet ef database update --project PoetryPlatform.Web --startup-project PoetryPlatform.Web
dotnet run --project PoetryPlatform.Web --launch-profile https
```

Open [https://localhost:7275](https://localhost:7275). If your browser rejects the local development certificate, run `dotnet dev-certs https --trust`.

## Paystack setup

For local checkout testing, set your Paystack test secret key with .NET user secrets:

```powershell
dotnet user-secrets set "Paystack:SecretKey" "sk_test_REPLACE_ME" --project PoetryPlatform.Web
```

For deployed environments, use the `Paystack__SecretKey` environment variable or your hosting platform's secret store. Configure Paystack to send webhooks to the deployed application's public HTTPS endpoint at `/checkout/webhooks/paystack`.

Authors can connect bank details from **Author Studio > Payouts**. New purchases can split through the author's connected Paystack subaccount, with Inkspire's commission set in `Payments:PlatformCommissionPercent`. Checkout defaults to NGN. Paid releases require preview text and a full PDF.

Admins can submit eligible earnings for transfer and refresh transfer statuses from **Admin > Payouts**. Paystack transfer availability depends on the merchant account's configuration.
