using PISM.Core.Enums;

namespace PISM.Core.Models;

public class ImageFile
{
    public Guid Id { get; set; }
    public string OriginalPath { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = null!;
    public bool IsEncrypted { get; set; }
    public ImageStatus Status { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
}
