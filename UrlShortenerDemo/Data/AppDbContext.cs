using Microsoft.EntityFrameworkCore;
using UrlShortenerDemo.Entities;

namespace UrlShortenerDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShortenedUrl>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(12).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();
            });
        }
    }
}
