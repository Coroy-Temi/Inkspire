namespace PoetryPlatform.Web.Models
{
    public class MostCommentedPoemVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorProfileImagePath { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Rank { get; set; }
    }
}
