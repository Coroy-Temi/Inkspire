using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Data;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;
using PoetryPlatform.Web.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// =====================
// Database
// =====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// =====================
// Identity
// =====================
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<PaymentPlatformOptions>(
    builder.Configuration.GetSection(PaymentPlatformOptions.SectionName));
// Stripe is intentionally parked for later. Paystack is the active payment flow right now.
builder.Services.Configure<StripePaymentOptions>(
    builder.Configuration.GetSection(StripePaymentOptions.SectionName));
builder.Services.Configure<PaystackPaymentOptions>(
    builder.Configuration.GetSection(PaystackPaymentOptions.SectionName));

builder.Services.AddHttpClient("Paystack", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IPaymentFeeCalculator, PaymentFeeCalculator>();
// Keep Stripe registered in the codebase so it can be revived later without rebuilding the abstraction.
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
builder.Services.AddScoped<IPaymentProvider, PaystackPaymentProvider>();
builder.Services.AddScoped<IPaystackCommerceService, PaystackCommerceService>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
builder.Services.AddScoped<ICheckoutProviderResolver, CheckoutProviderResolver>();
// Stripe Connect is parked for now, but the service stays available for future expansion.
builder.Services.AddScoped<IStripeConnectOnboardingService, StripeConnectOnboardingService>();

// =====================
// MVC
// =====================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// =====================
// Middleware
// =====================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// =====================
// Routing
// =====================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedRolesAndAdminAsync(services);
}

app.Run();
