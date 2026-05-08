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
            .OrderBy(x => x.DateScanned)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Tags)
            .ToListAsync();

    public Task<ImageFile?> GetByIdAsync(Guid id) =>
        _db.ImageFiles.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id);

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
