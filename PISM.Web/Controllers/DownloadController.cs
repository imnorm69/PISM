using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using PISM.Core.Services;
using PISM.Data.Repositories;
using PISM.Web.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PISM.Web.Controllers;

public class DownloadController : Controller
{
    private readonly IImageRepository _imageRepo;
    private readonly IFileStorageService _storage;

    public DownloadController(IImageRepository imageRepo, IFileStorageService storage)
    {
        _imageRepo = imageRepo;
        _storage = storage;
    }

    [HttpGet("/download/{id:guid}")]
    public async Task<IActionResult> Single(Guid id, string format = "original")
    {
        var image = await _imageRepo.GetByIdAsync(id);
        if (image == null) return NotFound();

        var bytes = await _storage.RetrieveAsync(image.EncryptedFilePath);
        bool hasEdits = image.RotationDegrees != 0 || image.CropWidth.HasValue;

        if (format == "edited" && hasEdits)
        {
            bytes = await ApplyEdits(bytes, image.RotationDegrees,
                image.CropX, image.CropY, image.CropWidth, image.CropHeight);
            var editedName = Path.GetFileNameWithoutExtension(image.FileName) + "_edited.jpg";
            return File(bytes, "image/jpeg", editedName);
        }

        return File(bytes, GetContentType(image.FileName), image.FileName);
    }

    [HttpPost("/download/bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkDownloadRequest request)
    {
        if (request.Ids.Count == 0) return BadRequest();

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in request.Ids)
            {
                var image = await _imageRepo.GetByIdAsync(id);
                if (image == null) continue;

                var bytes = await _storage.RetrieveAsync(image.EncryptedFilePath);
                var entry = archive.CreateEntry(image.FileName, CompressionLevel.NoCompression);
                await using var stream = entry.Open();
                await stream.WriteAsync(bytes);
            }
        }

        ms.Position = 0;
        return File(ms.ToArray(), "application/zip", "pism-export.zip");
    }

    private static async Task<byte[]> ApplyEdits(byte[] bytes, int rotation,
        int? cropX, int? cropY, int? cropWidth, int? cropHeight)
    {
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using var img = await Image.LoadAsync(input);

        img.Mutate(x =>
        {
            if (rotation != 0) x.Rotate(rotation);
            if (cropX.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                x.Crop(new Rectangle(cropX.Value, cropY ?? 0, cropWidth.Value, cropHeight.Value));
        });

        await img.SaveAsJpegAsync(output);
        return output.ToArray();
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
}
