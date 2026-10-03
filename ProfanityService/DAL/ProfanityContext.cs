using Microsoft.EntityFrameworkCore;
using ProfanityService.BE;

namespace ProfanityService.DAL;

public class ProfanityContext : DbContext
{
    public ProfanityContext(
        DbContextOptions<ProfanityContext> options)
        : base(options)
    {
    }

    public DbSet<ProfanityWord> ProfanityWords =>
        Set<ProfanityWord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var word = modelBuilder.Entity<ProfanityWord>();

        word.ToTable("profanity_words");

        word.HasKey(w => w.Id);

        word.Property(w => w.Id)
            .HasColumnName("id");

        word.Property(w => w.Word)
            .HasColumnName("word")
            .HasMaxLength(100)
            .IsRequired();

        word.HasIndex(w => w.Word)
            .IsUnique();
    }
}