using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; 

namespace PoetryPlatform.Web.Models
{
    public enum ContentAccessType
    {
        Free = 0,          // Free / Subscription-based
        PlatformPaid = 1,  // Paid on this platform
        ExternalLink = 2   // Amazon, Gumroad, etc.
    }

    public class Poem
    {
        public int Id { get; set; }

        // =====================
        // Core Content
        // =====================
        public string Title { get; set; } = string.Empty;

        // Used for poems & short writings
        public string Content { get; set; } = string.Empty;

        // Optional PDF upload (long writings / books)
        public string? PdfPath { get; set; }

        // =====================
        // Author
        // =====================
        public string AuthorId { get; set; } = string.Empty;

        // Navigation property (nullable is correct)
        public ApplicationUser? Author { get; set; }

        // =====================
        // Access & Monetization
        // =====================
        public ContentAccessType AccessType { get; set; } = ContentAccessType.Free;

        // Only used when AccessType == PlatformPaid
        public decimal Price { get; set; } = 0;

        // Only used when AccessType == ExternalLink
        public string? ExternalPurchaseUrl { get; set; }

        // =====================
        // Visibility
        // =====================
        public bool IsPublished { get; set; } = true;

        // =====================
        // Engagement Metrics
        // =====================
        public int Views { get; set; } = 0;

        public int Likes { get; set; } = 0;

        // =====================
        // COMMENTS (🔥 ADDED – DO NOT REMOVE)
        // =====================
        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        // =====================
        // Metadata
        // =====================

        [NotMapped]
        public int UniqueReaders { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
