namespace PoetryPlatform.Web.Models;

public class WriterPoemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public int Views { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
}
