using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Models;

namespace PoetryPlatform.Web.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Poem> Poems { get; set; }
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<PoemLike> PoemLikes => Set<PoemLike>();
        public DbSet<Report> Reports => Set<Report>();

        public DbSet<PoemView> PoemViews => Set<PoemView>();
        public DbSet<CommentFlag> CommentFlags { get; set; }

    }
}
