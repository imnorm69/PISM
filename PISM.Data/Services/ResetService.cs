using Microsoft.EntityFrameworkCore;
using PISM.Core.Services;

namespace PISM.Data.Services;

public class ResetService
{
    private readonly PismDbContext _db;
    private readonly IFileStorageService _storage;

    public ResetService(PismDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<ResetResult> ResetAsync()
    {
        // Collect sidecar paths before wiping ScanJobs
        var folderPaths = await _db.ScanJobs
            .Select(x => x.FolderPath)
            .Distinct()
            .ToListAsync();

        // Delete DB rows in FK-safe order
        var tags = await _db.ImageTags.ExecuteDeleteAsync();
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"ImageFiles\" SET \"DuplicateOfId\" = NULL");
        var images = await _db.ImageFiles.ExecuteDeleteAsync();
        var hashes = await _db.DeletedHashes.ExecuteDeleteAsync();
        var jobs = await _db.ScanJobs.ExecuteDeleteAsync();

        // Delete encrypted files
        await _storage.DeleteAllAsync();

        // Remove sidecar files
        int sidecarsRemoved = 0;
        foreach (var folder in folderPaths)
        {
            var sidecar = Path.Combine(folder, "_pism_scanned.txt");
            if (File.Exists(sidecar))
            {
                File.Delete(sidecar);
                sidecarsRemoved++;
            }
        }

        return new ResetResult
        {
            DbRowsCleared = tags + images + hashes + jobs,
            SidecarFilesRemoved = sidecarsRemoved
        };
    }
}

public class ResetResult
{
    public int DbRowsCleared { get; set; }
    public int SidecarFilesRemoved { get; set; }
}
