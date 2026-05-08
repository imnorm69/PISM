using PISM.Core.Models;

namespace PISM.Core.Services;

public interface IScannerService
{
    Task<ScanJob> EnqueueJobAsync(string folderPath);
    Task ProcessNextPendingJobAsync(IProgress<ScanProgressUpdate> progress, CancellationToken ct);
    Task<List<ScanJob>> GetActiveJobsAsync();
    Task ResetStuckJobsAsync();
}
