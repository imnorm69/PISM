using Microsoft.EntityFrameworkCore;
using PISM.Core.Enums;
using PISM.Core.Models;
using PISM.Data.Repositories;

namespace PISM.Data.Services;

public class ReviewService
{
    private readonly PismDbContext _db;
    private readonly IImageRepository _imageRepo;

    public ReviewService(PismDbContext db, IImageRepository imageRepo)
    {
        _db = db;
        _imageRepo = imageRepo;
    }

    public async Task KeepAsync(Guid id)
    {
        await _db.ImageFiles
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ImageStatus.Kept));
    }

    public async Task DeleteAsync(Guid id)
    {
        var image = await _imageRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Image {id} not found.");

        await _db.ImageFiles
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ImageStatus.Deleted));

        if (!await _imageRepo.HashExistsInDeletedAsync(image.Hash))
        {
            _db.DeletedHashes.Add(new DeletedHash
            {
                Id = Guid.NewGuid(),
                Hash = image.Hash,
                DateDeleted = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> BulkKeepAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _db.ImageFiles
            .Where(x => idList.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ImageStatus.Kept));
    }

    public async Task<int> BulkDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();

        var hashes = await _db.ImageFiles
            .Where(x => idList.Contains(x.Id))
            .Select(x => x.Hash)
            .ToListAsync();

        var existingHashes = await _db.DeletedHashes
            .Where(x => hashes.Contains(x.Hash))
            .Select(x => x.Hash)
            .ToListAsync();

        foreach (var hash in hashes.Except(existingHashes))
        {
            _db.DeletedHashes.Add(new DeletedHash
            {
                Id = Guid.NewGuid(),
                Hash = hash,
                DateDeleted = DateTime.UtcNow
            });
        }

        var count = await _db.ImageFiles
            .Where(x => idList.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ImageStatus.Deleted));

        await _db.SaveChangesAsync();
        return count;
    }

    public async Task AddTagAsync(Guid imageId, string tag)
    {
        tag = tag.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(tag)) return;

        var exists = await _db.ImageTags.AnyAsync(t => t.ImageFileId == imageId && t.Tag == tag);
        if (!exists)
        {
            _db.ImageTags.Add(new ImageTag { Id = Guid.NewGuid(), ImageFileId = imageId, Tag = tag });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveTagAsync(Guid tagId)
    {
        await _db.ImageTags.Where(t => t.Id == tagId).ExecuteDeleteAsync();
    }

    public async Task<int> BulkTagAsync(IEnumerable<Guid> ids, string tag)
    {
        tag = tag.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(tag)) return 0;

        var idList = ids.ToList();
        var existingOwners = await _db.ImageTags
            .Where(t => idList.Contains(t.ImageFileId) && t.Tag == tag)
            .Select(t => t.ImageFileId)
            .ToListAsync();

        var toAdd = idList.Except(existingOwners).ToList();
        foreach (var id in toAdd)
            _db.ImageTags.Add(new ImageTag { Id = Guid.NewGuid(), ImageFileId = id, Tag = tag });

        if (toAdd.Count > 0) await _db.SaveChangesAsync();
        return toAdd.Count;
    }

    public async Task SaveCropAsync(Guid id, int? x, int? y, int? width, int? height, int rotation)
    {
        await _db.ImageFiles
            .Where(img => img.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(img => img.CropX, x)
                .SetProperty(img => img.CropY, y)
                .SetProperty(img => img.CropWidth, width)
                .SetProperty(img => img.CropHeight, height)
                .SetProperty(img => img.RotationDegrees, rotation));
    }
}
