using Microsoft.EntityFrameworkCore;
using PISM.Core.Models;

namespace PISM.Data;

public class PismDbContext : DbContext
{
    public PismDbContext(DbContextOptions<PismDbContext> options) : base(options) { }

    public DbSet<ImageFile> ImageFiles => Set<ImageFile>();
    public DbSet<ImageTag> ImageTags => Set<ImageTag>();
    public DbSet<DeletedHash> DeletedHashes => Set<DeletedHash>();
    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();
    public DbSet<FolderContent> FolderContents => Set<FolderContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            e.Property(x => x.OriginalFolder).HasMaxLength(1024).IsRequired();
            e.Property(x => x.Hash).HasMaxLength(64).IsRequired();
            e.Property(x => x.EncryptedFilePath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.RotationDegrees).HasDefaultValue(0);
            e.HasIndex(x => x.Hash);
            e.HasIndex(x => x.Status);
            e.HasOne<ImageFile>().WithMany().HasForeignKey(x => x.DuplicateOfId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Tags).WithOne(t => t.ImageFile).HasForeignKey(t => t.ImageFileId);
        });

        modelBuilder.Entity<ImageTag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tag).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ImageFileId);
            e.HasIndex(x => x.Tag);
            e.HasIndex(x => new { x.ImageFileId, x.Tag }).IsUnique();
        });

        modelBuilder.Entity<DeletedHash>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Hash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Hash).IsUnique();
        });

        modelBuilder.Entity<ScanJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FolderPath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(2048);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<FolderContent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.FolderPath).HasMaxLength(1024).IsRequired();
            e.HasIndex(x => x.FileHash);
            e.HasIndex(x => x.FolderPath);
            e.HasIndex(x => new { x.FileHash, x.FolderPath }).IsUnique();
        });
    }
}
