using Microsoft.EntityFrameworkCore;
using PISM.Core.Enums;
using PISM.Core.Models;

namespace PISM.Data.Repositories;

public class ScanJobRepository : IScanJobRepository
{
    private readonly PismDbContext _db;

    public ScanJobRepository(PismDbContext db) => _db = db;

    public Task<ScanJob?> GetNextPendingAsync() =>
        _db.ScanJobs
            .Where(x => x.Status == ScanJobStatus.Pending)
            .OrderBy(x => x.DateStarted)
            .FirstOrDefaultAsync();

    public Task<List<ScanJob>> GetActiveJobsAsync() =>
        _db.ScanJobs
            .Where(x => x.Status == ScanJobStatus.Pending || x.Status == ScanJobStatus.Running)
            .OrderBy(x => x.DateStarted)
            .ToListAsync();

    public async Task<ScanJob> CreateAsync(string folderPath)
    {
        var job = new ScanJob
        {
            Id = Guid.NewGuid(),
            FolderPath = folderPath,
            DateStarted = DateTime.UtcNow,
            Status = ScanJobStatus.Pending
        };
        _db.ScanJobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task UpdateAsync(ScanJob job)
    {
        _db.ScanJobs.Update(job);
        await _db.SaveChangesAsync();
    }

    public async Task ResetRunningJobsAsync()
    {
        await _db.ScanJobs
            .Where(x => x.Status == ScanJobStatus.Running)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ScanJobStatus.Pending));
    }
}
