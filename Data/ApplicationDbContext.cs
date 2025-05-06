using Microsoft.EntityFrameworkCore;
using ClipsAutomation.Models;

namespace ClipsAutomation
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<VideoProject> VideoProjects { get; set; } = null!;
        public DbSet<VideoSegment> VideoSegments { get; set; } = null!;
        public DbSet<GeneratedClip> GeneratedClips { get; set; } = null!;
        public DbSet<ProcessingOptions> ProcessingOptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<VideoProject>()
                .HasOne(p => p.ProcessingOptions)
                .WithOne(o => o.VideoProject)
                .HasForeignKey<ProcessingOptions>(o => o.VideoProjectId);

            modelBuilder.Entity<VideoProject>()
                .HasMany(p => p.VideoSegments)
                .WithOne(s => s.VideoProject)
                .HasForeignKey(s => s.VideoProjectId);

            modelBuilder.Entity<VideoProject>()
                .HasMany(p => p.GeneratedClips)
                .WithOne(c => c.VideoProject)
                .HasForeignKey(c => c.VideoProjectId);

            // Configure many-to-many relationship between GeneratedClip and VideoSegment
            modelBuilder.Entity<GeneratedClip>()
                .HasMany(c => c.IncludedSegments)
                .WithMany();

            // Configure data types
            modelBuilder.Entity<VideoSegment>()
                .Property(s => s.EngagementScore)
                .HasPrecision(18, 6);

            modelBuilder.Entity<GeneratedClip>()
                .Property(c => c.DurationSeconds)
                .HasPrecision(18, 6);
        }
    }
} 