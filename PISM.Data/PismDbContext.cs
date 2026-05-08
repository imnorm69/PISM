using Microsoft.EntityFrameworkCore;
using PISM.Core.Models;

namespace PISM.Data;

public class PismDbContext : DbContext
{
    public PismDbContext(DbContextOptions<PismDbContext> options) : base(options) { }

    public DbSet<ImageFile> ImageFiles => Set<ImageFile>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Sha256Hash);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.OriginalFileName).IsRequired();
            e.Property(x => x.StoredFileName).IsRequired();
            e.Property(x => x.ImportedAt).IsRequired();
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
    }
}
