using Microsoft.EntityFrameworkCore;
using NewsletterService.BE;

namespace NewsletterService.DAL;

public class NewsletterContext : DbContext
{
    public NewsletterContext(
        DbContextOptions<NewsletterContext> options)
        : base(options)
    {
    }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscriber = modelBuilder.Entity<Subscriber>();

        subscriber.ToTable("subscribers");

        subscriber.HasKey(value => value.Id);

        subscriber.Property(value => value.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        subscriber.Property(value => value.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        subscriber.HasIndex(value => value.Email)
            .IsUnique();

        subscriber.Property(value => value.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();
    }
}