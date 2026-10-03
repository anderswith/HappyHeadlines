using DraftService.BE;
using Microsoft.EntityFrameworkCore;

namespace DraftService.DAL;

public class DraftContext : DbContext
{
    public DraftContext(
        DbContextOptions<DraftContext> options)
        : base(options)
    {
    }

    public DbSet<Draft> Drafts => Set<Draft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var draft = modelBuilder.Entity<Draft>();

        draft.ToTable("drafts");

        draft.HasKey(d => d.Id);

        draft.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        draft.Property(d => d.Author)
            .HasColumnName("author")
            .HasMaxLength(100)
            .IsRequired();

        draft.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        draft.Property(d => d.Content)
            .HasColumnName("content")
            .IsRequired();

        draft.Property(d => d.CreatedUtc)
            .HasColumnName("created_utc");

        draft.Property(d => d.UpdatedUtc)
            .HasColumnName("updated_utc");
    }
}