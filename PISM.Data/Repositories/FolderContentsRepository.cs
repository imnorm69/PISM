using Microsoft.EntityFrameworkCore;
using PISM.Core.Models;

namespace PISM.Data.Repositories;

public class FolderContentsRepository : IFolderContentsRepository
{
    private readonly PismDbContext _db;

    public FolderContentsRepository(PismDbContext db) => _db = db;

    public async Task UpsertAsync(string hash, string folderPath)
    {
        var exists = await _db.FolderContents
            .AnyAsync(x => x.FileHash == hash && x.FolderPath == folderPath);

        if (!exists)
        {
            _db.FolderContents.Add(new FolderContent
            {
                Id = Guid.NewGuid(),
                FileHash = hash,
                FolderPath = folderPath
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(string hash, string folderPath)
    {
        await _db.FolderContents
            .Where(x => x.FileHash == hash && x.FolderPath == folderPath)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteByHashAsync(string hash)
    {
        await _db.FolderContents
            .Where(x => x.FileHash == hash)
            .ExecuteDeleteAsync();
    }
}
