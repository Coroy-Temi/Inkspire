using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Models;

public class PoemReadVm
{
    public Poem Poem { get; set; } = null!;
    public List<string> Pages { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages => Pages.Count;
    public int LikeCount { get; set; }
    public bool UserHasLiked { get; set; }
    public bool ViewerOwnsPoem { get; set; }
    public bool HasPurchasedAccess { get; set; }
    public int? LatestOrderId { get; set; }
    public OrderStatus? LatestOrderStatus { get; set; }
    public string? LatestCheckoutUrl { get; set; }
    public string? LatestFailureReason { get; set; }
    public List<int> UserCommentFlags { get; set; } = [];
}
