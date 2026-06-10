using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLink>(entity =>
        {
            entity.ToTable("short_links");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.OriginalUrl)
                .HasColumnName("original_url")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.HitCount)
                .HasColumnName("hit_count")
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("idx_short_links_code");
        });
    }
}
