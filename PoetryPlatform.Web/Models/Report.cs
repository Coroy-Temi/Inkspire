namespace PoetryPlatform.Web.Models;

public class Report
{
    public int Id { get; set; }

    public int PoemId { get; set; }
    public Poem Poem { get; set; } = null!;

    public string ReporterId { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
