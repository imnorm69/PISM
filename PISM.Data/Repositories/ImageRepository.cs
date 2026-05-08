using Microsoft.EntityFrameworkCore;
using PISM.Core.Enums;
using PISM.Core.Models;

namespace PISM.Data.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly PismDbContext _db;

    public ImageRepository(PismDbContext db) => _db = db;

    public Task<ImageFile?> GetByHashAsync(string hash) =>
        _db.ImageFiles.FirstOrDefaultAsync(x => x.Hash == hash);

    public Task<bool> HashExistsInDeletedAsync(string hash) =>
        _db.DeletedHashes.AnyAsync(x => x.Hash == hash);

    public Task<List<ImageFile>> GetNeedsReviewAsync(int page, int pageSize) =>
        _db.ImageFiles
            .Where(x => x.Status == ImageStatus.NeedsReview)
            .OrderBy(x => x.DateScanned).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Tags)
            .ToListAsync();

    public Task<List<ImageFile>> GetByStatusAsync(ImageStatus status, int page, int pageSize) =>
        _db.ImageFiles
            .Where(x => x.Status == status)
            .OrderBy(x => x.DateScanned).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Tags)
            .ToListAsync();

    public Task<ImageFile?> GetByIdAsync(Guid id) =>
        _db.ImageFiles.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<(Guid? PreviousId, Guid? NextId)> GetAdjacentIdsAsync(Guid currentId, ImageStatus status)
    {
        var current = await _db.ImageFiles
            .Where(x => x.Id == currentId)
            .Select(x => new { x.DateScanned, x.Id })
            .FirstOrDefaultAsync();
        if (current == null) return (null, null);

        var prevId = await _db.ImageFiles
            .Where(x => x.Status == status &&
                (x.DateScanned < current.DateScanned ||
                 (x.DateScanned == current.DateScanned && x.Id < currentId)))
            .OrderByDescending(x => x.DateScanned).ThenByDescending(x => x.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        var nextId = await _db.ImageFiles
            .Where(x => x.Status == status &&
                (x.DateScanned > current.DateScanned ||
                 (x.DateScanned == current.DateScanned && x.Id > currentId)))
            .OrderBy(x => x.DateScanned).ThenBy(x => x.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        return (prevId, nextId);
    }

    public Task<int> CountDuplicatesAsync() =>
        _db.ImageFiles.CountAsync(x => x.IsDuplicate);

    public async Task<List<ImageFile>> GetRelatedAsync(string hash)
    {
        // Step 1: all folders that contain this hash
        var folders = await _db.FolderContents
            .Where(fc => fc.FileHash == hash)
            .Select(fc => fc.FolderPath)
            .Distinct()
            .ToListAsync();

        if (folders.Count == 0) return [];

        // Step 2: all hashes in those folders, excluding the queried hash itself
        var relatedHashes = await _db.FolderContents
            .Where(fc => folders.Contains(fc.FolderPath) && fc.FileHash != hash)
            .Select(fc => fc.FileHash)
            .Distinct()
            .ToListAsync();

        if (relatedHashes.Count == 0) return [];

        // Step 3: canonical (non-duplicate) ImageFiles for those hashes
        return await _db.ImageFiles
            .Where(img => relatedHashes.Contains(img.Hash) && !img.IsDuplicate)
            .Include(img => img.Tags)
            .OrderBy(img => img.DateScanned)
            .ToListAsync();
    }

    public async Task AddAsync(ImageFile image)
    {
        _db.ImageFiles.Add(image);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ImageFile image)
    {
        _db.ImageFiles.Update(image);
        await _db.SaveChangesAsync();
    }

    public Task<int> CountByStatusAsync(ImageStatus status) =>
        _db.ImageFiles.CountAsync(x => x.Status == status);

    public Task<long> TotalSizeByStatusAsync(ImageStatus status) =>
        _db.ImageFiles
            .Where(x => x.Status == status)
            .SumAsync(x => x.FileSizeBytes);
}
