namespace PoetryPlatform.Web.Models;

public class PoemCardVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorProfileImagePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public ContentAccessType AccessType { get; set; }
    public decimal Price { get; set; }
    public bool HasPdf { get; set; }
    public string? ExternalPurchaseUrl { get; set; }
    public int Views { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
}
