using PISM.Core.Enums;
using PISM.Core.Models;

namespace PISM.Web.ViewModels;

public class GalleryPageViewModel
{
    public List<ImageFile> Images { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public ImageStatus CurrentStatus { get; set; }
}
