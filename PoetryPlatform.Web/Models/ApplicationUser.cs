using Microsoft.AspNetCore.Identity;

namespace PoetryPlatform.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Public-facing author / pen name
        public string AuthorName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
