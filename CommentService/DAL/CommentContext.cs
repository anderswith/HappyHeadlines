using CommentService.BE;
using Microsoft.EntityFrameworkCore;

namespace CommentService.DAL;

public class CommentContext : DbContext
{
    public CommentContext(
        DbContextOptions<CommentContext> options)
        : base(options)
    {
    }

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var comment = modelBuilder.Entity<Comment>();

        comment.ToTable("comments");

        comment.HasKey(c => c.Id);

        comment.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        comment.Property(c => c.ArticleId)
            .HasColumnName("article_id");

        comment.Property(c => c.ArticleRegion)
            .HasColumnName("article_region")
            .HasMaxLength(30)
            .IsRequired();

        comment.Property(c => c.Author)
            .HasColumnName("author")
            .HasMaxLength(100)
            .IsRequired();

        comment.Property(c => c.Text)
            .HasColumnName("text")
            .IsRequired();

        comment.Property(c => c.CreatedUtc)
            .HasColumnName("created_utc");

        comment.HasIndex(c => new
        {
            c.ArticleRegion,
            c.ArticleId
        });
    }
}