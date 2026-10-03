
using ArticleService.BE;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.DAL;

public class ArticleServiceContext : DbContext
{
    public ArticleServiceContext(
        DbContextOptions<ArticleServiceContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var article = modelBuilder.Entity<Article>();

        article.ToTable("articles");

        article.HasKey(a => a.Id);

        article.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        article.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        article.Property(a => a.Content)
            .HasColumnName("content")
            .IsRequired();

        article.Property(a => a.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();
    }
}