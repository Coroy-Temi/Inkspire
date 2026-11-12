using Microsoft.AspNetCore.Identity;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string AuthorName { get; set; } = string.Empty;
        public string? ProfileImagePath { get; set; }
        public string CountryCode { get; set; } = "US";
        public PaymentProviderType? PreferredCheckoutProvider { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
