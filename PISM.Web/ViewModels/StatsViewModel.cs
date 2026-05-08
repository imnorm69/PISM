using PISM.Core.Models;

namespace PISM.Web.ViewModels;

public class StatsViewModel
{
    public int NeedsReviewCount { get; set; }
    public int KeptCount { get; set; }
    public int DeletedCount { get; set; }
    public long NeedsReviewBytes { get; set; }
    public long KeptBytes { get; set; }
    public long DeletedBytes { get; set; }
    public int DuplicatesCount { get; set; }
    public List<ScanJob> RecentJobs { get; set; } = [];

    public int TotalCount => NeedsReviewCount + KeptCount + DeletedCount;
    public long TotalBytes => NeedsReviewBytes + KeptBytes + DeletedBytes;
}
