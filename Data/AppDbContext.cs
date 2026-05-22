using Microsoft.EntityFrameworkCore;
using mkinfotech.Models.Blog;

namespace mkinfotech.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // TABLE
        public DbSet<BlogCategory> BlogCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TABLE NAME
            modelBuilder.Entity<BlogCategory>()
                .ToTable("mktech_blog_categories");

            // PRIMARY KEY
            modelBuilder.Entity<BlogCategory>()
                .HasKey(x => x.CategoryId);

            // COLUMN RULES (optional but good)
            modelBuilder.Entity<BlogCategory>()
                .Property(x => x.CategoryName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<BlogCategory>()
                .Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<BlogCategory>()
                .Property(x => x.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<BlogCategory>()
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW()");
        }
    }
}