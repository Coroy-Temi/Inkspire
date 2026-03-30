namespace PoetryPlatform.Web.Models;

public class AdminCommentVm
{
    public int CommentId { get; set; }
    public string Content { get; set; } = string.Empty;

    public string CommenterEmail { get; set; } = string.Empty;

    public string PoemTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsFlagged { get; set; }
}
