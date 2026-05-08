using Microsoft.Extensions.Options;
using PISM.Core.Options;

namespace PISM.Core.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly IEncryptionService _encryption;

    public LocalFileStorageService(IOptions<StorageOptions> options, IEncryptionService encryption)
    {
        _root = options.Value.RootPath;
        _encryption = encryption;
    }

    public async Task<string> StoreAsync(string sourceFilePath, string hash)
    {
        var relativePath = BuildRelativePath(hash);
        var fullPath = Path.Combine(_root, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var plaintext = await File.ReadAllBytesAsync(sourceFilePath);
        var encrypted = _encryption.Encrypt(plaintext);
        await File.WriteAllBytesAsync(fullPath, encrypted);

        return relativePath;
    }

    public async Task<byte[]> RetrieveAsync(string storedPath)
    {
        var fullPath = Path.Combine(_root, storedPath);
        var encrypted = await File.ReadAllBytesAsync(fullPath);
        return _encryption.Decrypt(encrypted);
    }

    public void Delete(string storedPath)
    {
        var fullPath = Path.Combine(_root, storedPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public Task DeleteAllAsync()
    {
        if (Directory.Exists(_root))
        {
            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                File.Delete(file);

            foreach (var dir in Directory.EnumerateDirectories(_root))
                Directory.Delete(dir, recursive: true);
        }
        return Task.CompletedTask;
    }

    private static string BuildRelativePath(string hash)
    {
        // e.g. ab/c1/abcdef1234...enc
        var sub1 = hash[..2];
        var sub2 = hash[2..4];
        return Path.Combine(sub1, sub2, $"{hash}.enc");
    }
}
