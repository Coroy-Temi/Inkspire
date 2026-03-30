using System.Collections.Generic;


namespace PoetryPlatform.Web.Models
{
    public class MostCommentedPoemVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }

        
        // Ranking
        public int Rank { get; set; }

    }
}
