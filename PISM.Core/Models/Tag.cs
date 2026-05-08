namespace PISM.Core.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<ImageFile> ImageFiles { get; set; } = [];
}
