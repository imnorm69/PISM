using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PISM.Core.Enums;
using PISM.Core.Models;
using PISM.Core.Options;
using PISM.Core.Services;
using PISM.Data.Repositories;

namespace PISM.Data.Services;

public class ScannerService : IScannerService
{
    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    private const string SidecarFileName = "_pism_scanned.txt";

    private readonly IImageRepository _imageRepo;
    private readonly IScanJobRepository _jobRepo;
    private readonly IFolderContentsRepository _folderContents;
    private readonly IFileStorageService _storage;
    private readonly TestingOptions _testing;
    private readonly ILogger<ScannerService> _logger;

    public ScannerService(
        IImageRepository imageRepo,
        IScanJobRepository jobRepo,
        IFolderContentsRepository folderContents,
        IFileStorageService storage,
        IOptions<TestingOptions> testing,
        ILogger<ScannerService> logger)
    {
        _imageRepo = imageRepo;
        _jobRepo = jobRepo;
        _folderContents = folderContents;
        _storage = storage;
        _testing = testing.Value;
        _logger = logger;
    }

    public Task<ScanJob> EnqueueJobAsync(string folderPath) =>
        _jobRepo.CreateAsync(folderPath);

    public async Task<List<ScanJob>> GetActiveJobsAsync() =>
        await _jobRepo.GetActiveJobsAsync();

    public Task ResetStuckJobsAsync() =>
        _jobRepo.ResetRunningJobsAsync();

    public async Task ProcessNextPendingJobAsync(IProgress<ScanProgressUpdate> progress, CancellationToken ct)
    {
        var job = await _jobRepo.GetNextPendingAsync();
        if (job is null) return;

        job.Status = ScanJobStatus.Running;
        job.DateStarted = DateTime.UtcNow;
        await _jobRepo.UpdateAsync(job);

        try
        {
            await ProcessJobAsync(job, progress, ct);
        }
        catch (OperationCanceledException)
        {
            job.Status = ScanJobStatus.Pending;
            await _jobRepo.UpdateAsync(job);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan job {JobId} failed", job.Id);
            job.Status = ScanJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.DateCompleted = DateTime.UtcNow;
            await _jobRepo.UpdateAsync(job);
        }
    }

    private async Task ProcessJobAsync(ScanJob job, IProgress<ScanProgressUpdate> progress, CancellationToken ct)
    {
        if (!Directory.Exists(job.FolderPath))
            throw new DirectoryNotFoundException($"Folder not found: {job.FolderPath}");

        var allFiles = Directory
            .EnumerateFiles(job.FolderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        job.TotalFiles = allFiles.Count;
        await _jobRepo.UpdateAsync(job);

        var sidecar = _testing.PreserveOriginals
            ? await LoadSidecarAsync(job.FolderPath)
            : null;

        foreach (var filePath in allFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(filePath);

            if (IsAlreadyProcessed(fileName, sidecar))
            {
                job.ProcessedFiles++;
                continue;
            }

            try
            {
                await ProcessFileAsync(job, filePath, fileName, sidecar, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping file {File} due to error", filePath);
            }

            job.ProcessedFiles++;

            if (job.ProcessedFiles % 10 == 0)
                await _jobRepo.UpdateAsync(job);

            progress.Report(BuildUpdate(job, fileName));
        }

        if (sidecar is not null)
            await SaveSidecarAsync(job.FolderPath, sidecar);

        job.Status = ScanJobStatus.Completed;
        job.DateCompleted = DateTime.UtcNow;
        await _jobRepo.UpdateAsync(job);

        progress.Report(BuildUpdate(job, null));
    }

    private async Task ProcessFileAsync(
        ScanJob job, string filePath, string fileName,
        Dictionary<string, string>? sidecar, CancellationToken ct)
    {
        var hash = await ComputeHashAsync(filePath, ct);

        // Auto-delete: matches a previously deleted file
        if (await _imageRepo.HashExistsInDeletedAsync(hash))
        {
            job.DuplicatesFound++;
            sidecar?.TryAdd(fileName, hash);
            if (!_testing.PreserveOriginals)
                File.Delete(filePath);
            return;
        }

        // Duplicate of an existing kept/review file
        var existing = await _imageRepo.GetByHashAsync(hash);
        if (existing is not null)
        {
            var duplicate = new ImageFile
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                OriginalFolder = job.FolderPath,
                FileSizeBytes = new FileInfo(filePath).Length,
                Hash = hash,
                DateScanned = DateTime.UtcNow,
                Status = ImageStatus.NeedsReview,
                IsEncrypted = false,
                EncryptedFilePath = string.Empty,
                IsDuplicate = true,
                DuplicateOfId = existing.Id
            };
            await _imageRepo.AddAsync(duplicate);
            await _folderContents.UpsertAsync(hash, job.FolderPath);
            job.DuplicatesFound++;
            sidecar?.TryAdd(fileName, hash);
            return;
        }

        // New file — encrypt and store
        var storedPath = await _storage.StoreAsync(filePath, hash);

        var image = new ImageFile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            OriginalFolder = job.FolderPath,
            FileSizeBytes = new FileInfo(filePath).Length,
            Hash = hash,
            DateScanned = DateTime.UtcNow,
            Status = ImageStatus.NeedsReview,
            IsEncrypted = true,
            EncryptedFilePath = storedPath,
            IsDuplicate = false
        };
        await _imageRepo.AddAsync(image);
        await _folderContents.UpsertAsync(hash, job.FolderPath);
        job.NewFiles++;

        if (!_testing.PreserveOriginals)
            File.Delete(filePath);

        sidecar?.TryAdd(fileName, hash);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool IsAlreadyProcessed(string fileName, Dictionary<string, string>? sidecar) =>
        sidecar is not null && sidecar.ContainsKey(fileName);

    private static ScanProgressUpdate BuildUpdate(ScanJob job, string? currentFileName) => new()
    {
        JobId = job.Id,
        FolderPath = job.FolderPath,
        Status = job.Status,
        TotalFiles = job.TotalFiles,
        ProcessedFiles = job.ProcessedFiles,
        NewFiles = job.NewFiles,
        DuplicatesFound = job.DuplicatesFound,
        CurrentFileName = currentFileName
    };

    private static async Task<Dictionary<string, string>> LoadSidecarAsync(string folderPath)
    {
        var path = Path.Combine(folderPath, SidecarFileName);
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    private static async Task SaveSidecarAsync(string folderPath, Dictionary<string, string> sidecar)
    {
        var path = Path.Combine(folderPath, SidecarFileName);
        var json = JsonSerializer.Serialize(sidecar, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}
