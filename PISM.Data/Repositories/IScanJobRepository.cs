using PISM.Core.Models;

namespace PISM.Data.Repositories;

public interface IScanJobRepository
{
    Task<ScanJob?> GetNextPendingAsync();
    Task<List<ScanJob>> GetActiveJobsAsync();
    Task<List<ScanJob>> GetRecentAsync(int count);
    Task<ScanJob> CreateAsync(string folderPath);
    Task UpdateAsync(ScanJob job);
    Task ResetRunningJobsAsync();
}
