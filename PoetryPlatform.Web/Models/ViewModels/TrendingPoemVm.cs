namespace PoetryPlatform.Web.Models;

public class TrendingPoemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorProfileImagePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UniqueReaders { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Score { get; set; }
}
