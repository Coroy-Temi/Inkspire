using System;

namespace PoetryPlatform.Web.Models;

public class PoemLike
{
    public int Id { get; set; }

    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
