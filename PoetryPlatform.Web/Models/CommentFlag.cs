namespace PoetryPlatform.Web.Models
{
    public class CommentFlag
    {
            public int Id { get; set; }

            public int CommentId { get; set; }
            public Comment Comment { get; set; } = null!;

            public string UserId { get; set; } = string.Empty;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        

    }
}
