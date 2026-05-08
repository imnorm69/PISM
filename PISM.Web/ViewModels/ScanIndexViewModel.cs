using PISM.Core.Models;

namespace PISM.Web.ViewModels;

public class ScanIndexViewModel
{
    public List<ScanJob> ActiveJobs { get; set; } = [];
    public List<ScanJob> RecentJobs { get; set; } = [];
}
