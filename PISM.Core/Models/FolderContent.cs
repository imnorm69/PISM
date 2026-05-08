namespace PISM.Core.Models;

public class FolderContent
{
    public Guid Id { get; set; }
    public string FileHash { get; set; } = null!;
    public string FolderPath { get; set; } = null!;
}
