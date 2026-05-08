namespace PISM.Data.Repositories;

public interface IFolderContentsRepository
{
    Task UpsertAsync(string hash, string folderPath);
    Task DeleteAsync(string hash, string folderPath);
    Task DeleteByHashAsync(string hash);
}
