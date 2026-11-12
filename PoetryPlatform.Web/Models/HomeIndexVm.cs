namespace PoetryPlatform.Web.Models;

public class HomeIndexVm
{
    public int PublishedPoems { get; set; }
    public int ActiveAuthors { get; set; }
    public int TotalReads { get; set; }
    public int TotalComments { get; set; }
    public int BookListings { get; set; }
    public List<PoemCardVm> LatestReleases { get; set; } = [];
    public List<TrendingPoemVm> TrendingNow { get; set; } = [];
    public List<MostCommentedPoemVm> ConversationStarters { get; set; } = [];
}
