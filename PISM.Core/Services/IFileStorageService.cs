namespace PISM.Core.Services;

public interface IFileStorageService
{
    /// <summary>Encrypts and stores a file. Returns the relative storage path.</summary>
    Task<string> StoreAsync(string sourceFilePath, string hash);

    /// <summary>Retrieves and decrypts a stored file by its relative path.</summary>
    Task<byte[]> RetrieveAsync(string storedPath);

    /// <summary>Deletes a single stored file by its relative path.</summary>
    void Delete(string storedPath);

    /// <summary>Deletes all files under the storage root. Used by the reset utility.</summary>
    Task DeleteAllAsync();
}
