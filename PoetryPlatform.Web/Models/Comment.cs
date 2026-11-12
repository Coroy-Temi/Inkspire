namespace PoetryPlatform.Web.Models;

public class Comment
{
    public int Id { get; set; }

    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Flagged by users for review

    public int FlagCount { get; set; } = 0;
    public bool IsFlagged { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public DateTime? FlaggedAt { get; set; }

}
