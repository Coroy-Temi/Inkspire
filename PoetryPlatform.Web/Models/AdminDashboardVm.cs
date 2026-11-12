namespace PoetryPlatform.Web.Models;

public class AdminDashboardVm
{
    public int TotalUsers { get; set; }
    public int PublishedPoems { get; set; }
    public int TotalComments { get; set; }
    public int FlaggedComments { get; set; }
    public int TotalLikes { get; set; }
    public int TotalReads { get; set; }
    public int PdfListings { get; set; }
    public int PaidListings { get; set; }
    public int ExternalListings { get; set; }
    public decimal PlatformCommissionPercent { get; set; }
    public List<AdminTopAuthorVm> TopAuthors { get; set; } = [];
}

public class AdminTopAuthorVm
{
    public string AuthorName { get; set; } = string.Empty;
    public int Poems { get; set; }
    public int Views { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
}
