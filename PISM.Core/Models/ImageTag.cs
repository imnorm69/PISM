namespace PISM.Core.Models;

public class ImageTag
{
    public Guid Id { get; set; }
    public Guid ImageFileId { get; set; }
    public string Tag { get; set; } = null!;
    public ImageFile ImageFile { get; set; } = null!;
}
