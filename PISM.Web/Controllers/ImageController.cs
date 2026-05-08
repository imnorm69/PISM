using Microsoft.AspNetCore.Mvc;
using PISM.Core.Services;
using PISM.Data.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PISM.Web.Controllers;

public class ImageController : Controller
{
    private readonly IImageRepository _imageRepo;
    private readonly IFileStorageService _storage;

    public ImageController(IImageRepository imageRepo, IFileStorageService storage)
    {
        _imageRepo = imageRepo;
        _storage = storage;
    }

    [HttpGet("/image/{id:guid}")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = ["mode"])]
    public async Task<IActionResult> Get(Guid id, string mode = "preview")
    {
        var image = await _imageRepo.GetByIdAsync(id);
        if (image == null) return NotFound();

        var bytes = await _storage.RetrieveAsync(image.EncryptedFilePath);

        return mode switch
        {
            "thumbnail" => await ServeThumbnail(bytes, image.RotationDegrees),
            "preview" => await ServePreview(bytes, image.RotationDegrees),
            _ => File(bytes, GetContentType(image.FileName))
        };
    }

    private async Task<IActionResult> ServeThumbnail(byte[] bytes, int rotation)
    {
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using var img = await Image.LoadAsync(input);

        img.Mutate(x =>
        {
            if (rotation != 0) x.Rotate(rotation);
            x.Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.Max });
        });

        await img.SaveAsJpegAsync(output);
        return File(output.ToArray(), "image/jpeg");
    }

    private async Task<FileContentResult> ServePreview(byte[] bytes, int rotation)
    {
        if (rotation == 0) return File(bytes, "image/jpeg");

        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using var img = await Image.LoadAsync(input);
        img.Mutate(x => x.Rotate(rotation));
        await img.SaveAsJpegAsync(output);
        return File(output.ToArray(), "image/jpeg");
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
