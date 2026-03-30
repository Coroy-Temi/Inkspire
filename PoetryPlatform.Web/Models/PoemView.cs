namespace PoetryPlatform.Web.Models;

public class PoemView
{
    public int Id { get; set; }

    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
