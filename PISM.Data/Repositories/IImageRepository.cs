using PISM.Core.Enums;
using PISM.Core.Models;

namespace PISM.Data.Repositories;

public interface IImageRepository
{
    Task<ImageFile?> GetByHashAsync(string hash);
    Task<bool> HashExistsInDeletedAsync(string hash);
    Task<List<ImageFile>> GetNeedsReviewAsync(int page, int pageSize);
    Task<ImageFile?> GetByIdAsync(Guid id);
    Task AddAsync(ImageFile image);
    Task UpdateAsync(ImageFile image);
    Task<int> CountByStatusAsync(ImageStatus status);
    Task<long> TotalSizeByStatusAsync(ImageStatus status);
}
