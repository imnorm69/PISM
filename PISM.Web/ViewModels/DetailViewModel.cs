using PISM.Core.Enums;
using PISM.Core.Models;

namespace PISM.Web.ViewModels;

public class DetailViewModel
{
    public ImageFile Image { get; set; } = null!;
    public Guid? PreviousId { get; set; }
    public Guid? NextId { get; set; }
    public ImageFile? DuplicateOf { get; set; }
    public ImageStatus CurrentStatus { get; set; }
    public List<ImageFile> RelatedImages { get; set; } = [];
}
