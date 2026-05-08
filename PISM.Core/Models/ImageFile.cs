using PISM.Core.Enums;

namespace PISM.Core.Models;

public class ImageFile
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string OriginalFolder { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string Hash { get; set; } = null!;
    public DateTime DateScanned { get; set; }
    public ImageStatus Status { get; set; }
    public bool IsEncrypted { get; set; }
    public string EncryptedFilePath { get; set; } = null!;
    public int? CropX { get; set; }
    public int? CropY { get; set; }
    public int? CropWidth { get; set; }
    public int? CropHeight { get; set; }
    public int RotationDegrees { get; set; }
    public ICollection<ImageTag> Tags { get; set; } = [];
}
